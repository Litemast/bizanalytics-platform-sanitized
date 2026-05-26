namespace BizAnalytics.Api.Contracts.Market;

public class CompanyMarketResponse
{
    public string Provider { get; set; } = string.Empty;
    public string RefreshMode { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public List<CompanyQuoteResponse> Companies { get; set; } = [];
}

public class CompanyQuoteResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long? Volume { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }
}
