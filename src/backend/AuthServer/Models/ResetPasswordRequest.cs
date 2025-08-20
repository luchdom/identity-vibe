using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

public record ResetPasswordRequest
{
    [Required]
    public required string Token { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string NewPassword { get; init; }

    [Required]
    [Compare(nameof(NewPassword))]
    public required string ConfirmPassword { get; init; }
}