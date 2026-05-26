using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Infrastructure.Security;

public class OrganizationAccessService
{
    private readonly AppDbContext _db;

    public OrganizationAccessService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Organization?> GetOwnedOrganizationAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == organizationId && x.OwnerUserId == userId,
                cancellationToken);
    }

    public async Task<AnalysisWorkspace?> GetOwnedAnalysisWorkspaceAsync(
        Guid analysisWorkspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.AnalysisWorkspaces
            .AsNoTracking()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(
                x => x.Id == analysisWorkspaceId && x.Organization.OwnerUserId == userId,
                cancellationToken);
    }
}
