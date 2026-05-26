using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IndividualEntrepreneursController : ControllerBase
{
    private readonly IndividualEntrepreneurRegistryService _registryService;

    public IndividualEntrepreneursController(IndividualEntrepreneurRegistryService registryService)
    {
        _registryService = registryService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string iin, CancellationToken cancellationToken)
    {
        var normalizedIin = new string((iin ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalizedIin.Length != 12)
        {
            return BadRequest(new
            {
                message = IsEnglishRequested()
                    ? "Enter a valid 12-digit Kazakhstan IIN."
                    : "Введите корректный 12-значный ИИН Казахстана."
            });
        }

        var result = await _registryService.SearchAsync(normalizedIin, cancellationToken);
        return Ok(result);
    }

    private bool IsEnglishRequested()
    {
        var acceptLanguage = HttpContext.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        var firstLanguage = acceptLanguage
            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLanguage?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
    }
}
