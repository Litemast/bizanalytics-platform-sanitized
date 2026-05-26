using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.AnalysisWorkspaces;

public class CreateAnalysisWorkspaceRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    [StringLength(200)]
    public string? Name { get; set; }
}
