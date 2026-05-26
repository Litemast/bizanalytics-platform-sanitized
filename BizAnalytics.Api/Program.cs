using BizAnalytics.Api.Infrastructure.Data;
using BizAnalytics.Api.Infrastructure.Localization;
using BizAnalytics.Api.Infrastructure.Security;
using BizAnalytics.Api.Options;
using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Collections;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

var kgdRegistryOverrides = Program.BuildKgdRegistryEnvironmentOverrides(Environment.GetEnvironmentVariables());
if (kgdRegistryOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(kgdRegistryOverrides);
}

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(
    jwtSettings["Key"] ?? throw new InvalidOperationException("JWT key is not configured."));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
var rawDbConnectionString = builder.Configuration.GetConnectionString("Db")
    ?? builder.Configuration["ConnectionStrings:Db"]
    ?? builder.Configuration["ConnectionStrings__Db"];
var normalizedDbConnectionString = Program.NormalizePostgresConnectionString(rawDbConnectionString);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(normalizedDbConnectionString));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddScoped<OrganizationAccessService>();
builder.Services.AddScoped<AnalysisWorkspaceService>();
builder.Services.AddScoped<IApiTextLocalizer, ApiTextLocalizer>();
builder.Services.AddScoped<ImportProcessingService>();
builder.Services.AddScoped<ReportImportProcessingService>();
builder.Services.AddScoped<AnalyticsAggregationService>();
builder.Services.AddScoped<AnalyticsReportService>();
builder.Services.AddScoped<EntrepreneurTaxFormReportService>();
builder.Services.Configure<MarketDataOptions>(builder.Configuration.GetSection("MarketData"));
builder.Services.Configure<KgdRegistryOptions>(builder.Configuration.GetSection("KgdRegistry"));
builder.Services.AddHttpClient<MarketDataService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BizAnalyticsPlatform/1.0");
});
builder.Services.AddHttpClient<IndividualEntrepreneurRegistryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BizAnalyticsPlatform/1.0");
});

var app = builder.Build();

var enableHttpsRedirection = builder.Configuration.GetValue<bool?>("EnableHttpsRedirection")
    ?? (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"));
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var hasBuiltFrontend = File.Exists(Path.Combine(webRootPath, "index.html"));

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseForwardedHeaders();

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

if (hasBuiltFrontend)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

if (hasBuiltFrontend)
{
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program
{
    public static Dictionary<string, string?> BuildKgdRegistryEnvironmentOverrides(IDictionary environmentVariables)
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var mode = GetEnvironmentValue(environmentVariables, "KGD_REGISTRY_MODE");
        var baseUrl = GetEnvironmentValue(environmentVariables, "KGD_PORTAL_BASE_URL");
        var portalToken = GetEnvironmentValue(environmentVariables, "KGD_PORTAL_TOKEN");

        if (!string.IsNullOrWhiteSpace(mode))
        {
            overrides["KgdRegistry:Mode"] = mode;
        }

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            overrides["KgdRegistry:BaseUrl"] = baseUrl;
        }

        if (!string.IsNullOrWhiteSpace(portalToken))
        {
            overrides["KgdRegistry:PortalToken"] = portalToken;

            if (string.IsNullOrWhiteSpace(mode))
            {
                overrides["KgdRegistry:Mode"] = "live";
            }
        }

        return overrides;
    }

    public static string NormalizePostgresConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = username,
            Password = password
        };

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            var query = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var segment in query)
            {
                var parts = segment.Split('=', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);

                switch (key.ToLowerInvariant())
                {
                    case "sslmode":
                        builder.SslMode = Enum.TryParse<SslMode>(value, true, out var sslMode)
                            ? sslMode
                            : builder.SslMode;
                        break;
                    case "trust server certificate":
                    case "trustservercertificate":
                        if (bool.TryParse(value, out var trustServerCertificate))
                        {
                            builder.TrustServerCertificate = trustServerCertificate;
                        }
                        break;
                    case "pooling":
                        if (bool.TryParse(value, out var pooling))
                        {
                            builder.Pooling = pooling;
                        }
                        break;
                    default:
                        builder[key] = value;
                        break;
                }
            }
        }

        return builder.ConnectionString;
    }

    private static string? GetEnvironmentValue(IDictionary environmentVariables, string key)
    {
        return environmentVariables.Contains(key)
            ? environmentVariables[key]?.ToString()
            : null;
    }
}
