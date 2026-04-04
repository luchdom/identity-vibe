namespace Orders.Models.Requests;

public sealed record UpdateOrderRequest(
    CustomerInfo? Customer,
    List<OrderItemRequest>? Items,
    ShippingInfo? Shipping,
    string? SpecialInstructions,
    string? IdempotencyKey);

