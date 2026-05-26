namespace BizAnalytics.Api.Domain.Entities;

public class FinancialRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid? AnalysisWorkspaceId { get; set; }
    public AnalysisWorkspace? AnalysisWorkspace { get; set; }

    public DateTime Period { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public string? SourceFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
