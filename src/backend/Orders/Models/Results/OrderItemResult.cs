namespace Orders.Models.Results;

public sealed record OrderItemResult(
    int Id,
    string ProductSku,
    string ProductName,
    string? ProductDescription,
    string? ProductImageUrl,
    string? ProductCategory,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TotalPrice,
    string? Size,
    string? Color,
    string? AdditionalAttributes);
