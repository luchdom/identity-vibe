using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record UpdateOrderRequest
{
    public CustomerInfo? Customer { get; init; }

    public List<OrderItemRequest>? Items { get; init; }

    public ShippingInfo? Shipping { get; init; }

    [MaxLength(1000)]
    public string? SpecialInstructions { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }
}

