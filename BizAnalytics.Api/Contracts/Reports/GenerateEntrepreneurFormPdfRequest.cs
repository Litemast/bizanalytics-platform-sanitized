using BizAnalytics.Api.Contracts.Entrepreneurs;
using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.Reports;

public class GenerateEntrepreneurFormPdfRequest
{
    [Required]
    public string FormCode { get; set; } = string.Empty;

    [Required]
    public IndividualEntrepreneurRegistryProfileResponse Registry { get; set; } = new();

    public string Language { get; set; } = "ru";
    public string? GeneratedFor { get; set; }
}
