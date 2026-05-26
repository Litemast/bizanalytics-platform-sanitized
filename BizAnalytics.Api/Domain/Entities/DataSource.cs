namespace BizAnalytics.Api.Domain.Entities;

public class DataSource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!; 
    public string? SettingsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}