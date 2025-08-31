using Orders.Models.Enums;

namespace Orders.Models.Responses;

public record OrderSummaryResponse
{
    public int Id { get; init; }
    public required string OrderNumber { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    
    public required string CustomerName { get; init; }
    public required string CustomerEmail { get; init; }
    
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    
    public int ItemCount { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    
    public bool CanBeCancelled { get; init; }
    public bool CanBeModified { get; init; }
}