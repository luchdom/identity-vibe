using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record ShippingInfo
{
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

    [Range(0, double.MaxValue)]
    public decimal ShippingCost { get; init; } = 0;
}