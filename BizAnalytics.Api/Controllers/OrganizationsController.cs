using BizAnalytics.Api.Contracts.Organizations;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Auth;
using BizAnalytics.Api.Infrastructure.Data;
using BizAnalytics.Api.Infrastructure.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IApiTextLocalizer _texts;

    public OrganizationsController(AppDbContext db, IApiTextLocalizer texts)
    {
        _db = db;
        _texts = texts;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetUserId();
        if (ownerUserId is null)
        {
            return Unauthorized();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = _texts.OrganizationNameIsRequired() });
        }

        var organization = new Organization
        {
            Name = name,
            OwnerUserId = ownerUserId.Value
        };

        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(organization));
    }

    [HttpPut("{organizationId:guid}")]
    public async Task<IActionResult> Update(
        Guid organizationId,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetUserId();
        if (ownerUserId is null)
        {
            return Unauthorized();
        }

        var organization = await GetOwnedOrganizationAsync(organizationId, ownerUserId.Value, cancellationToken);
        if (organization is null)
        {
            return NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() });
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = _texts.OrganizationNameIsRequired() });
        }

        organization.Name = name;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = _texts.OrganizationUpdatedSuccessfully(),
            organization = ToResponse(organization)
        });
    }

    [HttpDelete("{organizationId:guid}")]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetUserId();
        if (ownerUserId is null)
        {
            return Unauthorized();
        }

        var organization = await GetOwnedOrganizationAsync(organizationId, ownerUserId.Value, cancellationToken);
        if (organization is null)
        {
            return NotFound(new { message = _texts.OrganizationWasNotFoundOrIsNotAccessible() });
        }

        _db.Organizations.Remove(organization);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = _texts.OrganizationDeletedSuccessfully() });
    }

    [HttpGet]
    public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetUserId();
        if (ownerUserId is null)
        {
            return Unauthorized();
        }

        var organizations = await _db.Organizations
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId.Value)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.OwnerUserId
            })
            .ToListAsync(cancellationToken);

        return Ok(organizations);
    }

    private Task<Organization?> GetOwnedOrganizationAsync(Guid organizationId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        return _db.Organizations.FirstOrDefaultAsync(
            x => x.Id == organizationId && x.OwnerUserId == ownerUserId,
            cancellationToken);
    }

    private static object ToResponse(Organization organization)
    {
        return new
        {
            organization.Id,
            organization.Name,
            organization.OwnerUserId
        };
    }
}
