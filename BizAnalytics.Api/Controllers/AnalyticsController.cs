using BizAnalytics.Api.Contracts.Analytics;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Auth;
using BizAnalytics.Api.Infrastructure.Data;
using BizAnalytics.Api.Infrastructure.Localization;
using BizAnalytics.Api.Infrastructure.Security;
using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly OrganizationAccessService _organizationAccessService;
    private readonly IApiTextLocalizer _texts;
    private readonly AnalyticsAggregationService _analyticsAggregationService;

    public AnalyticsController(
        AppDbContext db,
        OrganizationAccessService organizationAccessService,
        IApiTextLocalizer texts,
        AnalyticsAggregationService analyticsAggregationService)
    {
        _db = db;
        _organizationAccessService = organizationAccessService;
        _texts = texts;
        _analyticsAggregationService = analyticsAggregationService;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? analysisWorkspaceId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateOrganizationAndRangeAsync(
            organizationId,
            analysisWorkspaceId,
            startDate,
            endDate,
            cancellationToken);

        if (validation.Error is not null)
        {
            return validation.Error;
        }

        var query = BuildSalesQuery(organizationId, analysisWorkspaceId, validation.StartDate, validation.EndDate);
        var data = await query
            .GroupBy(x => x.Date.Date)
            .Select(g => new RevenuePointResponse
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? analysisWorkspaceId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateOrganizationAndRangeAsync(
            organizationId,
            analysisWorkspaceId,
            startDate,
            endDate,
            cancellationToken);

        if (validation.Error is not null)
        {
            return validation.Error;
        }

        var query = BuildSalesQuery(organizationId, analysisWorkspaceId, validation.StartDate, validation.EndDate);
        var data = await query
            .GroupBy(x => x.ProductName)
            .Select(g => new TopProductResponse
            {
                ProductName = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ThenByDescending(x => x.TotalQuantity)
            .ThenBy(x => x.ProductName)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? analysisWorkspaceId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateOrganizationAndRangeAsync(
            organizationId,
            analysisWorkspaceId,
            startDate,
            endDate,
            cancellationToken);

        if (validation.Error is not null)
        {
            return validation.Error;
        }

        var query = BuildSalesQuery(organizationId, analysisWorkspaceId, validation.StartDate, validation.EndDate);
        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new SummaryResponse
            {
                TotalRevenue = g.Sum(x => x.Amount),
                TotalSalesCount = g.Count(),
                TotalQuantity = g.Sum(x => x.Quantity),
                AverageCheck = g.Sum(x => x.Amount) / g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(summary ?? new SummaryResponse());
    }

    [HttpGet("deep-dive")]
    public async Task<IActionResult> DeepDive(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? analysisWorkspaceId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateOrganizationAndRangeAsync(
            organizationId,
            analysisWorkspaceId,
            startDate,
            endDate,
            cancellationToken);

        if (validation.Error is not null)
        {
            return validation.Error;
        }

        var salesRecords = await BuildSalesQuery(organizationId, analysisWorkspaceId, validation.StartDate, validation.EndDate)
            .ToListAsync(cancellationToken);

        var financialRecords = await BuildFinancialQuery(organizationId, analysisWorkspaceId, validation.StartDate, validation.EndDate)
            .ToListAsync(cancellationToken);

        var educationRecords = await BuildEducationQuery(organizationId, analysisWorkspaceId)
            .ToListAsync(cancellationToken);

        return Ok(_analyticsAggregationService.BuildAnalysis(salesRecords, financialRecords, educationRecords));
    }

    [HttpDelete("reset")]
    public async Task<IActionResult> Reset(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? analysisWorkspaceId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateOrganizationAndRangeAsync(
            organizationId,
            analysisWorkspaceId,
            null,
            null,
            cancellationToken);

        if (validation.Error is not null)
        {
            return validation.Error;
        }

        var records = await _db.SalesRecords
            .Where(x => x.OrganizationId == organizationId)
            .Where(x => !analysisWorkspaceId.HasValue || x.AnalysisWorkspaceId == analysisWorkspaceId)
            .ToListAsync(cancellationToken);

        var financialRecords = await _db.FinancialRecords
            .Where(x => x.OrganizationId == organizationId)
            .Where(x => !analysisWorkspaceId.HasValue || x.AnalysisWorkspaceId == analysisWorkspaceId)
            .ToListAsync(cancellationToken);

        var educationRecords = await _db.EducationRecords
            .Where(x => x.OrganizationId == organizationId)
            .Where(x => !analysisWorkspaceId.HasValue || x.AnalysisWorkspaceId == analysisWorkspaceId)
            .ToListAsync(cancellationToken);

        var deletedCount = records.Count + financialRecords.Count + educationRecords.Count;

        if (deletedCount > 0)
        {
            _db.SalesRecords.RemoveRange(records);
            _db.FinancialRecords.RemoveRange(financialRecords);
            _db.EducationRecords.RemoveRange(educationRecords);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            message = _texts.AnalyticsResetSuccessfully(deletedCount),
            deletedCount
        });
    }

    private IQueryable<SalesRecord> BuildSalesQuery(
        Guid organizationId,
        Guid? analysisWorkspaceId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = _db.SalesRecords
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (analysisWorkspaceId.HasValue)
        {
            query = query.Where(x => x.AnalysisWorkspaceId == analysisWorkspaceId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.Date < endDate.Value.AddDays(1));
        }

        return query;
    }

    private IQueryable<FinancialRecord> BuildFinancialQuery(
        Guid organizationId,
        Guid? analysisWorkspaceId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = _db.FinancialRecords
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (analysisWorkspaceId.HasValue)
        {
            query = query.Where(x => x.AnalysisWorkspaceId == analysisWorkspaceId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.Period >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.Period < endDate.Value.AddDays(1));
        }

        return query;
    }

    private IQueryable<EducationRecord> BuildEducationQuery(
        Guid organizationId,
        Guid? analysisWorkspaceId)
    {
        var query = _db.EducationRecords
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (analysisWorkspaceId.HasValue)
        {
            query = query.Where(x => x.AnalysisWorkspaceId == analysisWorkspaceId);
        }

        return query;
    }

    private async Task<(DateTime? StartDate, DateTime? EndDate, IActionResult? Error)> ValidateOrganizationAndRangeAsync(
        Guid organizationId,
        Guid? analysisWorkspaceId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return (null, null, Unauthorized());
        }

        var organization = await _organizationAccessService.GetOwnedOrganizationAsync(
            organizationId,
            userId.Value,
            cancellationToken);

        if (organization is null)
        {
            return (null, null, NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() }));
        }

        if (analysisWorkspaceId.HasValue)
        {
            var workspace = await _organizationAccessService.GetOwnedAnalysisWorkspaceAsync(
                analysisWorkspaceId.Value,
                userId.Value,
                cancellationToken);

            if (workspace is null || workspace.OrganizationId != organizationId)
            {
                return (null, null, NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() }));
            }
        }

        var normalizedStart = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;

        var normalizedEnd = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;

        if (normalizedStart.HasValue && normalizedEnd.HasValue && normalizedStart > normalizedEnd)
        {
            return (null, null, BadRequest(new
            {
                message = _texts.StartDateCannotBeLaterThanEndDate()
            }));
        }

        return (normalizedStart, normalizedEnd, null);
    }
}
