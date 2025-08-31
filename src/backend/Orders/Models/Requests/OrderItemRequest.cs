using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record OrderItemRequest
{
    [Required]
    [MaxLength(100)]
    public required string ProductSku { get; init; }

    [Required]
    [MaxLength(255)]
    public required string ProductName { get; init; }

    [MaxLength(1000)]
    public string? ProductDescription { get; init; }

    [MaxLength(500)]
    public string? ProductImageUrl { get; init; }

    [MaxLength(100)]
    public string? ProductCategory { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public required int Quantity { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public required decimal UnitPrice { get; init; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; init; } = 0;

    [MaxLength(50)]
    public string? Size { get; init; }

    [MaxLength(50)]
    public string? Color { get; init; }

    [MaxLength(500)]
    public string? AdditionalAttributes { get; init; }
}