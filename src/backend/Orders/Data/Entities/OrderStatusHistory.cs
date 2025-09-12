using System.ComponentModel.DataAnnotations;
using Orders.Models.Enums;

namespace Orders.Data.Entities;

public class OrderStatusHistory
{
    [Key]
    public int Id { get; set; }
    
    // Order Reference
    [Required]
    public int OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    
    // Status Change Information
    [Required]
    public OrderStatus FromStatus { get; set; }
    
    [Required]
    public OrderStatus ToStatus { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    // System Information
    [Required]
    [MaxLength(100)]
    public string ChangedByUserId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ChangedByUserName { get; set; }
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    [MaxLength(50)]
    public string? Source { get; set; } = "API";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Computed Properties
    public string StatusChange => $"{FromStatus} → {ToStatus}";
}