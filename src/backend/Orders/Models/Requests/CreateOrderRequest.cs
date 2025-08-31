using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record CreateOrderRequest
{
    [Required]
    public required CustomerInfo Customer { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one order item is required")]
    public required List<OrderItemRequest> Items { get; init; }

    public ShippingInfo? Shipping { get; init; }

    [MaxLength(1000)]
    public string? SpecialInstructions { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }
}