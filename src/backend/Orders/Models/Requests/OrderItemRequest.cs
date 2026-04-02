namespace Orders.Models.Requests;

public sealed record OrderItemRequest(
    string ProductSku,
    string ProductName,
    string? ProductDescription,
    string? ProductImageUrl,
    string? ProductCategory,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    string? Size,
    string? Color,
    string? AdditionalAttributes);
