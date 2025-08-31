namespace Orders.Models.Responses;

public record ShippingResponse
{
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public required string FullAddress { get; init; }
    public decimal ShippingCost { get; init; }
    public string? ShippingMethod { get; init; }
    public string? Notes { get; init; }
}