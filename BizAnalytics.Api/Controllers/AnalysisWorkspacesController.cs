using BizAnalytics.Api.Contracts.AnalysisWorkspaces;
using BizAnalytics.Api.Infrastructure.Auth;
using BizAnalytics.Api.Infrastructure.Security;
using BizAnalytics.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisWorkspacesController : ControllerBase
{
    private readonly OrganizationAccessService _organizationAccessService;
    private readonly AnalysisWorkspaceService _analysisWorkspaceService;

    public AnalysisWorkspacesController(
        OrganizationAccessService organizationAccessService,
        AnalysisWorkspaceService analysisWorkspaceService)
    {
        _organizationAccessService = organizationAccessService;
        _analysisWorkspaceService = analysisWorkspaceService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organizationValidation = await ValidateOrganizationAsync(organizationId, cancellationToken);
        if (organizationValidation is not null)
        {
            return organizationValidation;
        }

        var workspaces = await _analysisWorkspaceService.GetOrCreateAsync(organizationId, cancellationToken);
        return Ok(workspaces);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAnalysisWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var organizationValidation = await ValidateOrganizationAsync(request.OrganizationId, cancellationToken);
        if (organizationValidation is not null)
        {
            return organizationValidation;
        }

        var workspace = await _analysisWorkspaceService.CreateAsync(
            request.OrganizationId,
            request.Name,
            cancellationToken);

        return Ok(workspace);
    }

    [HttpPatch("{workspaceId:guid}")]
    public async Task<IActionResult> Rename(
        Guid workspaceId,
        [FromBody] RenameAnalysisWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Analysis name is required." });
        }

        var workspaceValidation = await ValidateWorkspaceAsync(workspaceId, cancellationToken);
        if (workspaceValidation is not null)
        {
            return workspaceValidation;
        }

        var updated = await _analysisWorkspaceService.RenameAsync(workspaceId, request.Name, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{workspaceId:guid}")]
    public async Task<IActionResult> Delete(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var workspaceValidation = await ValidateWorkspaceAsync(workspaceId, cancellationToken);
        if (workspaceValidation is not null)
        {
            return workspaceValidation;
        }

        var result = await _analysisWorkspaceService.DeleteAsync(workspaceId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<IActionResult?> ValidateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
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

        return organization is null ? NotFound() : null;
    }

    private async Task<IActionResult?> ValidateWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var workspace = await _organizationAccessService.GetOwnedAnalysisWorkspaceAsync(
            workspaceId,
            userId.Value,
            cancellationToken);

        return workspace is null ? NotFound() : null;
    }
}
