namespace BizAnalytics.Api.Contracts.Analytics;

public class InsightResponse
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tone { get; set; } = "info";
}
