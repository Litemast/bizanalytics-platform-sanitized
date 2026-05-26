using BizAnalytics.Api.Contracts.Analytics;
using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.Reports;

public class GenerateAnalyticsReportRequest
{
    [Required]
    [StringLength(200)]
    public string OrganizationName { get; set; } = string.Empty;

    [Required]
    public AnalysisBundleResponse Analytics { get; set; } = new();

    public string Language { get; set; } = "ru";
    public string? GeneratedFor { get; set; }
    public string? AnalysisName { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}
