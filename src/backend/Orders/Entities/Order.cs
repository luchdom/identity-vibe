using System.ComponentModel.DataAnnotations;
using Orders.Models.Enums;

namespace Orders.Entities;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;
    
    // Customer Information
    [Required]
    public int CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;
    
    // Order Status and Tracking
    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    
    [MaxLength(100)]
    public string? TrackingNumber { get; set; }
    
    [MaxLength(50)]
    public string? Source { get; set; } = "API";
    
    // Financial Information
    [Required]
    public decimal SubtotalAmount { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public decimal ShippingAmount { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    [Required]
    public decimal TotalAmount { get; set; }
    
    // Currency and Payment
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    
    [MaxLength(100)]
    public string? PaymentReference { get; set; }
    
    // Shipping Information
    [MaxLength(255)]
    public string? ShippingAddress { get; set; }
    
    [MaxLength(100)]
    public string? ShippingCity { get; set; }
    
    [MaxLength(50)]
    public string? ShippingState { get; set; }
    
    [MaxLength(20)]
    public string? ShippingPostalCode { get; set; }
    
    [MaxLength(100)]
    public string? ShippingCountry { get; set; }
    
    // Special Instructions and Notes
    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }
    
    [MaxLength(1000)]
    public string? InternalNotes { get; set; }
    
    // System and Audit Fields
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty; // From JWT token
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Navigation Properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    
    // Computed Properties
    public int ItemCount => OrderItems.Sum(item => item.Quantity);
    public string FullShippingAddress => $"{ShippingAddress}, {ShippingCity}, {ShippingState} {ShippingPostalCode}, {ShippingCountry}".Replace(", , ", ", ").Trim();
    
    // Business Methods
    public void UpdateTotals()
    {
        SubtotalAmount = OrderItems.Sum(item => item.LineTotal);
        TotalAmount = SubtotalAmount + TaxAmount + ShippingAmount - DiscountAmount;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool CanBeCancelled()
    {
        return Status is OrderStatus.Draft or OrderStatus.Confirmed;
    }
    
    public bool CanBeModified()
    {
        return Status == OrderStatus.Draft;
    }
}