namespace BizAnalytics.Api.Domain.Entities;

public class EducationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid? AnalysisWorkspaceId { get; set; }
    public AnalysisWorkspace? AnalysisWorkspace { get; set; }

    public string StudentName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public decimal Grade { get; set; }
    public decimal? AverageScore { get; set; }
    public string? SourceFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
