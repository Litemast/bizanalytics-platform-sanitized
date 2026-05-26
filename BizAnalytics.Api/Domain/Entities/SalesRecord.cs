namespace BizAnalytics.Api.Domain.Entities;

public class SalesRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid? AnalysisWorkspaceId { get; set; }
    public AnalysisWorkspace? AnalysisWorkspace { get; set; }

    public DateTime Date { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? SourceFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
