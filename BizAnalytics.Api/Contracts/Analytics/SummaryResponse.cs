namespace BizAnalytics.Api.Contracts.Analytics;

public class SummaryResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalSalesCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal AverageCheck { get; set; }
}
