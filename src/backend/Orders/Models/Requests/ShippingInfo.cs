namespace Orders.Models.Requests;

public sealed record ShippingInfo(
    string? Address,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    decimal ShippingCost = 0);
