using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.DataSources;

public class CreateDataSourceRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    public string? SettingsJson { get; set; }
}
