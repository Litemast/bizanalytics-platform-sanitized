namespace BizAnalytics.Api.Contracts.Analytics;

public class SourceComparisonResponse
{
    public string SourceName { get; set; } = string.Empty;
    public int RecordsCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageUnitPrice { get; set; }
}
