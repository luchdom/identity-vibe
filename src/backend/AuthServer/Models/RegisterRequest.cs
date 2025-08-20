using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; init; }

    [Required]
    [Compare(nameof(Password))]
    public required string ConfirmPassword { get; init; }

    [Required]
    [StringLength(50)]
    public required string FirstName { get; init; }

    [Required]
    [StringLength(50)]
    public required string LastName { get; init; }
}