using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record CustomerInfo
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public required string Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(255)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(50)]
    public string? State { get; init; }

    [MaxLength(20)]
    public string? PostalCode { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }
}