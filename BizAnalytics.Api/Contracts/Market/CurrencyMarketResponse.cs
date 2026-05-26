namespace BizAnalytics.Api.Contracts.Market;

public class CurrencyMarketResponse
{
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public List<CurrencyRatePointResponse> Points { get; set; } = [];
}

public class CurrencyRatePointResponse
{
    public DateTime Date { get; set; }
    public decimal Rate { get; set; }
}

public class CurrencyOptionResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
