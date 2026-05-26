using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.AnalysisWorkspaces;

public class RenameAnalysisWorkspaceRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
}
