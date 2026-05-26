using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.Import;

public class ImportFilesRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    public Guid? AnalysisWorkspaceId { get; set; }

    [Required]
    [FromForm(Name = "files")]
    public List<IFormFile> Files { get; set; } = [];
}
