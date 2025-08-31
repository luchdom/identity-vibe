namespace Orders.Models.ViewModels;

public record ShippingData
{
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public required string Country { get; init; }
    public decimal ShippingCost { get; init; }
    public string? ShippingMethod { get; init; }
    public string? Notes { get; init; }
}