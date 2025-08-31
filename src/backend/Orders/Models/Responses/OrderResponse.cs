using Orders.Models.Enums;

namespace Orders.Models.Responses;

public record OrderResponse
{
    public int Id { get; init; }
    public required string OrderNumber { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    
    public required CustomerResponse Customer { get; init; }
    public required List<OrderItemResponse> Items { get; init; }
    
    public decimal SubtotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    
    public string? TrackingNumber { get; init; }
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    
    public ShippingResponse? Shipping { get; init; }
    public string? SpecialInstructions { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    
    public int ItemCount { get; init; }
    public bool CanBeCancelled { get; init; }
    public bool CanBeModified { get; init; }
    
    public required List<OrderStatusHistoryResponse> StatusHistory { get; init; }
}