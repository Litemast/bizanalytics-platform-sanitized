using System.ComponentModel.DataAnnotations;

namespace BizAnalytics.Api.Contracts.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
