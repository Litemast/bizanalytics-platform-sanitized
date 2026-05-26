using BizAnalytics.Api.Contracts.Reports;
using BizAnalytics.Api.Infrastructure.Localization;
using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AnalyticsReportService _analyticsReportService;
    private readonly EntrepreneurTaxFormReportService _entrepreneurTaxFormReportService;
    private readonly IApiTextLocalizer _texts;

    public ReportsController(
        AnalyticsReportService analyticsReportService,
        EntrepreneurTaxFormReportService entrepreneurTaxFormReportService,
        IApiTextLocalizer texts)
    {
        _analyticsReportService = analyticsReportService;
        _entrepreneurTaxFormReportService = entrepreneurTaxFormReportService;
        _texts = texts;
    }

    [HttpPost("analytics-pdf")]
    public IActionResult GenerateAnalyticsPdf([FromBody] GenerateAnalyticsReportRequest request)
    {
        if (request.Analytics is null)
        {
            return BadRequest(new { message = _texts.ReportPayloadIsMissing() });
        }

        var bytes = _analyticsReportService.GeneratePdf(request);
        var fileName = "BizAnalitics report.pdf";

        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost("entrepreneur-form-pdf")]
    public IActionResult GenerateEntrepreneurFormPdf([FromBody] GenerateEntrepreneurFormPdfRequest request)
    {
        if (request.Registry is null)
        {
            return BadRequest(new { message = _texts.ReportPayloadIsMissing() });
        }

        var bytes = _entrepreneurTaxFormReportService.GeneratePdf(request);
        var fileName = $"Entrepreneur-{request.FormCode}.pdf";

        return File(bytes, "application/pdf", fileName);
    }
}
