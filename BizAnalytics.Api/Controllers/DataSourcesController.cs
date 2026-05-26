using BizAnalytics.Api.Contracts.DataSources;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Auth;
using BizAnalytics.Api.Infrastructure.Data;
using BizAnalytics.Api.Infrastructure.Localization;
using BizAnalytics.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataSourcesController : ControllerBase
{
    private static readonly string[] SupportedTypes = ["CSV", "DOCX", "XLS", "XLSX"];
    private readonly AppDbContext _db;
    private readonly OrganizationAccessService _organizationAccessService;
    private readonly IApiTextLocalizer _texts;

    public DataSourcesController(
        AppDbContext db,
        OrganizationAccessService organizationAccessService,
        IApiTextLocalizer texts)
    {
        _db = db;
        _organizationAccessService = organizationAccessService;
        _texts = texts;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDataSourceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var organization = await _organizationAccessService.GetOwnedOrganizationAsync(
            request.OrganizationId,
            userId.Value,
            cancellationToken);

        if (organization is null)
        {
            return NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() });
        }

        var normalizedType = request.Type.Trim().ToUpperInvariant();
        if (!SupportedTypes.Contains(normalizedType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = _texts.UnsupportedDataSourceType(string.Join(", ", SupportedTypes))
            });
        }

        var dataSource = new DataSource
        {
            OrganizationId = request.OrganizationId,
            Name = request.Name.Trim(),
            Type = normalizedType,
            SettingsJson = string.IsNullOrWhiteSpace(request.SettingsJson)
                ? null
                : request.SettingsJson.Trim()
        };

        _db.DataSources.Add(dataSource);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            dataSource.Id,
            dataSource.OrganizationId,
            dataSource.Name,
            dataSource.Type,
            dataSource.SettingsJson,
            dataSource.CreatedAt
        });
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<IActionResult> GetByOrg(Guid organizationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var organization = await _organizationAccessService.GetOwnedOrganizationAsync(
            organizationId,
            userId.Value,
            cancellationToken);

        if (organization is null)
        {
            return NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() });
        }

        var list = await _db.DataSources
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Type,
                x.SettingsJson,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(list);
    }
}
