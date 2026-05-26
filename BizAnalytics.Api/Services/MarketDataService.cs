using BizAnalytics.Api.Contracts.Market;
using BizAnalytics.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text;

namespace BizAnalytics.Api.Services;

public class MarketDataService
{
    private static readonly DateTime EarliestCurrencyRateDate = new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MaxTrackedQuoteAge = TimeSpan.FromDays(5);
    private static readonly ConcurrentDictionary<string, CurrencyMarketResponse> CurrencySeriesCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CompanyQuoteResponse> TrackedCompanyQuoteCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CompanyCacheLock = new();
    private static CompanyMarketResponse? CachedCompanyResponse;
    private static readonly Dictionary<string, string> CompanyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MSFT"] = "Microsoft",
        ["AAPL"] = "Apple",
        ["NVDA"] = "NVIDIA",
        ["AMZN"] = "Amazon",
        ["GOOGL"] = "Alphabet",
        ["META"] = "Meta"
    };

    private static readonly Dictionary<string, CompanyQuoteResponse> SeedTrackedCompanyQuotes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MSFT"] = new CompanyQuoteResponse
        {
            Symbol = "MSFT",
            Name = "Microsoft",
            Price = 415.75m,
            Change = -17.17m,
            ChangePercent = -3.9661m,
            Volume = 38307959,
            LastUpdatedUtc = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc)
        },
        ["AAPL"] = new CompanyQuoteResponse
        {
            Symbol = "AAPL",
            Name = "Apple",
            Price = 273.43m,
            Change = 0.26m,
            ChangePercent = 0.10m,
            Volume = 33399639,
            LastUpdatedUtc = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc)
        },
        ["NVDA"] = new CompanyQuoteResponse
        {
            Symbol = "NVDA",
            Name = "NVIDIA Corporation",
            Price = 199.64m,
            Change = -2.86m,
            ChangePercent = 0m,
            Volume = 108930898,
            LastUpdatedUtc = new DateTime(2026, 4, 23, 20, 0, 0, DateTimeKind.Utc)
        },
        ["AMZN"] = new CompanyQuoteResponse
        {
            Symbol = "AMZN",
            Name = "Amazon",
            Price = 255.08m,
            Change = -0.28m,
            ChangePercent = -0.1096m,
            Volume = 39091394,
            LastUpdatedUtc = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc)
        },
        ["GOOGL"] = new CompanyQuoteResponse
        {
            Symbol = "GOOGL",
            Name = "Alphabet Inc.",
            Price = 338.89m,
            Change = -0.43m,
            ChangePercent = 0m,
            Volume = 18458741,
            LastUpdatedUtc = new DateTime(2026, 4, 23, 20, 0, 1, DateTimeKind.Utc)
        },
        ["META"] = new CompanyQuoteResponse
        {
            Symbol = "META",
            Name = "Meta",
            Price = 659.15m,
            Change = -15.57m,
            ChangePercent = -2.3076m,
            Volume = 11666981,
            LastUpdatedUtc = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc)
        }
    };

    private static readonly Dictionary<string, string> RussianCurrencyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "Доллар США",
        ["EUR"] = "Евро",
        ["RUB"] = "Российский рубль",
        ["KZT"] = "Казахстанский тенге",
        ["CNY"] = "Китайский юань",
        ["GBP"] = "Фунт стерлингов",
        ["JPY"] = "Японская иена",
        ["CHF"] = "Швейцарский франк",
        ["TRY"] = "Турецкая лира",
        ["AED"] = "Дирхам ОАЭ",
        ["UZS"] = "Узбекский сум",
        ["BYN"] = "Белорусский рубль",
        ["KGS"] = "Киргизский сом",
        ["UAH"] = "Украинская гривна",
        ["INR"] = "Индийская рупия",
        ["CAD"] = "Канадский доллар",
        ["AUD"] = "Австралийский доллар",
        ["SEK"] = "Шведская крона",
        ["NOK"] = "Норвежская крона",
        ["PLN"] = "Польский злотый"
    };

    private static readonly Dictionary<string, string> CleanRussianCurrencyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "Доллар США",
        ["EUR"] = "Евро",
        ["RUB"] = "Российский рубль",
        ["KZT"] = "Казахстанский тенге",
        ["CNY"] = "Китайский юань",
        ["GBP"] = "Фунт стерлингов",
        ["JPY"] = "Японская иена",
        ["CHF"] = "Швейцарский франк",
        ["TRY"] = "Турецкая лира",
        ["AED"] = "Дирхам ОАЭ",
        ["UZS"] = "Узбекский сум",
        ["BYN"] = "Белорусский рубль",
        ["KGS"] = "Киргизский сом",
        ["UAH"] = "Украинская гривна",
        ["INR"] = "Индийская рупия",
        ["CAD"] = "Канадский доллар",
        ["AUD"] = "Австралийский доллар",
        ["SEK"] = "Шведская крона",
        ["NOK"] = "Норвежская крона",
        ["PLN"] = "Польский злотый"
    };

    private readonly HttpClient _httpClient;
    private readonly MarketDataOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MarketDataService(
        HttpClient httpClient,
        IOptions<MarketDataOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<CurrencyOptionResponse>> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "https://api.frankfurter.dev/v2/currencies",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return document.RootElement
                .EnumerateArray()
                .Select(item => new CurrencyOptionResponse
                {
                    Code = item.GetProperty("iso_code").GetString() ?? string.Empty,
                    Name = LocalizeCurrencyName(
                        item.GetProperty("iso_code").GetString() ?? string.Empty,
                        item.GetProperty("name").GetString() ?? string.Empty)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Code))
                .OrderBy(item => item.Code)
                .ToList();
        }
        catch
        {
            return BuildFallbackCurrencyOptions();
        }
    }

    public async Task<CurrencyMarketResponse> GetCurrencySeriesAsync(
        string baseCurrency,
        string quoteCurrency,
        int days,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        baseCurrency = baseCurrency.Trim().ToUpperInvariant();
        quoteCurrency = quoteCurrency.Trim().ToUpperInvariant();
        var (from, to, totalDays) = ResolveCurrencyPeriod(days, startDate, endDate);
        var cacheKey = $"{baseCurrency}:{quoteCurrency}:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";

        if (baseCurrency == quoteCurrency)
        {
            var flatPoints = Enumerable.Range(0, totalDays)
                .Select(offset => new CurrencyRatePointResponse
                {
                    Date = from.AddDays(offset),
                    Rate = 1m
                })
                .ToList();

            return new CurrencyMarketResponse
            {
                BaseCurrency = baseCurrency,
                QuoteCurrency = quoteCurrency,
                Points = flatPoints
            };
        }

        try
        {
            var url =
                $"https://api.frankfurter.dev/v2/rates?base={baseCurrency}&quotes={quoteCurrency}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var points = document.RootElement
                .EnumerateArray()
                .Select(item => new CurrencyRatePointResponse
                {
                    Date = DateTime.Parse(item.GetProperty("date").GetString() ?? string.Empty, CultureInfo.InvariantCulture),
                    Rate = item.GetProperty("rate").GetDecimal()
                })
                .OrderBy(item => item.Date)
                .ToList();

            var result = new CurrencyMarketResponse
            {
                BaseCurrency = baseCurrency,
                QuoteCurrency = quoteCurrency,
                Points = points
            };

            CurrencySeriesCache[cacheKey] = CloneCurrencySeries(result);
            return result;
        }
        catch
        {
            if (CurrencySeriesCache.TryGetValue(cacheKey, out var cachedSeries))
            {
                return CloneCurrencySeries(cachedSeries);
            }

            return new CurrencyMarketResponse
            {
                BaseCurrency = baseCurrency,
                QuoteCurrency = quoteCurrency,
                Points = []
            };
        }
    }

    private static (DateTime From, DateTime To, int TotalDays) ResolveCurrencyPeriod(
        int days,
        DateTime? startDate,
        DateTime? endDate)
    {
        var today = DateTime.UtcNow.Date;
        var resolvedTo = (endDate?.Date ?? today) > today
            ? today
            : endDate?.Date ?? today;

        var resolvedFrom = startDate?.Date ?? resolvedTo.AddDays(-Math.Clamp(days, 7, 180) + 1);

        if (resolvedFrom > resolvedTo)
        {
            resolvedFrom = resolvedTo;
        }

        var totalDays = (resolvedTo - resolvedFrom).Days + 1;
        if (totalDays < 2)
        {
            resolvedFrom = resolvedTo.AddDays(-1);
            totalDays = 2;
        }

        if (resolvedFrom < EarliestCurrencyRateDate)
        {
            resolvedFrom = EarliestCurrencyRateDate;
            totalDays = (resolvedTo - resolvedFrom).Days + 1;

            if (totalDays < 2)
            {
                resolvedTo = resolvedFrom.AddDays(1);
                totalDays = 2;
            }
        }

        return (resolvedFrom, resolvedTo, totalDays);
    }

    public async Task<CompanyMarketResponse> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        var primaryMarketMovers = await TryGetMarketMoversAsync(cancellationToken);
        var yahooTrackedQuotes = await TryGetYahooTrackedQuotesAsync(cancellationToken);
        if (HasAnyTrackedMarketData(yahooTrackedQuotes))
        {
            foreach (var quote in yahooTrackedQuotes.Where(HasTrackedMarketData))
            {
                TrackedCompanyQuoteCache[quote.Symbol] = CloneCompanyQuote(quote);
            }

            var response = new CompanyMarketResponse
            {
                Provider = "Yahoo Finance",
                RefreshMode = "tracked-leaders",
                Note = Translate(
                    "Котировки ведущих компаний загружаются через Yahoo Finance с автоматическим резервом на случай недоступности основного источника.",
                    "Leading company quotes are loaded through Yahoo Finance with an automatic fallback when the main provider is unavailable."),
                Companies = MergePreferredCompanies(yahooTrackedQuotes, primaryMarketMovers)
            };

            CacheCompanyResponse(response);
            return response;
        }

        var primaryTrackedQuotes = await TryGetTrackedQuotesAsync(cancellationToken);
        if (HasAnyTrackedMarketData(primaryTrackedQuotes))
        {
            var mergedCompanies = MergePreferredCompanies(primaryTrackedQuotes, primaryMarketMovers);
            mergedCompanies = MergePreferredCompanies(mergedCompanies, await TryGetFmpTrackedQuotesAsync(cancellationToken));
            mergedCompanies = MergePreferredCompanies(mergedCompanies, await TryGetFmpMostActivesAsync(cancellationToken));

            var response = new CompanyMarketResponse
            {
                Provider = "Alpha Vantage",
                RefreshMode = IsDemoKey() ? "demo-delayed" : "tracked-leaders",
                Note = IsDemoKey()
                    ? Translate(
                        "Р”Р»СЏ РїРѕР»РЅРѕСЃС‚СЊСЋ Р¶РёРІС‹С… РєРѕС‚РёСЂРѕРІРѕРє СѓРєР°Р¶РёС‚Рµ СЃРѕР±СЃС‚РІРµРЅРЅС‹Р№ Alpha Vantage API key РІ РЅР°СЃС‚СЂРѕР№РєР°С… API.",
                        "Provide your own Alpha Vantage API key in the API settings to unlock live quotes.")
                    : Translate(
                        "Р›РµРЅС‚Р° Р»РёРґРµСЂРѕРІ СЂС‹РЅРєР° РѕР±РЅРѕРІР»СЏРµС‚СЃСЏ С‡РµСЂРµР· Alpha Vantage.",
                        "Market leader quotes are refreshed through Alpha Vantage."),
                Companies = mergedCompanies
            };

            CacheCompanyResponse(response);
            return response;
        }

        if (primaryMarketMovers.Count > 0)
        {
            var response = new CompanyMarketResponse
            {
                Provider = "Alpha Vantage",
                RefreshMode = "market-movers",
                Note = Translate(
                    "РџРѕРєР°Р·Р°РЅС‹ СЃР°РјС‹Рµ Р°РєС‚РёРІРЅС‹Рµ С‚РёРєРµСЂС‹ РёР· СЂС‹РЅРѕС‡РЅРѕР№ Р»РµРЅС‚С‹ Alpha Vantage.",
                    "Showing the most actively traded tickers from the Alpha Vantage market feed."),
                Companies = primaryMarketMovers
            };

            CacheCompanyResponse(response);
            return response;
        }

        var fmpQuotes = await TryGetFmpTrackedQuotesAsync(cancellationToken);
        if (fmpQuotes.Count >= 2)
        {
            var response = new CompanyMarketResponse
            {
                Provider = "Financial Modeling Prep",
                RefreshMode = "tracked-leaders",
                Note = Translate(
                    "Котировки ведущих компаний загружаются через Financial Modeling Prep.",
                    "Leading company quotes are loaded through Financial Modeling Prep."),
                Companies = fmpQuotes
            };

            CacheCompanyResponse(response);
            return response;
        }

        var fmpMostActives = await TryGetFmpMostActivesAsync(cancellationToken);
        if (fmpMostActives.Count > 0)
        {
            var response = new CompanyMarketResponse
            {
                Provider = "Financial Modeling Prep",
                RefreshMode = "market-movers",
                Note = Translate(
                    "Показаны самые активные компании из рыночной ленты Financial Modeling Prep.",
                    "Showing the most active companies from the Financial Modeling Prep market feed."),
                Companies = fmpMostActives
            };

            CacheCompanyResponse(response);
            return response;
        }

        var trackedQuotes = await TryGetTrackedQuotesAsync(cancellationToken);
        if (HasAnyTrackedMarketData(trackedQuotes))
        {
            var supplementalMarketMovers = await TryGetMarketMoversAsync(cancellationToken);
            var response = new CompanyMarketResponse
            {
                Provider = "Alpha Vantage",
                RefreshMode = IsDemoKey() ? "demo-delayed" : "tracked-leaders",
                Note = IsDemoKey()
                    ? Translate(
                        "Для полностью живых котировок укажите собственный Alpha Vantage API key в настройках API.",
                        "Provide your own Alpha Vantage API key in the API settings to unlock live quotes.")
                    : Translate(
                        "Лента лидеров рынка обновляется через Alpha Vantage.",
                        "Market leader quotes are refreshed through Alpha Vantage."),
                Companies = MergePreferredCompanies(trackedQuotes, supplementalMarketMovers)
            };

            CacheCompanyResponse(response);
            return response;
        }

        var marketMovers = await TryGetMarketMoversAsync(cancellationToken);
        if (marketMovers.Count > 0)
        {
            var response = new CompanyMarketResponse
            {
                Provider = "Alpha Vantage",
                RefreshMode = "market-movers",
                Note = Translate(
                    "Показаны самые активные тикеры из рыночной ленты Alpha Vantage.",
                    "Showing the most actively traded tickers from the Alpha Vantage market feed."),
                Companies = marketMovers
            };

            CacheCompanyResponse(response);
            return response;
        }

        return TryGetCachedCompanyResponse() ?? new CompanyMarketResponse
        {
            Provider = HasFmpKey() ? "Financial Modeling Prep / Alpha Vantage" : "Alpha Vantage",
            RefreshMode = "unavailable",
            Note = Translate(
                "Внешний источник рыночных данных временно недоступен. Попробуйте обновить позже или укажите рабочий API-ключ FMP.",
                "The external market data provider is temporarily unavailable. Try refreshing later or configure a valid FMP API key."),
            Companies = []
        };
    }

    private async Task<List<CompanyQuoteResponse>> TryGetFmpTrackedQuotesAsync(CancellationToken cancellationToken)
    {
        if (!HasFmpKey())
        {
            return [];
        }

        try
        {
            var symbols = _options.TrackedSymbols
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            if (symbols.Count == 0)
            {
                return [];
            }

            var url =
                $"{_options.FmpBaseUrl.TrimEnd('/')}/api/v3/quote/{Uri.EscapeDataString(string.Join(",", symbols))}?apikey={Uri.EscapeDataString(_options.FmpApiKey)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement
                .EnumerateArray()
                .Select(MapFmpQuote)
                .Where(item => !string.IsNullOrWhiteSpace(item.Symbol))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<CompanyQuoteResponse>> TryGetFmpMostActivesAsync(CancellationToken cancellationToken)
    {
        if (!HasFmpKey())
        {
            return [];
        }

        try
        {
            var url =
                $"{_options.FmpBaseUrl.TrimEnd('/')}/api/v3/stock_market/actives?apikey={Uri.EscapeDataString(_options.FmpApiKey)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement
                .EnumerateArray()
                .Take(6)
                .Select(MapFmpQuote)
                .Where(item => !string.IsNullOrWhiteSpace(item.Symbol))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<CompanyQuoteResponse>> TryGetTrackedQuotesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await GetTrackedQuotesAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<CompanyQuoteResponse>> TryGetYahooTrackedQuotesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var symbols = _options.TrackedSymbols
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            if (symbols.Count == 0)
            {
                return [];
            }

            var url =
                $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={Uri.EscapeDataString(string.Join(",", symbols))}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("quoteResponse", out var quoteResponseElement) ||
                !quoteResponseElement.TryGetProperty("result", out var resultElement) ||
                resultElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return resultElement
                .EnumerateArray()
                .Select(MapYahooQuote)
                .Where(item => !string.IsNullOrWhiteSpace(item.Symbol))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<CompanyQuoteResponse>> TryGetMarketMoversAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await GetMarketMoversAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<CompanyQuoteResponse>> GetTrackedQuotesAsync(CancellationToken cancellationToken)
    {
        var symbols = _options.TrackedSymbols
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        var batchQuotes = (await TryGetBatchTrackedQuotesAsync(symbols, cancellationToken))
            .Where(HasTrackedMarketData)
            .GroupBy(quote => quote.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var results = new List<CompanyQuoteResponse>();

        foreach (var symbol in symbols)
        {
            if (batchQuotes.TryGetValue(symbol, out var batchQuote))
            {
                TrackedCompanyQuoteCache[symbol] = CloneCompanyQuote(batchQuote);
                results.Add(CloneCompanyQuote(batchQuote));
                continue;
            }

            var quote = await TryGetTrackedQuoteFromDailySeriesAsync(symbol, cancellationToken)
                ?? await TryGetGlobalQuoteAsync(symbol, cancellationToken)
                ?? await TryGetYahooChartQuoteAsync(symbol, cancellationToken)
                ?? await TryGetTrackedQuoteFromStooqAsync(symbol, cancellationToken);

            if (quote is not null && HasTrackedMarketData(quote))
            {
                TrackedCompanyQuoteCache[symbol] = CloneCompanyQuote(quote);
                results.Add(quote);
                continue;
            }

            if (TrackedCompanyQuoteCache.TryGetValue(symbol, out var cachedQuote))
            {
                if (HasTrackedMarketData(cachedQuote))
                {
                    results.Add(CloneCompanyQuote(cachedQuote));
                    continue;
                }
            }

            if (SeedTrackedCompanyQuotes.TryGetValue(symbol, out var seedQuote))
            {
                if (HasTrackedMarketData(seedQuote))
                {
                    TrackedCompanyQuoteCache[symbol] = CloneCompanyQuote(seedQuote);
                    results.Add(CloneCompanyQuote(seedQuote));
                    continue;
                }
            }

            results.Add(BuildTrackedCompanyPlaceholder(symbol));
        }

        return results;
    }

    private async Task<List<CompanyQuoteResponse>> TryGetBatchTrackedQuotesAsync(
        IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken)
    {
        if (symbols.Count == 0)
        {
            return [];
        }

        try
        {
            var url =
                $"{_options.AlphaVantageBaseUrl.TrimEnd('/')}/query?function=BATCH_STOCK_QUOTES&symbols={Uri.EscapeDataString(string.Join(",", symbols))}&apikey={_options.AlphaVantageApiKey}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!TryGetFirstProperty(document.RootElement, out var quotesElement, "Stock Quotes", "stock quotes") ||
                quotesElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var quotes = new List<CompanyQuoteResponse>();

            foreach (var item in quotesElement.EnumerateArray())
            {
                var symbol = GetFirstString(item, "1. symbol", "symbol", "ticker");
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    continue;
                }

                quotes.Add(new CompanyQuoteResponse
                {
                    Symbol = symbol,
                    Name = CompanyNames.TryGetValue(symbol, out var name) ? name : symbol,
                    Price = ParseFirstDecimal(item, "2. price", "price"),
                    Change = 0m,
                    ChangePercent = 0m,
                    Volume = ParseFirstLong(item, "3. volume", "volume"),
                    LastUpdatedUtc = ParseFirstDate(item, "4. timestamp", "timestamp")
                });
            }

            return quotes;
        }
        catch
        {
            return [];
        }
    }

    private async Task<CompanyQuoteResponse?> TryGetYahooChartQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=5d";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("chart", out var chartElement) ||
                !chartElement.TryGetProperty("result", out var resultElement) ||
                resultElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var firstResult = resultElement.EnumerateArray().FirstOrDefault();
            if (firstResult.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var metaElement = firstResult.TryGetProperty("meta", out var parsedMeta)
                ? parsedMeta
                : default;
            var timestamps = firstResult.TryGetProperty("timestamp", out var timestampElement) &&
                             timestampElement.ValueKind == JsonValueKind.Array
                ? timestampElement.EnumerateArray().ToList()
                : [];

            if (!firstResult.TryGetProperty("indicators", out var indicatorsElement) ||
                !indicatorsElement.TryGetProperty("quote", out var quoteArray) ||
                quoteArray.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var quoteElement = quoteArray.EnumerateArray().FirstOrDefault();
            if (quoteElement.ValueKind != JsonValueKind.Object ||
                !quoteElement.TryGetProperty("close", out var closeArray) ||
                closeArray.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var closes = closeArray.EnumerateArray().ToList();
            var volumes = quoteElement.TryGetProperty("volume", out var volumeArray) && volumeArray.ValueKind == JsonValueKind.Array
                ? volumeArray.EnumerateArray().ToList()
                : [];

            var points = new List<(DateTime Date, decimal Close, long? Volume)>();

            for (var index = 0; index < Math.Min(timestamps.Count, closes.Count); index++)
            {
                if (closes[index].ValueKind != JsonValueKind.Number || !closes[index].TryGetDecimal(out var close))
                {
                    continue;
                }

                var unixSeconds = timestamps[index].GetInt64();
                var date = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                long? volume = null;

                if (index < volumes.Count && volumes[index].ValueKind == JsonValueKind.Number && volumes[index].TryGetInt64(out var parsedVolume))
                {
                    volume = parsedVolume;
                }

                points.Add((date, close, volume));
            }

            if (points.Count == 0)
            {
                return null;
            }

            var latestPoint = points[^1];
            var previousPoint = points.Count > 1 ? points[^2] : ((DateTime Date, decimal Close, long? Volume)?)null;
            var change = previousPoint is null ? 0m : latestPoint.Close - previousPoint.Value.Close;
            var changePercent = previousPoint is null || previousPoint.Value.Close == 0m
                ? 0m
                : decimal.Round(change / previousPoint.Value.Close * 100m, 2);

            var resolvedName = metaElement.ValueKind == JsonValueKind.Object
                ? GetFirstString(metaElement, "longName", "shortName")
                : null;

            return new CompanyQuoteResponse
            {
                Symbol = symbol,
                Name = string.IsNullOrWhiteSpace(resolvedName)
                    ? (CompanyNames.TryGetValue(symbol, out var companyName) ? companyName : symbol)
                    : resolvedName,
                Price = latestPoint.Close,
                Change = change,
                ChangePercent = changePercent,
                Volume = latestPoint.Volume,
                LastUpdatedUtc = latestPoint.Date
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<CompanyQuoteResponse?> TryGetGlobalQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        var url = $"{_options.AlphaVantageBaseUrl.TrimEnd('/')}/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_options.AlphaVantageApiKey}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("Global Quote", out var quoteElement) ||
            quoteElement.ValueKind != JsonValueKind.Object ||
            !quoteElement.TryGetProperty("01. symbol", out _))
        {
            return null;
        }

        var lastUpdated = quoteElement.TryGetProperty("07. latest trading day", out var latestTradingDay)
            && DateTime.TryParse(latestTradingDay.GetString(), out var parsedDay)
            ? DateTime.SpecifyKind(parsedDay, DateTimeKind.Utc)
            : (DateTime?)null;

        return new CompanyQuoteResponse
        {
            Symbol = quoteElement.GetProperty("01. symbol").GetString() ?? symbol,
            Name = CompanyNames.TryGetValue(symbol, out var name) ? name : symbol,
            Price = ParseDecimal(quoteElement, "05. price"),
            Change = ParseDecimal(quoteElement, "09. change"),
            ChangePercent = ParsePercent(quoteElement, "10. change percent"),
            Volume = ParseLong(quoteElement, "06. volume"),
            LastUpdatedUtc = lastUpdated
        };
    }

    private async Task<CompanyQuoteResponse?> TryGetTrackedQuoteFromDailySeriesAsync(string symbol, CancellationToken cancellationToken)
    {
        var url =
            $"{_options.AlphaVantageBaseUrl.TrimEnd('/')}/query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=compact&apikey={_options.AlphaVantageApiKey}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("Time Series (Daily)", out var seriesElement) ||
            seriesElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var dailyPoints = seriesElement
            .EnumerateObject()
            .Select(item =>
            {
                var parsedDate = DateTime.TryParse(item.Name, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue)
                    ? DateTime.SpecifyKind(dateValue, DateTimeKind.Utc)
                    : (DateTime?)null;

                return new
                {
                    Date = parsedDate,
                    Close = ParseDecimal(item.Value, "4. close"),
                    Volume = ParseLong(item.Value, "5. volume")
                };
            })
            .Where(item => item.Date.HasValue)
            .OrderByDescending(item => item.Date!.Value)
            .Take(2)
            .ToList();

        if (dailyPoints.Count == 0)
        {
            return null;
        }

        var latestPoint = dailyPoints[0];
        var previousPoint = dailyPoints.Count > 1 ? dailyPoints[1] : null;
        var change = previousPoint is null ? 0m : latestPoint.Close - previousPoint.Close;
        var changePercent = previousPoint is null || previousPoint.Close == 0m
            ? 0m
            : decimal.Round(change / previousPoint.Close * 100m, 2);

        return new CompanyQuoteResponse
        {
            Symbol = symbol,
            Name = CompanyNames.TryGetValue(symbol, out var name) ? name : symbol,
            Price = latestPoint.Close,
            Change = change,
            ChangePercent = changePercent,
            Volume = latestPoint.Volume,
            LastUpdatedUtc = latestPoint.Date
        };
    }

    private async Task<CompanyQuoteResponse?> TryGetTrackedQuoteFromStooqAsync(string symbol, CancellationToken cancellationToken)
    {
        var stooqSymbol = $"{symbol.Trim().ToLowerInvariant()}.us";
        var url = $"https://stooq.com/q/d/l/?s={Uri.EscapeDataString(stooqSymbol)}&i=d";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var csv = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(csv))
        {
            return null;
        }

        var rows = csv
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .Select(ParseStooqDailyRow)
            .Where(item => item is not null)
            .Cast<(DateTime Date, decimal Close, long? Volume)>()
            .OrderByDescending(item => item.Date)
            .Take(2)
            .ToList();

        if (rows.Count == 0)
        {
            return null;
        }

        var latestPoint = rows[0];
        var previousPoint = rows.Count > 1 ? rows[1] : ((DateTime Date, decimal Close, long? Volume)?)null;
        var change = previousPoint is null ? 0m : latestPoint.Close - previousPoint.Value.Close;
        var changePercent = previousPoint is null || previousPoint.Value.Close == 0m
            ? 0m
            : decimal.Round(change / previousPoint.Value.Close * 100m, 2);

        return new CompanyQuoteResponse
        {
            Symbol = symbol,
            Name = CompanyNames.TryGetValue(symbol, out var name) ? name : symbol,
            Price = latestPoint.Close,
            Change = change,
            ChangePercent = changePercent,
            Volume = latestPoint.Volume,
            LastUpdatedUtc = latestPoint.Date
        };
    }

    private async Task<List<CompanyQuoteResponse>> GetMarketMoversAsync(CancellationToken cancellationToken)
    {
        var entitlement = IsDemoKey() ? string.Empty : "&entitlement=delayed";
        var url =
            $"{_options.AlphaVantageBaseUrl.TrimEnd('/')}/query?function=TOP_GAINERS_LOSERS{entitlement}&apikey={_options.AlphaVantageApiKey}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("most_actively_traded", out var activeElement) ||
            activeElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return activeElement
            .EnumerateArray()
            .Take(6)
            .Select(item => new CompanyQuoteResponse
            {
                Symbol = item.GetProperty("ticker").GetString() ?? string.Empty,
                Name = item.GetProperty("ticker").GetString() ?? string.Empty,
                Price = ParseDecimal(item, "price"),
                Change = ParseDecimal(item, "change_amount"),
                ChangePercent = ParsePercent(item, "change_percentage"),
                Volume = ParseLong(item, "volume"),
                LastUpdatedUtc = DateTime.UtcNow
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Symbol))
            .ToList();
    }

    private bool IsDemoKey()
    {
        return string.Equals(_options.AlphaVantageApiKey, "demo", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasFmpKey()
    {
        return !string.IsNullOrWhiteSpace(_options.FmpApiKey);
    }

    private string Translate(string russian, string english)
    {
        return IsEnglishRequested() ? english : russian;
    }

    private string LocalizeCurrencyName(string code, string defaultName)
    {
        if (IsEnglishRequested())
        {
            return defaultName;
        }

        return CleanRussianCurrencyNames.TryGetValue(code, out var localizedName)
            ? localizedName
            : defaultName;
    }

    private bool IsEnglishRequested()
    {
        var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        var firstLanguage = acceptLanguage
            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLanguage?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
    }

    private decimal ParseDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0m;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return decimal.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue)
            ? decimalValue
            : 0m;
    }

    private decimal ParsePercent(JsonElement element, string propertyName)
    {
        var raw = element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;

        raw = raw?.Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private long? ParseLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        return long.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out longValue)
            ? longValue
            : null;
    }

    private static bool TryGetFirstProperty(JsonElement element, out JsonElement value, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? GetFirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            else if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return property.ToString();
            }
        }

        return null;
    }

    private static decimal ParseFirstDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (decimal.TryParse(property.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
            {
                return decimalValue;
            }
        }

        return 0m;
    }

    private static long? ParseFirstLong(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var longValue))
            {
                return longValue;
            }

            if (long.TryParse(property.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out longValue))
            {
                return longValue;
            }
        }

        return null;
    }

    private static DateTime? ParseFirstDate(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var unixSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            }

            if (DateTime.TryParse(property.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        return null;
    }

    private (DateTime Date, decimal Close, long? Volume)? ParseStooqDailyRow(string row)
    {
        var parts = row.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 6 ||
            !DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ||
            !decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var closePrice))
        {
            return null;
        }

        long? volume = long.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVolume)
            ? parsedVolume
            : null;

        return (DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc), closePrice, volume);
    }

    private DateTime? ParseUnixTimestamp(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }

        return long.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out unixSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime
            : null;
    }

    private CompanyQuoteResponse MapFmpQuote(JsonElement item)
    {
        var resolvedSymbol =
            item.TryGetProperty("symbol", out var symbolElement)
                ? symbolElement.GetString()
                : item.TryGetProperty("ticker", out var tickerElement)
                    ? tickerElement.GetString()
                    : string.Empty;

        var companyName =
            item.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : item.TryGetProperty("companyName", out var companyNameElement)
                    ? companyNameElement.GetString()
                    : null;

        var timestamp =
            item.TryGetProperty("timestamp", out var timestampElement)
                ? ParseUnixTimestamp(timestampElement)
                : null;

        return new CompanyQuoteResponse
        {
            Symbol = resolvedSymbol ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(companyName)
                ? (CompanyNames.TryGetValue(resolvedSymbol ?? string.Empty, out var knownName)
                    ? knownName
                    : resolvedSymbol ?? string.Empty)
                : companyName,
            Price = ParseDecimal(item, "price"),
            Change = ParseDecimal(item, "change"),
            ChangePercent = ParsePercent(item, "changesPercentage"),
            Volume = ParseLong(item, "volume"),
            LastUpdatedUtc = timestamp ?? DateTime.UtcNow
        };
    }

    private CompanyQuoteResponse MapYahooQuote(JsonElement item)
    {
        var resolvedSymbol =
            item.TryGetProperty("symbol", out var symbolElement)
                ? symbolElement.GetString()
                : string.Empty;

        var companyName =
            item.TryGetProperty("shortName", out var shortNameElement)
                ? shortNameElement.GetString()
                : item.TryGetProperty("longName", out var longNameElement)
                    ? longNameElement.GetString()
                    : null;

        var timestamp =
            item.TryGetProperty("regularMarketTime", out var timestampElement)
                ? ParseUnixTimestamp(timestampElement)
                : null;

        return new CompanyQuoteResponse
        {
            Symbol = resolvedSymbol ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(companyName)
                ? (CompanyNames.TryGetValue(resolvedSymbol ?? string.Empty, out var knownName)
                    ? knownName
                    : resolvedSymbol ?? string.Empty)
                : companyName,
            Price = ParseDecimal(item, "regularMarketPrice"),
            Change = ParseDecimal(item, "regularMarketChange"),
            ChangePercent = ParseDecimal(item, "regularMarketChangePercent"),
            Volume = ParseLong(item, "regularMarketVolume"),
            LastUpdatedUtc = timestamp ?? DateTime.UtcNow
        };
    }

    private static bool HasAnyTrackedMarketData(IEnumerable<CompanyQuoteResponse> quotes)
    {
        return quotes.Any(HasTrackedMarketData);
    }

    private static bool HasTrackedMarketData(CompanyQuoteResponse quote)
    {
        if (quote.Price <= 0m || !quote.LastUpdatedUtc.HasValue)
        {
            return false;
        }

        var lastUpdatedUtc = NormalizeUtc(quote.LastUpdatedUtc.Value);
        var utcNow = DateTime.UtcNow;

        if (lastUpdatedUtc > utcNow.AddHours(6))
        {
            return false;
        }

        return utcNow - lastUpdatedUtc <= MaxTrackedQuoteAge;
    }

    private static List<CompanyQuoteResponse> MergePreferredCompanies(
        IEnumerable<CompanyQuoteResponse> preferredQuotes,
        IEnumerable<CompanyQuoteResponse> fallbackQuotes)
    {
        var merged = new List<CompanyQuoteResponse>();
        var seenSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var quote in preferredQuotes.Where(HasTrackedMarketData))
        {
            if (seenSymbols.Add(quote.Symbol))
            {
                merged.Add(CloneCompanyQuote(quote));
            }
        }

        foreach (var quote in fallbackQuotes.Where(HasTrackedMarketData))
        {
            if (merged.Count >= 6)
            {
                break;
            }

            if (seenSymbols.Add(quote.Symbol))
            {
                merged.Add(CloneCompanyQuote(quote));
            }
        }

        return merged;
    }

    private static CompanyQuoteResponse BuildTrackedCompanyPlaceholder(string symbol)
    {
        return new CompanyQuoteResponse
        {
            Symbol = symbol,
            Name = CompanyNames.TryGetValue(symbol, out var knownName) ? knownName : symbol,
            Price = 0m,
            Change = 0m,
            ChangePercent = 0m,
            Volume = null,
            LastUpdatedUtc = null
        };
    }

    private static CompanyQuoteResponse CloneCompanyQuote(CompanyQuoteResponse source)
    {
        return new CompanyQuoteResponse
        {
            Symbol = source.Symbol,
            Name = source.Name,
            Price = source.Price,
            Change = source.Change,
            ChangePercent = source.ChangePercent,
            Volume = source.Volume,
            LastUpdatedUtc = source.LastUpdatedUtc
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private List<CurrencyOptionResponse> BuildFallbackCurrencyOptions()
    {
        var englishFallbackNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = "US Dollar",
            ["EUR"] = "Euro",
            ["RUB"] = "Russian Ruble",
            ["KZT"] = "Kazakhstani Tenge",
            ["CNY"] = "Chinese Yuan",
            ["GBP"] = "British Pound",
            ["JPY"] = "Japanese Yen",
            ["CHF"] = "Swiss Franc",
            ["TRY"] = "Turkish Lira",
            ["AED"] = "UAE Dirham",
            ["UZS"] = "Uzbekistani Som",
            ["BYN"] = "Belarusian Ruble",
            ["KGS"] = "Kyrgyzstani Som",
            ["UAH"] = "Ukrainian Hryvnia",
            ["INR"] = "Indian Rupee",
            ["CAD"] = "Canadian Dollar",
            ["AUD"] = "Australian Dollar",
            ["SEK"] = "Swedish Krona",
            ["NOK"] = "Norwegian Krone",
            ["PLN"] = "Polish Zloty"
        };

        return englishFallbackNames
            .Select(item => new CurrencyOptionResponse
            {
                Code = item.Key,
                Name = LocalizeCurrencyName(item.Key, item.Value)
            })
            .OrderBy(item => item.Code)
            .ToList();
    }

    private static CurrencyMarketResponse CloneCurrencySeries(CurrencyMarketResponse source)
    {
        return new CurrencyMarketResponse
        {
            BaseCurrency = source.BaseCurrency,
            QuoteCurrency = source.QuoteCurrency,
            Points = source.Points
                .Select(point => new CurrencyRatePointResponse
                {
                    Date = point.Date,
                    Rate = point.Rate
                })
                .ToList()
        };
    }

    private void CacheCompanyResponse(CompanyMarketResponse response)
    {
        lock (CompanyCacheLock)
        {
            CachedCompanyResponse = CloneCompanyResponse(response);
        }
    }

    private CompanyMarketResponse? TryGetCachedCompanyResponse()
    {
        lock (CompanyCacheLock)
        {
            if (CachedCompanyResponse is null)
            {
                return null;
            }

            var cachedResponse = CloneCompanyResponse(CachedCompanyResponse);
            cachedResponse.Companies = cachedResponse.Companies
                .Where(HasTrackedMarketData)
                .Select(CloneCompanyQuote)
                .ToList();

            return cachedResponse.Companies.Count == 0 ? null : cachedResponse;
        }
    }

    private static CompanyMarketResponse CloneCompanyResponse(CompanyMarketResponse source)
    {
        return new CompanyMarketResponse
        {
            Provider = source.Provider,
            RefreshMode = source.RefreshMode,
            Note = source.Note,
            Companies = source.Companies
                .Select(company => new CompanyQuoteResponse
                {
                    Symbol = company.Symbol,
                    Name = company.Name,
                    Price = company.Price,
                    Change = company.Change,
                    ChangePercent = company.ChangePercent,
                    Volume = company.Volume,
                    LastUpdatedUtc = company.LastUpdatedUtc
                })
                .ToList()
        };
    }
}
