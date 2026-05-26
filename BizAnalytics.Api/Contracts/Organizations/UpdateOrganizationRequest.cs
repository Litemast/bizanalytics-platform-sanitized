using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.Organizations;

public class UpdateOrganizationRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
}
