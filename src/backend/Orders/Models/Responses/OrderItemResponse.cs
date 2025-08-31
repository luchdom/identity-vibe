namespace Orders.Models.Responses;

public record OrderItemResponse
{
    public int Id { get; init; }
    public required string ProductSku { get; init; }
    public required string ProductName { get; init; }
    public required string DisplayName { get; init; }
    public string? ProductDescription { get; init; }
    public string? ProductImageUrl { get; init; }
    public string? ProductCategory { get; init; }
    
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LineTotal { get; init; }
    
    public string? Size { get; init; }
    public string? Color { get; init; }
    public string? AdditionalAttributes { get; init; }
}