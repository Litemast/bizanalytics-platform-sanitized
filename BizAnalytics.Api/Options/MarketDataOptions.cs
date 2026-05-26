namespace BizAnalytics.Api.Options;

public class MarketDataOptions
{
    public string FmpBaseUrl { get; set; } = "https://financialmodelingprep.com";
    public string FmpApiKey { get; set; } = string.Empty;
    public string AlphaVantageBaseUrl { get; set; } = "https://www.alphavantage.co";
    public string AlphaVantageApiKey { get; set; } = "demo";
    public List<string> TrackedSymbols { get; set; } = ["MSFT", "AAPL", "NVDA", "AMZN", "GOOGL", "META"];
}
