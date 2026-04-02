using Orders.Models.Enums;

namespace Orders.Models.Results;

public sealed record OrderDetailsResult
{
    public required int Id { get; init; }
    public required string UserId { get; init; }
    public required string OrderNumber { get; init; }
    public required OrderStatus Status { get; init; }
    public required CustomerResult Customer { get; init; }
    public required IReadOnlyList<OrderItemResult> Items { get; init; }
    public ShippingResult? Shipping { get; init; }
    public string? TrackingNumber { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? InternalNotes { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    public string? Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public IReadOnlyList<OrderStatusHistoryResult> StatusHistory { get; init; } = [];

    public bool CanBeCancelled => Status is OrderStatus.Draft or OrderStatus.Confirmed;

    public bool CanBeModified => Status == OrderStatus.Draft;
}
