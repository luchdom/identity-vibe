using Orders.Models.Enums;

namespace Orders.Models.ViewModels;

public record OrderData
{
    public int Id { get; init; }
    public required string UserId { get; init; }
    public required string OrderNumber { get; init; }
    public OrderStatus Status { get; init; }
    public required CustomerData Customer { get; init; }
    public required List<OrderItemData> Items { get; init; }
    public ShippingData? Shipping { get; init; }
    public string? TrackingNumber { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? InternalNotes { get; init; }
    
    // Financial data
    public decimal SubtotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    
    // System fields
    public string? Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    
    // Status history
    public List<OrderStatusHistoryData> StatusHistory { get; init; } = new();
}