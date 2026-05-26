using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketController : ControllerBase
{
    private readonly MarketDataService _marketDataService;

    public MarketController(MarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken)
    {
        var currencies = await _marketDataService.GetCurrenciesAsync(cancellationToken);
        return Ok(currencies);
    }

    [HttpGet("currency-series")]
    public async Task<IActionResult> GetCurrencySeries(
        [FromQuery] string baseCurrency = "USD",
        [FromQuery] string quoteCurrency = "RUB",
        [FromQuery] int days = 30,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var series = await _marketDataService.GetCurrencySeriesAsync(
            baseCurrency,
            quoteCurrency,
            days,
            startDate,
            endDate,
            cancellationToken);

        return Ok(series);
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _marketDataService.GetCompaniesAsync(cancellationToken);
        return Ok(companies);
    }
}
