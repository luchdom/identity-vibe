using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

public record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}