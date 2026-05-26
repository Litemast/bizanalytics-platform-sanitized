using BizAnalytics.Api.Options;
using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Text;
using Xunit;

namespace BizAnalytics.Api.Tests;

public class MarketDataServiceTests
{
    [Fact]
    public async Task GetCompaniesAsync_Uses_Fresh_Fallbacks_Instead_Of_Stale_SeedQuotes()
    {
        ResetMarketDataServiceState();

        var now = DateTime.UtcNow;
        var latestTradingDay = now.Date.AddDays(-1);
        var previousTradingDay = latestTradingDay.AddDays(-1);
        var fmpTimestamp = new DateTimeOffset(now).ToUnixTimeSeconds();

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;

            if (url.Contains("TOP_GAINERS_LOSERS", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"most_actively_traded":[]}""");
            }

            if (url.Contains("query1.finance.yahoo.com/v7/finance/quote", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"quoteResponse":{"result":[]}}""");
            }

            if (url.Contains("/api/v3/quote/", StringComparison.OrdinalIgnoreCase))
            {
                var payload =
                    $$"""
                    [
                      {
                        "symbol": "MSFT",
                        "name": "Microsoft",
                        "price": 499.42,
                        "change": 2.14,
                        "changesPercentage": "0.43%",
                        "volume": 45234000,
                        "timestamp": {{fmpTimestamp}}
                      }
                    ]
                    """;

                return Json(payload);
            }

            if (url.Contains("TIME_SERIES_DAILY&symbol=AAPL", StringComparison.OrdinalIgnoreCase))
            {
                var payload =
                    $$"""
                    {
                      "Time Series (Daily)": {
                        "{{latestTradingDay:yyyy-MM-dd}}": {
                          "4. close": "203.11",
                          "5. volume": "32100000"
                        },
                        "{{previousTradingDay:yyyy-MM-dd}}": {
                          "4. close": "200.01",
                          "5. volume": "29800000"
                        }
                      }
                    }
                    """;

                return Json(payload);
            }

            if (url.Contains("TIME_SERIES_DAILY&symbol=MSFT", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("function=GLOBAL_QUOTE", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("query1.finance.yahoo.com/v8/finance/chart/", StringComparison.OrdinalIgnoreCase))
            {
                return Json("{}");
            }

            if (url.Contains("stooq.com", StringComparison.OrdinalIgnoreCase))
            {
                return Text("Date,Open,High,Low,Close,Volume\n");
            }

            return Json("{}");
        }));

        var options = Microsoft.Extensions.Options.Options.Create(new MarketDataOptions
        {
            AlphaVantageApiKey = "test-alpha-key",
            FmpApiKey = "test-fmp-key",
            TrackedSymbols = ["MSFT", "AAPL"]
        });

        var service = new MarketDataService(httpClient, options, new HttpContextAccessor());

        var response = await service.GetCompaniesAsync(CancellationToken.None);

        Assert.Equal("tracked-leaders", response.RefreshMode);
        Assert.Contains(response.Companies, quote => quote.Symbol == "MSFT" && quote.Price == 499.42m);
        Assert.Contains(response.Companies, quote => quote.Symbol == "AAPL" && quote.Price == 203.11m);
        Assert.DoesNotContain(response.Companies, quote =>
            quote.Symbol == "MSFT" &&
            quote.LastUpdatedUtc.HasValue &&
            quote.LastUpdatedUtc.Value < now.AddDays(-5));
    }

    [Fact]
    public async Task GetCompaniesAsync_Returns_Unavailable_When_Only_Stale_SeedQuotes_Exist()
    {
        ResetMarketDataServiceState();

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;

            if (url.Contains("TOP_GAINERS_LOSERS", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"most_actively_traded":[]}""");
            }

            if (url.Contains("query1.finance.yahoo.com/v7/finance/quote", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"quoteResponse":{"result":[]}}""");
            }

            if (url.Contains("stooq.com", StringComparison.OrdinalIgnoreCase))
            {
                return Text("Date,Open,High,Low,Close,Volume\n");
            }

            return Json("{}");
        }));

        var options = Microsoft.Extensions.Options.Options.Create(new MarketDataOptions
        {
            AlphaVantageApiKey = "test-alpha-key",
            FmpApiKey = string.Empty,
            TrackedSymbols = ["MSFT"]
        });

        var service = new MarketDataService(httpClient, options, new HttpContextAccessor());

        var response = await service.GetCompaniesAsync(CancellationToken.None);

        Assert.Equal("unavailable", response.RefreshMode);
        Assert.Empty(response.Companies);
    }

    private static HttpResponseMessage Json(string payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage Text(string payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain")
        };
    }

    private static void ResetMarketDataServiceState()
    {
        var serviceType = typeof(MarketDataService);

        var trackedCacheField = serviceType.GetField("TrackedCompanyQuoteCache", BindingFlags.Static | BindingFlags.NonPublic);
        trackedCacheField?.FieldType.GetMethod("Clear")?.Invoke(trackedCacheField.GetValue(null), null);

        var cachedResponseField = serviceType.GetField("CachedCompanyResponse", BindingFlags.Static | BindingFlags.NonPublic);
        cachedResponseField?.SetValue(null, null);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
