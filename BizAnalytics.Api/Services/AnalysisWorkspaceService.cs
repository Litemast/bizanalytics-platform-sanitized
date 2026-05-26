using BizAnalytics.Api.Contracts.AnalysisWorkspaces;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Services;

public class AnalysisWorkspaceService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalysisWorkspaceService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<AnalysisWorkspaceResponse>> GetOrCreateAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var workspaces = await _db.AnalysisWorkspaces
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (workspaces.Count == 0)
        {
            var created = await CreateInternalAsync(organizationId, null, cancellationToken);

            var orphanRecords = await _db.SalesRecords
                .Where(x => x.OrganizationId == organizationId && x.AnalysisWorkspaceId == null)
                .ToListAsync(cancellationToken);

            if (orphanRecords.Count > 0)
            {
                foreach (var record in orphanRecords)
                {
                    record.AnalysisWorkspaceId = created.Id;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            workspaces = [created];
        }

        return workspaces.Select(Map).ToList();
    }

    public async Task<AnalysisWorkspaceResponse> CreateAsync(
        Guid organizationId,
        string? requestedName,
        CancellationToken cancellationToken)
    {
        var created = await CreateInternalAsync(organizationId, requestedName, cancellationToken);
        return Map(created);
    }

    public async Task<AnalysisWorkspaceResponse?> RenameAsync(
        Guid workspaceId,
        string name,
        CancellationToken cancellationToken)
    {
        var workspace = await _db.AnalysisWorkspaces.FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken);
        if (workspace is null)
        {
            return null;
        }

        workspace.Name = name.Trim();
        workspace.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Map(workspace);
    }

    public async Task<DeleteAnalysisWorkspaceResult?> DeleteAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var workspace = await _db.AnalysisWorkspaces.FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken);
        if (workspace is null)
        {
            return null;
        }

        var organizationId = workspace.OrganizationId;

        var relatedRecords = await _db.SalesRecords
            .Where(x => x.AnalysisWorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        if (relatedRecords.Count > 0)
        {
            _db.SalesRecords.RemoveRange(relatedRecords);
        }

        _db.AnalysisWorkspaces.Remove(workspace);
        await _db.SaveChangesAsync(cancellationToken);

        var remaining = await _db.AnalysisWorkspaces
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        AnalysisWorkspace? replacement = null;
        if (remaining.Count == 0)
        {
            replacement = await CreateInternalAsync(organizationId, null, cancellationToken);
            remaining = [replacement];
        }

        return new DeleteAnalysisWorkspaceResult
        {
            DeletedWorkspaceId = workspaceId,
            RemainingWorkspaces = remaining.Select(Map).ToList(),
            ReplacementWorkspaceId = replacement?.Id ?? remaining.FirstOrDefault()?.Id
        };
    }

    private async Task<AnalysisWorkspace> CreateInternalAsync(
        Guid organizationId,
        string? requestedName,
        CancellationToken cancellationToken)
    {
        var workspace = new AnalysisWorkspace
        {
            OrganizationId = organizationId,
            Name = await ResolveWorkspaceNameAsync(organizationId, requestedName, cancellationToken)
        };

        _db.AnalysisWorkspaces.Add(workspace);
        await _db.SaveChangesAsync(cancellationToken);

        return workspace;
    }

    private async Task<string> ResolveWorkspaceNameAsync(
        Guid organizationId,
        string? requestedName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName.Trim();
        }

        var prefix = IsEnglishRequested() ? "Analysis" : "Аналитика";
        var existingNames = await _db.AnalysisWorkspaces
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var index = 1;
        while (existingNames.Contains($"{prefix} {index}", StringComparer.OrdinalIgnoreCase))
        {
            index++;
        }

        return $"{prefix} {index}";
    }

    private bool IsEnglishRequested()
    {
        var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        var firstLanguage = acceptLanguage
            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLanguage?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static AnalysisWorkspaceResponse Map(AnalysisWorkspace workspace)
    {
        return new AnalysisWorkspaceResponse
        {
            Id = workspace.Id,
            OrganizationId = workspace.OrganizationId,
            Name = workspace.Name,
            CreatedAt = workspace.CreatedAt,
            UpdatedAt = workspace.UpdatedAt
        };
    }

    public sealed class DeleteAnalysisWorkspaceResult
    {
        public Guid DeletedWorkspaceId { get; set; }
        public Guid? ReplacementWorkspaceId { get; set; }
        public List<AnalysisWorkspaceResponse> RemainingWorkspaces { get; set; } = [];
    }
}
