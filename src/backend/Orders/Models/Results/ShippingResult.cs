namespace Orders.Models.Results;

public sealed record ShippingResult(
    string Address,
    string City,
    string State,
    string PostalCode,
    string Country,
    decimal ShippingCost,
    string? ShippingMethod,
    string? Notes);
