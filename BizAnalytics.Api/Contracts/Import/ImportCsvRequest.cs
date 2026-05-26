using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.Import;

public class ImportCsvRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    public Guid? AnalysisWorkspaceId { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}
