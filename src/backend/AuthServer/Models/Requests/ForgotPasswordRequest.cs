using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models.Requests;

public record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}