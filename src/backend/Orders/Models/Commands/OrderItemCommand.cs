namespace Orders.Models.Commands;

public record OrderItemCommand
{
    public required string ProductSku { get; init; }
    public required string ProductName { get; init; }
    public string? ProductDescription { get; init; }
    public string? ProductImageUrl { get; init; }
    public string? ProductCategory { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? Size { get; init; }
    public string? Color { get; init; }
    public string? AdditionalAttributes { get; init; }
}