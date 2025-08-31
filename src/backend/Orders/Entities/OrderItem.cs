using System.ComponentModel.DataAnnotations;

namespace Orders.Entities;

public class OrderItem
{
    [Key]
    public int Id { get; set; }
    
    // Order Reference
    [Required]
    public int OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    
    // Product Information
    [Required]
    [MaxLength(100)]
    public string ProductSku { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? ProductDescription { get; set; }
    
    [MaxLength(500)]
    public string? ProductImageUrl { get; set; }
    
    [MaxLength(100)]
    public string? ProductCategory { get; set; }
    
    // Pricing and Quantity
    [Required]
    public int Quantity { get; set; }
    
    [Required]
    public decimal UnitPrice { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    [Required]
    public decimal LineTotal { get; set; }
    
    // Product Attributes (for variants)
    [MaxLength(50)]
    public string? Size { get; set; }
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    [MaxLength(500)]
    public string? AdditionalAttributes { get; set; } // JSON string for flexible attributes
    
    // System Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Business Methods
    public void CalculateLineTotal()
    {
        LineTotal = (UnitPrice * Quantity) - DiscountAmount;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public string GetDisplayName()
    {
        var displayName = ProductName;
        
        if (!string.IsNullOrEmpty(Size) || !string.IsNullOrEmpty(Color))
        {
            var attributes = new List<string>();
            if (!string.IsNullOrEmpty(Size)) attributes.Add($"Size: {Size}");
            if (!string.IsNullOrEmpty(Color)) attributes.Add($"Color: {Color}");
            displayName += $" ({string.Join(", ", attributes)})";
        }
        
        return displayName;
    }
}