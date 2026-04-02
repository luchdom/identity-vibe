namespace Orders.Models.Requests;

public sealed record CreateOrderRequest(
    CustomerInfo Customer,
    List<OrderItemRequest> Items,
    ShippingInfo? Shipping,
    string? SpecialInstructions,
    string? IdempotencyKey);
