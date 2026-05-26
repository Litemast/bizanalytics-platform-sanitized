namespace BizAnalytics.Api.Contracts.Analytics;

public class AnalysisBundleResponse
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string ReportType { get; set; } = "sales_report";
    public SummaryResponse Summary { get; set; } = new();
    public List<RevenuePointResponse> Revenue { get; set; } = [];
    public List<TopProductResponse> TopProducts { get; set; } = [];
    public List<PriceTrendPointResponse> PriceTrends { get; set; } = [];
    public List<SourceComparisonResponse> SourceComparisons { get; set; } = [];
    public List<InsightResponse> Insights { get; set; } = [];
    public FinancialAnalysisResponse? Financial { get; set; }
    public EducationAnalysisResponse? Education { get; set; }
}
