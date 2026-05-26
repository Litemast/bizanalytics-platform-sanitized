using BizAnalytics.Api.Contracts.Import;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Auth;
using BizAnalytics.Api.Infrastructure.Data;
using BizAnalytics.Api.Infrastructure.Localization;
using BizAnalytics.Api.Infrastructure.Security;
using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private const int MaxFilesPerImport = 10;

    private readonly AppDbContext _db;
    private readonly OrganizationAccessService _organizationAccessService;
    private readonly IApiTextLocalizer _texts;
    private readonly ImportProcessingService _importProcessingService;
    private readonly ReportImportProcessingService _reportImportProcessingService;
    private readonly AnalyticsAggregationService _analyticsAggregationService;

    public ImportController(
        AppDbContext db,
        OrganizationAccessService organizationAccessService,
        IApiTextLocalizer texts,
        ImportProcessingService importProcessingService,
        ReportImportProcessingService reportImportProcessingService,
        AnalyticsAggregationService analyticsAggregationService)
    {
        _db = db;
        _organizationAccessService = organizationAccessService;
        _texts = texts;
        _importProcessingService = importProcessingService;
        _reportImportProcessingService = reportImportProcessingService;
        _analyticsAggregationService = analyticsAggregationService;
    }

    [HttpPost("csv")]
    public async Task<IActionResult> ImportCsv(
        [FromForm] ImportCsvRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateAccessAsync(
            request.OrganizationId,
            request.AnalysisWorkspaceId,
            cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = _texts.FileWasNotUploaded() });
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = _texts.UnsupportedImportFileExtension(extension) });
        }

        try
        {
            var result = await _importProcessingService.ParseFilesAsync(
                [request.File],
                request.OrganizationId,
                cancellationToken);

            if (result.Records.Count == 0)
            {
                return BadRequest(new { message = _texts.TableWithSupportedHeadersWasNotFound(request.File.FileName) });
            }

            foreach (var record in result.Records)
            {
                record.AnalysisWorkspaceId = request.AnalysisWorkspaceId;
            }

            _db.SalesRecords.AddRange(result.Records);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new ImportCsvResponse
            {
                Message = _texts.ImportCompletedSuccessfully(1, result.Records.Count),
                Count = result.Records.Count
            });
        }
        catch (ImportProcessingService.ImportValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("files")]
    public async Task<IActionResult> ImportFiles(
        [FromForm] ImportFilesRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateAccessAsync(
            request.OrganizationId,
            request.AnalysisWorkspaceId,
            cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        if (request.Files.Count > MaxFilesPerImport)
        {
            return BadRequest(new { message = _texts.TooManyImportFiles(MaxFilesPerImport) });
        }

        try
        {
            var result = await ParseImportRequestAsync(request, cancellationToken);

            foreach (var record in result.SalesRecords)
            {
                record.AnalysisWorkspaceId = request.AnalysisWorkspaceId;
            }

            foreach (var record in result.FinancialRecords)
            {
                record.AnalysisWorkspaceId = request.AnalysisWorkspaceId;
            }

            foreach (var record in result.EducationRecords)
            {
                record.AnalysisWorkspaceId = request.AnalysisWorkspaceId;
            }

            _db.SalesRecords.AddRange(result.SalesRecords);
            _db.FinancialRecords.AddRange(result.FinancialRecords);
            _db.EducationRecords.AddRange(result.EducationRecords);
            await _db.SaveChangesAsync(cancellationToken);

            var salesRecords = await _db.SalesRecords
                .AsNoTracking()
                .Where(record => record.OrganizationId == request.OrganizationId)
                .Where(record => !request.AnalysisWorkspaceId.HasValue || record.AnalysisWorkspaceId == request.AnalysisWorkspaceId)
                .ToListAsync(cancellationToken);

            var financialRecords = await _db.FinancialRecords
                .AsNoTracking()
                .Where(record => record.OrganizationId == request.OrganizationId)
                .Where(record => !request.AnalysisWorkspaceId.HasValue || record.AnalysisWorkspaceId == request.AnalysisWorkspaceId)
                .ToListAsync(cancellationToken);

            var educationRecords = await _db.EducationRecords
                .AsNoTracking()
                .Where(record => record.OrganizationId == request.OrganizationId)
                .Where(record => !request.AnalysisWorkspaceId.HasValue || record.AnalysisWorkspaceId == request.AnalysisWorkspaceId)
                .ToListAsync(cancellationToken);

            return Ok(new ImportFilesResponse
            {
                Message = _texts.ImportCompletedSuccessfully(result.ImportedFiles.Count, result.TotalRecords),
                Count = result.TotalRecords,
                FileCount = result.ImportedFiles.Count,
                Files = result.ImportedFiles,
                Analytics = _analyticsAggregationService.BuildAnalysis(salesRecords, financialRecords, educationRecords)
            });
        }
        catch (ImportProcessingService.ImportValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<CombinedImportResult> ParseImportRequestAsync(
        ImportFilesRequest request,
        CancellationToken cancellationToken)
    {
        var combinedResult = new CombinedImportResult([], [], [], []);

        foreach (var file in request.Files)
        {
            var fileResult = await ParseSingleImportFileAsync(
                file,
                request.OrganizationId,
                cancellationToken);

            combinedResult.SalesRecords.AddRange(fileResult.SalesRecords);
            combinedResult.FinancialRecords.AddRange(fileResult.FinancialRecords);
            combinedResult.EducationRecords.AddRange(fileResult.EducationRecords);
            combinedResult.ImportedFiles.AddRange(fileResult.ImportedFiles);
        }

        if (combinedResult.TotalRecords == 0)
        {
            throw new ImportProcessingService.ImportValidationException(_texts.ImportedFilesDoNotContainDataRows());
        }

        return combinedResult;
    }

    private async Task<CombinedImportResult> ParseSingleImportFileAsync(
        IFormFile file,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var salesRecords = new List<SalesRecord>();
        var financialRecords = new List<FinancialRecord>();
        var educationRecords = new List<EducationRecord>();
        ImportProcessingService.ImportValidationException? salesError = null;
        ImportProcessingService.ImportValidationException? reportError = null;

        try
        {
            var salesResult = await _importProcessingService.ParseFilesAsync(
                [file],
                organizationId,
                cancellationToken);

            salesRecords.AddRange(salesResult.Records);
        }
        catch (ImportProcessingService.ImportValidationException ex)
        {
            salesError = ex;
        }

        try
        {
            var reportResult = await _reportImportProcessingService.ParseFilesAsync(
                [file],
                organizationId,
                cancellationToken);

            financialRecords.AddRange(reportResult.FinancialRecords);
            educationRecords.AddRange(reportResult.EducationRecords);
        }
        catch (ImportProcessingService.ImportValidationException ex)
        {
            reportError = ex;
        }

        var totalRecords = salesRecords.Count + financialRecords.Count + educationRecords.Count;
        if (totalRecords == 0)
        {
            throw reportError ?? salesError ?? new ImportProcessingService.ImportValidationException(
                _texts.FileDoesNotContainDataRows(file.FileName));
        }

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToUpperInvariant();

        return new CombinedImportResult(
            salesRecords,
            financialRecords,
            educationRecords,
            [
                new ImportedFileResponse
                {
                    FileName = file.FileName,
                    Extension = extension,
                    ImportedRecords = totalRecords
                }
            ]);
    }

    private async Task<IActionResult?> ValidateAccessAsync(
        Guid organizationId,
        Guid? analysisWorkspaceId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var organization = await _organizationAccessService.GetOwnedOrganizationAsync(
            organizationId,
            userId.Value,
            cancellationToken);

        if (organization is null)
        {
            return NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() });
        }

        if (analysisWorkspaceId.HasValue)
        {
            var workspace = await _organizationAccessService.GetOwnedAnalysisWorkspaceAsync(
                analysisWorkspaceId.Value,
                userId.Value,
                cancellationToken);

            if (workspace is null || workspace.OrganizationId != organizationId)
            {
                return NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() });
            }
        }

        return null;
    }

    private sealed record CombinedImportResult(
        List<SalesRecord> SalesRecords,
        List<FinancialRecord> FinancialRecords,
        List<EducationRecord> EducationRecords,
        List<ImportedFileResponse> ImportedFiles)
    {
        public int TotalRecords => SalesRecords.Count + FinancialRecords.Count + EducationRecords.Count;
    }
}
