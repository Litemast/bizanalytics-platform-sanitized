namespace BizAnalytics.Api.Contracts.Analytics;

public class TopProductResponse
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}
