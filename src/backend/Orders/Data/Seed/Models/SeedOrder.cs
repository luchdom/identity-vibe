using Orders.Models.Enums;

namespace Orders.Data.Seed.Models;

public sealed record SeedOrder
{
    public required string OrderNumber { get; init; }
    public required string UserId { get; init; }
    public OrderStatus Status { get; init; }
    public string? Source { get; init; }

    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }

    public string Currency { get; init; } = "USD";
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }

    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }

    public string? TrackingNumber { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? InternalNotes { get; init; }

    public required SeedCustomer Customer { get; init; }
    public required List<SeedOrderItem> Items { get; init; }
    public required List<SeedOrderStatusHistory> StatusHistory { get; init; }
}
