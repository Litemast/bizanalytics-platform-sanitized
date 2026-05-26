using BizAnalytics.Api.Contracts.Auth;
using BizAnalytics.Api.Contracts.Organizations;
using BizAnalytics.Api.Tests.Infrastructure;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BizAnalytics.Api.Tests;

public class ApiWorkflowTests
{
    [Fact]
    public async Task Register_Login_And_Organizations_Workflow_Succeeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "org-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Northwind Retail" });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var organization = await createResponse.Content.ReadFromJsonAsync<OrganizationResponse>();
        Assert.NotNull(organization);
        Assert.Equal("Northwind Retail", organization.Name);

        var list = await client.GetFromJsonAsync<List<OrganizationResponse>>("/api/organizations");

        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal(organization.Id, list[0].Id);
    }

    [Fact]
    public async Task CsvImport_And_Analytics_Workflow_Succeeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "analytics-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Analytics Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var csv = """
                  Date,ProductName,Quantity,Amount
                  2026-03-01,Laptop,1,350000
                  2026-03-02,Mouse,2,12000
                  2026-03-02,Keyboard,1,18000
                  """;
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "file", "sales.csv");

        var importResponse = await client.PostAsync("/api/import/csv", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var importResult = await importResponse.Content.ReadFromJsonAsync<ImportCsvResponse>();
        Assert.NotNull(importResult);
        Assert.Equal(3, importResult.Count);

        var revenue = await client.GetFromJsonAsync<List<RevenuePointResponse>>(
            $"/api/analytics/revenue?organizationId={organization.Id}");
        var topProducts = await client.GetFromJsonAsync<List<TopProductResponse>>(
            $"/api/analytics/top-products?organizationId={organization.Id}");
        var summary = await client.GetFromJsonAsync<SummaryResponse>(
            $"/api/analytics/summary?organizationId={organization.Id}");

        Assert.NotNull(revenue);
        Assert.Equal(2, revenue.Count);
        Assert.Contains(revenue, x => x.Date.Date == new DateTime(2026, 3, 1) && x.Revenue == 350000m);
        Assert.Contains(revenue, x => x.Date.Date == new DateTime(2026, 3, 2) && x.Revenue == 30000m);

        Assert.NotNull(topProducts);
        Assert.Equal(3, topProducts.Count);
        Assert.Equal("Laptop", topProducts[0].ProductName);
        Assert.Equal(350000m, topProducts[0].TotalRevenue);

        Assert.NotNull(summary);
        Assert.Equal(380000m, summary.TotalRevenue);
        Assert.Equal(3, summary.TotalSalesCount);
        Assert.Equal(4, summary.TotalQuantity);
        Assert.Equal(380000m / 3m, summary.AverageCheck);
    }

    [Fact]
    public async Task Organization_Can_Be_Updated_And_Deleted()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "company-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Initial Company" });
        var organization = await createResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/organizations/{organization.Id}",
            new { name = "Updated Company" });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var organizations = await client.GetFromJsonAsync<List<OrganizationResponse>>("/api/organizations");
        Assert.NotNull(organizations);
        Assert.Single(organizations);
        Assert.Equal("Updated Company", organizations[0].Name);

        var deleteResponse = await client.DeleteAsync($"/api/organizations/{organization.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        organizations = await client.GetFromJsonAsync<List<OrganizationResponse>>("/api/organizations");
        Assert.NotNull(organizations);
        Assert.Empty(organizations);
    }

    [Fact]
    public async Task Multi_File_Import_Returns_Deep_Analytics_With_Source_Comparison()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "multi-import-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Multi Import Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var firstCsv = """
                       Date,ProductName,Quantity,Amount
                       2026-03-01,Laptop,1,350000
                       2026-03-02,Mouse,2,12000
                       """;
        var firstFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(firstCsv));
        firstFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(firstFileContent, "files", "march-sales.csv");

        var secondCsv = """
                        Date,ProductName,Quantity,Amount
                        2026-03-03,Laptop,1,360000
                        2026-03-03,Keyboard,1,18000
                        """;
        var secondFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(secondCsv));
        secondFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(secondFileContent, "files", "april-sales.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var importResult = await importResponse.Content.ReadFromJsonAsync<ImportFilesResponse>();
        Assert.NotNull(importResult);
        Assert.Equal(4, importResult.Count);
        Assert.Equal(2, importResult.FileCount);
        Assert.Equal(2, importResult.Files.Count);
        Assert.Equal(2, importResult.Analytics.SourceComparisons.Count);

        var deepDive = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}");

        Assert.NotNull(deepDive);
        Assert.Equal(740000m, deepDive.Summary.TotalRevenue);
        Assert.Equal(2, deepDive.SourceComparisons.Count);
        Assert.Contains(deepDive.SourceComparisons, item => item.SourceName == "march-sales.csv");
        Assert.Contains(deepDive.SourceComparisons, item => item.SourceName == "april-sales.csv");
    }

    [Fact]
    public async Task Multi_File_Import_Supports_Russian_Headers_And_Cyrillic_Data()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "ru-import-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Русская организация" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var firstCsv = "Дата;Товар;Количество;Сумма\n31.03.2026;Ноутбук;1;100000\n31.03.2026;Мышь;2;5000";
        var firstFileContent = new ByteArrayContent(Encoding.GetEncoding(1251).GetBytes(firstCsv));
        firstFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(firstFileContent, "files", "sales-ru-1.csv");

        var secondCsv = "Дата;Товар;Количество;Сумма\n30.03.2026;Клавиатура;1;7000\n30.03.2026;Монитор;1;45000";
        var secondFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(secondCsv));
        secondFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(secondFileContent, "files", "sales-ru-2.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var importResult = await importResponse.Content.ReadFromJsonAsync<ImportFilesResponse>();
        Assert.NotNull(importResult);
        Assert.Equal(4, importResult.Count);
        Assert.Equal(2, importResult.FileCount);
        Assert.Equal(2, importResult.Analytics.SourceComparisons.Count);

        var deepDive = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}");

        Assert.NotNull(deepDive);
        Assert.Equal(157000m, deepDive.Summary.TotalRevenue);
        Assert.Contains(deepDive.SourceComparisons, item => item.SourceName == "sales-ru-1.csv");
        Assert.Contains(deepDive.SourceComparisons, item => item.SourceName == "sales-ru-2.csv");
    }

    [Fact]
    public async Task Word_Report_With_Period_And_Sold_Column_Is_Imported_Into_Analytics()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "docx-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "DOCX Import Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var docxContent = new ByteArrayContent(CreateMonthlySalesReportDocxBytes());
        docxContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        multipart.Add(docxContent, "files", "sales-summary.docx");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var importResult = await importResponse.Content.ReadFromJsonAsync<ImportFilesResponse>();
        Assert.NotNull(importResult);
        Assert.Equal(3, importResult.Count);
        Assert.Equal(1, importResult.FileCount);

        var deepDive = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}");

        Assert.NotNull(deepDive);
        Assert.Equal(10_750_000m, deepDive.Summary.TotalRevenue);
        Assert.Equal(3, deepDive.Summary.TotalSalesCount);
        Assert.Equal(80, deepDive.Summary.TotalQuantity);
        Assert.Single(deepDive.SourceComparisons);
        Assert.Equal("sales-summary.docx", deepDive.SourceComparisons[0].SourceName);

        var revenue = await client.GetFromJsonAsync<List<RevenuePointResponse>>(
            $"/api/analytics/revenue?organizationId={organization.Id}");

        Assert.NotNull(revenue);
        Assert.Single(revenue);
        Assert.Equal(new DateTime(2026, 1, 1), revenue[0].Date.Date);
        Assert.Equal(10_750_000m, revenue[0].Revenue);
    }

    [Fact]
    public async Task Sales_Report_With_Alternative_Headers_Is_Imported_Into_Analytics()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "sales-alt-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Sales Alt Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var csv = """
                  Order Date,Item Name,Qty Sold,Sales Amount
                  2026-03-01,Laptop,1,350000
                  2026-03-02,Mouse,2,12000
                  """;
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "files", "sales-alt.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var importResult = await importResponse.Content.ReadFromJsonAsync<ImportFilesResponse>();
        Assert.NotNull(importResult);
        Assert.Equal(2, importResult.Count);
        Assert.Equal(362000m, importResult.Analytics.Summary.TotalRevenue);
        Assert.Equal(2, importResult.Analytics.Summary.TotalSalesCount);
        Assert.Equal(3, importResult.Analytics.Summary.TotalQuantity);
    }

    [Fact]
    public async Task Financial_Report_Is_Imported_And_Analyzed()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "finance-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Finance Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var csv = """
                  Period,Revenue,Expenses,Profit
                  2026-01-01,100000,70000,30000
                  2026-02-01,130000,80000,50000
                  2026-03-01,160000,100000,60000
                  """;
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "files", "finance.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var payload = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var analytics = payload.RootElement.GetProperty("analytics");

        Assert.Equal("financial_report", analytics.GetProperty("reportType").GetString());
        Assert.Equal(390000m, analytics.GetProperty("financial").GetProperty("totalRevenue").GetDecimal());
        Assert.Equal(250000m, analytics.GetProperty("financial").GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(140000m, analytics.GetProperty("financial").GetProperty("totalProfit").GetDecimal());
        Assert.Equal(3, analytics.GetProperty("financial").GetProperty("periods").GetArrayLength());
        Assert.Equal(3, analytics.GetProperty("financial").GetProperty("forecastTrend").GetArrayLength());
    }

    [Fact]
    public async Task Financial_Report_With_Alternative_Headers_Is_Imported_And_Analyzed()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "finance-alt-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Finance Alt Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var csv = """
                  Reporting Period,Gross Revenue,Operating Expenses,Net Income
                  2026-01-01,90000,60000,30000
                  2026-02-01,140000,90000,50000
                  """;
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "files", "finance-alt.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var payload = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var analytics = payload.RootElement.GetProperty("analytics");

        Assert.Equal("financial_report", analytics.GetProperty("reportType").GetString());
        Assert.Equal(230000m, analytics.GetProperty("financial").GetProperty("totalRevenue").GetDecimal());
        Assert.Equal(150000m, analytics.GetProperty("financial").GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(80000m, analytics.GetProperty("financial").GetProperty("totalProfit").GetDecimal());
    }

    [Fact]
    public async Task Education_Report_Is_Imported_And_Analyzed()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "education-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Education Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var csv = """
                  Student,Subject,Grade
                  Anna,Math,5
                  Anna,History,4
                  Boris,Math,3
                  Boris,History,2
                  """;
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "files", "education.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var payload = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var analytics = payload.RootElement.GetProperty("analytics");
        var education = analytics.GetProperty("education");

        Assert.Equal("education_report", analytics.GetProperty("reportType").GetString());
        Assert.Equal(3.5m, education.GetProperty("averageScore").GetDecimal());
        Assert.Equal("Anna", education.GetProperty("bestStudent").GetString());
        Assert.Equal("Boris", education.GetProperty("worstStudent").GetString());
        Assert.Equal(75m, education.GetProperty("successRate").GetDecimal());
        Assert.True(education.GetProperty("gradeDistribution").GetArrayLength() > 0);
        Assert.Equal("Anna", education.GetProperty("studentForecasts")[0].GetProperty("studentName").GetString());
    }

    [Fact]
    public async Task Education_Report_With_Alternative_Headers_Is_Imported_And_Analyzed()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "education-alt-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Education Alt Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var csv = """
                  Student Full Name,Subject Name,Test Score
                  Anna Petrova,Math,5
                  Anna Petrova,Physics,4
                  Boris Sidorov,Math,3
                  Boris Sidorov,Physics,2
                  """;
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "files", "education-alt.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var payload = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var analytics = payload.RootElement.GetProperty("analytics");
        var education = analytics.GetProperty("education");

        Assert.Equal("education_report", analytics.GetProperty("reportType").GetString());
        Assert.Equal(3.5m, education.GetProperty("averageScore").GetDecimal());
        Assert.Equal("Anna Petrova", education.GetProperty("bestStudent").GetString());
        Assert.Equal("Boris Sidorov", education.GetProperty("worstStudent").GetString());
    }

    [Fact]
    public async Task Multi_File_Import_Can_Mix_Sales_Financial_And_Education_Reports()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "mixed-reports-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Mixed Reports Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var salesCsv = """
                       Date,ProductName,Quantity,Amount
                       2026-04-01,Laptop,1,350000
                       2026-04-02,Mouse,2,12000
                       """;
        var salesContent = new ByteArrayContent(Encoding.UTF8.GetBytes(salesCsv));
        salesContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(salesContent, "files", "sales.csv");

        var financeCsv = """
                         Period,Revenue,Expenses,Profit
                         2026-04-01,200000,120000,80000
                         2026-04-02,240000,150000,90000
                         """;
        var financeContent = new ByteArrayContent(Encoding.UTF8.GetBytes(financeCsv));
        financeContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(financeContent, "files", "finance.csv");

        var educationCsv = """
                           Student,Subject,Grade
                           Anna,Math,5
                           Boris,Math,3
                           """;
        var educationContent = new ByteArrayContent(Encoding.UTF8.GetBytes(educationCsv));
        educationContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(educationContent, "files", "education.csv");

        var importResponse = await client.PostAsync("/api/import/files", multipart);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var payload = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        var analytics = root.GetProperty("analytics");

        Assert.Equal(6, root.GetProperty("count").GetInt32());
        Assert.Equal(3, root.GetProperty("fileCount").GetInt32());
        Assert.Equal("mixed_report", analytics.GetProperty("reportType").GetString());
        Assert.Equal(362000m, analytics.GetProperty("summary").GetProperty("totalRevenue").GetDecimal());
        Assert.Equal(170000m, analytics.GetProperty("financial").GetProperty("totalProfit").GetDecimal());
        Assert.Equal(4m, analytics.GetProperty("education").GetProperty("averageScore").GetDecimal());
        Assert.Single(analytics.GetProperty("sourceComparisons").EnumerateArray());
    }

    [Fact]
    public async Task Multi_File_Import_Rejects_More_Than_Ten_Files()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "too-many-files-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "File Limit Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(organization.Id.ToString()), "organizationId");

        for (var index = 1; index <= 11; index++)
        {
            var csv = $"""
                       Date,ProductName,Quantity,Amount
                       2026-04-{index:00},Product {index},1,1000
                       """;
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipart.Add(content, "files", $"sales-{index}.csv");
        }

        var importResponse = await client.PostAsync("/api/import/files", multipart);

        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
        var payload = await importResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Contains("10", payload["message"]);
    }

    [Fact]
    public async Task Pdf_Report_Supports_Financial_And_Education_Analytics()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "report-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var response = await client.PostAsJsonAsync(
            "/api/reports/analytics-pdf",
            new
            {
                organizationName = "Report Org",
                analysisName = "Mixed analytics",
                generatedFor = login.Email,
                language = "ru",
                analytics = new
                {
                    generatedAtUtc = DateTime.UtcNow,
                    reportType = "mixed_report",
                    summary = new { totalRevenue = 0, totalSalesCount = 0, totalQuantity = 0, averageCheck = 0 },
                    revenue = Array.Empty<object>(),
                    topProducts = Array.Empty<object>(),
                    priceTrends = Array.Empty<object>(),
                    sourceComparisons = Array.Empty<object>(),
                    insights = new[]
                    {
                        new { title = "Финансовый вывод", description = "Прибыль растет.", tone = "success" }
                    },
                    financial = new
                    {
                        totalRevenue = 300000,
                        totalExpenses = 180000,
                        totalProfit = 120000,
                        profitability = 40,
                        linearRegressionForecast = 130000,
                        movingAverageForecast = 120000,
                        trendExtrapolationForecast = 140000,
                        periods = new[]
                        {
                            new { period = new DateTime(2026, 1, 1), periodLabel = "Январь 2026", revenue = 100000, expenses = 70000, profit = 30000, profitability = 30 },
                            new { period = new DateTime(2026, 2, 1), periodLabel = "Февраль 2026", revenue = 200000, expenses = 110000, profit = 90000, profitability = 45 }
                        },
                        forecastTrend = new[]
                        {
                            new { period = new DateTime(2026, 3, 1), periodLabel = "01.03.2026", forecastProfit = 130000 }
                        },
                        insights = Array.Empty<object>()
                    },
                    education = new
                    {
                        averageScore = 4.2,
                        bestStudent = "Anna",
                        worstStudent = "Boris",
                        bestSubject = "Math",
                        worstSubject = "History",
                        successRate = 80,
                        gradeDistribution = new[]
                        {
                            new { grade = "5", count = 2 },
                            new { grade = "4", count = 1 }
                        },
                        studentPerformance = new[]
                        {
                            new { name = "Anna", averageScore = 4.8 },
                            new { name = "Boris", averageScore = 3.2 }
                        },
                        subjectPerformance = new[]
                        {
                            new { name = "Math", averageScore = 4.5 }
                        },
                        studentForecasts = new[]
                        {
                            new { studentName = "Anna", currentAverage = 4.8, forecastAverage = 5.0 }
                        },
                        riskStudents = new[]
                        {
                            new { studentName = "Boris", averageScore = 2.7, riskLevel = "medium" }
                        },
                        insights = Array.Empty<object>()
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "BizAnalitics report.pdf",
            response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task Analytics_Reset_Removes_Previous_Records_Before_New_Import()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "reset-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Reset Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        using (var firstImport = new MultipartFormDataContent())
        {
            firstImport.Add(new StringContent(organization.Id.ToString()), "organizationId");

            var firstCsv = """
                           Date,ProductName,Quantity,Amount
                           2026-03-01,Laptop,1,350000
                           2026-03-02,Mouse,2,12000
                           """;
            var firstFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(firstCsv));
            firstFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            firstImport.Add(firstFileContent, "files", "first-batch.csv");

            var firstImportResponse = await client.PostAsync("/api/import/files", firstImport);
            Assert.Equal(HttpStatusCode.OK, firstImportResponse.StatusCode);
        }

        var resetResponse = await client.DeleteAsync($"/api/analytics/reset?organizationId={organization.Id}");
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var analyticsAfterReset = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}");

        Assert.NotNull(analyticsAfterReset);
        Assert.Equal(0m, analyticsAfterReset.Summary.TotalRevenue);
        Assert.Equal(0, analyticsAfterReset.Summary.TotalSalesCount);
        Assert.Empty(analyticsAfterReset.SourceComparisons);

        using var secondImport = new MultipartFormDataContent();
        secondImport.Add(new StringContent(organization.Id.ToString()), "organizationId");

        var secondCsv = """
                        Date,ProductName,Quantity,Amount
                        2026-03-04,Keyboard,1,18000
                        2026-03-04,Monitor,1,45000
                        """;
        var secondFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(secondCsv));
        secondFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        secondImport.Add(secondFileContent, "files", "second-batch.csv");

        var secondImportResponse = await client.PostAsync("/api/import/files", secondImport);
        Assert.Equal(HttpStatusCode.OK, secondImportResponse.StatusCode);

        var finalAnalytics = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}");

        Assert.NotNull(finalAnalytics);
        Assert.Equal(63000m, finalAnalytics.Summary.TotalRevenue);
        Assert.Equal(2, finalAnalytics.Summary.TotalSalesCount);
        Assert.Single(finalAnalytics.SourceComparisons);
        Assert.Equal("second-batch.csv", finalAnalytics.SourceComparisons[0].SourceName);
    }

    [Fact]
    public async Task Analysis_Workspaces_Isolate_Analytics_For_Separate_Imports()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "workspace-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var createOrganizationResponse = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Workspace Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        var initialWorkspaces = await client.GetFromJsonAsync<List<AnalysisWorkspaceResponse>>(
            $"/api/analysisworkspaces?organizationId={organization.Id}");

        Assert.NotNull(initialWorkspaces);
        Assert.Single(initialWorkspaces);

        var firstWorkspace = initialWorkspaces[0];

        var secondWorkspaceResponse = await client.PostAsJsonAsync(
            "/api/analysisworkspaces",
            new { organizationId = organization.Id });

        Assert.Equal(HttpStatusCode.OK, secondWorkspaceResponse.StatusCode);

        var secondWorkspace = await secondWorkspaceResponse.Content.ReadFromJsonAsync<AnalysisWorkspaceResponse>();
        Assert.NotNull(secondWorkspace);
        Assert.NotEqual(firstWorkspace.Id, secondWorkspace.Id);

        using (var firstImport = new MultipartFormDataContent())
        {
            firstImport.Add(new StringContent(organization.Id.ToString()), "organizationId");
            firstImport.Add(new StringContent(firstWorkspace.Id.ToString()), "analysisWorkspaceId");

            var firstCsv = """
                           Date,ProductName,Quantity,Amount
                           2026-03-01,Laptop,1,350000
                           2026-03-02,Mouse,2,12000
                           """;
            var firstFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(firstCsv));
            firstFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            firstImport.Add(firstFileContent, "files", "workspace-a.csv");

            var firstImportResponse = await client.PostAsync("/api/import/files", firstImport);
            Assert.Equal(HttpStatusCode.OK, firstImportResponse.StatusCode);
        }

        using (var secondImport = new MultipartFormDataContent())
        {
            secondImport.Add(new StringContent(organization.Id.ToString()), "organizationId");
            secondImport.Add(new StringContent(secondWorkspace.Id.ToString()), "analysisWorkspaceId");

            var secondCsv = """
                            Date,ProductName,Quantity,Amount
                            2026-03-03,Monitor,1,45000
                            2026-03-03,Keyboard,1,18000
                            """;
            var secondFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(secondCsv));
            secondFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            secondImport.Add(secondFileContent, "files", "workspace-b.csv");

            var secondImportResponse = await client.PostAsync("/api/import/files", secondImport);
            Assert.Equal(HttpStatusCode.OK, secondImportResponse.StatusCode);
        }

        var firstAnalytics = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}&analysisWorkspaceId={firstWorkspace.Id}");
        var secondAnalytics = await client.GetFromJsonAsync<AnalysisBundleResponse>(
            $"/api/analytics/deep-dive?organizationId={organization.Id}&analysisWorkspaceId={secondWorkspace.Id}");

        Assert.NotNull(firstAnalytics);
        Assert.NotNull(secondAnalytics);
        Assert.Equal(362000m, firstAnalytics.Summary.TotalRevenue);
        Assert.Equal(63000m, secondAnalytics.Summary.TotalRevenue);
        Assert.Single(firstAnalytics.SourceComparisons);
        Assert.Single(secondAnalytics.SourceComparisons);
        Assert.Equal("workspace-a.csv", firstAnalytics.SourceComparisons[0].SourceName);
        Assert.Equal("workspace-b.csv", secondAnalytics.SourceComparisons[0].SourceName);
    }

    [Fact]
    public async Task Foreign_User_Cannot_Access_Another_Users_Organization()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var ownerClient = factory.CreateClient();
        using var foreignClient = factory.CreateClient();

        var ownerLogin = await RegisterAndLoginAsync(ownerClient, "owner@example.com");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerLogin.Token);

        var createOrganizationResponse = await ownerClient.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest { Name = "Private Org" });
        var organization = await createOrganizationResponse.Content.ReadFromJsonAsync<OrganizationResponse>();

        Assert.NotNull(organization);

        var foreignLogin = await RegisterAndLoginAsync(foreignClient, "foreign@example.com");
        foreignClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", foreignLogin.Token);

        var response = await foreignClient.GetAsync($"/api/datasources/{organization.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Api_Returns_English_Message_When_AcceptLanguage_Is_English()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.AcceptLanguage.Add(
            new StringWithQualityHeaderValue("en"));

        var request = new RegisterRequest
        {
            Email = "english@example.com",
            Password = "Password123!"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var payload = await secondResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(payload);
        Assert.Equal("A user with this email already exists.", payload["message"]);
    }

    [Fact]
    public async Task Entrepreneur_Search_Returns_Registry_Data_And_Recommended_Forms()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "entrepreneur-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var simplifiedResponse = await client.GetAsync("/api/individualentrepreneurs/search?iin=123456789876");
        Assert.Equal(HttpStatusCode.OK, simplifiedResponse.StatusCode);

        using var simplifiedPayload = JsonDocument.Parse(await simplifiedResponse.Content.ReadAsStringAsync());
        Assert.True(simplifiedPayload.RootElement.GetProperty("found").GetBoolean());
        Assert.Equal("demo", simplifiedPayload.RootElement.GetProperty("mode").GetString());
        Assert.Contains(
            simplifiedPayload.RootElement.GetProperty("reportForms").EnumerateArray(),
            item => item.GetProperty("formCode").GetString() == "910.00");

        var generalResponse = await client.GetAsync("/api/individualentrepreneurs/search?iin=444444444444");
        Assert.Equal(HttpStatusCode.OK, generalResponse.StatusCode);

        using var generalPayload = JsonDocument.Parse(await generalResponse.Content.ReadAsStringAsync());
        Assert.True(generalPayload.RootElement.GetProperty("found").GetBoolean());
        Assert.Contains(
            generalPayload.RootElement.GetProperty("reportForms").EnumerateArray(),
            item => item.GetProperty("formCode").GetString() == "220.00");
        Assert.Contains(
            generalPayload.RootElement.GetProperty("reportForms").EnumerateArray(),
            item => item.GetProperty("formCode").GetString() == "200.00");
    }

    [Fact]
    public async Task Entrepreneur_Form_Pdf_Can_Be_Generated_From_Registry_Search()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await RegisterAndLoginAsync(client, "entrepreneur-report@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var searchResponse = await client.GetAsync("/api/individualentrepreneurs/search?iin=444444444444");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        using var searchPayload = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        var registryJson = searchPayload.RootElement.GetProperty("registry").GetRawText();
        var requestBody = $$"""
        {
          "formCode": "220.00",
          "language": "ru",
          "generatedFor": "{{login.Email}}",
          "registry": {{registryJson}}
        }
        """;

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        var pdfResponse = await client.PostAsync("/api/reports/entrepreneur-form-pdf", content);

        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Contains(
            "Entrepreneur-220.00.pdf",
            pdfResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty);

        var bytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    private static async Task<AuthTokenResponse> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(authResult);
        Assert.False(string.IsNullOrWhiteSpace(authResult.Token));

        return authResult;
    }

    private static byte[] CreateMonthlySalesReportDocxBytes()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            var body = new Body(
                CreateParagraph("Отчет по продажам за месяц"),
                CreateParagraph("Период: Январь 2026"),
                CreateParagraph("Валюта: тенге (₸)"),
                CreateSalesTable(),
                CreateParagraph("Общая выручка: 10750000 ₸"));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static Paragraph CreateParagraph(string text)
    {
        return new Paragraph(new Run(new Text(text)));
    }

    private static Table CreateSalesTable()
    {
        var table = new Table();

        table.AppendChild(new TableProperties(
            new TableWidth { Type = TableWidthUnitValues.Auto, Width = "0" }));

        table.Append(
            CreateRow("Товар", "Куплено", "Продано", "Остаток", "Цена", "Выручка"),
            CreateRow("Ноутбук", "20", "15", "5", "350000", "5250000"),
            CreateRow("Смартфон", "30", "25", "5", "180000", "4500000"),
            CreateRow("Наушники", "50", "40", "10", "25000", "1000000"));

        return table;
    }

    private static TableRow CreateRow(params string[] values)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            row.Append(new TableCell(new Paragraph(new Run(new Text(value)))));
        }

        return row;
    }

    private sealed class OrganizationResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ImportCsvResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private sealed class ImportFilesResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public int FileCount { get; set; }
        public List<ImportedFileResponse> Files { get; set; } = [];
        public AnalysisBundleResponse Analytics { get; set; } = new();
    }

    private sealed class ImportedFileResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public int ImportedRecords { get; set; }
    }

    private sealed class RevenuePointResponse
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class TopProductResponse
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    private sealed class SummaryResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalSalesCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal AverageCheck { get; set; }
    }

    private sealed class AnalysisBundleResponse
    {
        public SummaryResponse Summary { get; set; } = new();
        public List<SourceComparisonResponse> SourceComparisons { get; set; } = [];
    }

    private sealed class SourceComparisonResponse
    {
        public string SourceName { get; set; } = string.Empty;
        public int RecordsCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageUnitPrice { get; set; }
    }

    private sealed class AnalysisWorkspaceResponse
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
