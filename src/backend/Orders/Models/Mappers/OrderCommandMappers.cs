using Orders.Models.Commands;
using Orders.Models.ViewModels;

namespace Orders.Models.Mappers;

public static class OrderCommandMappers
{
    // Command → Domain mapping (used by Service layer)
    public static CustomerData ToDomain(this CustomerCommand command) => new()
    {
        Id = 0, // Will be set by repository
        FirstName = command.FirstName,
        LastName = command.LastName,
        Email = command.Email,
        Phone = command.Phone,
        Address = command.Address,
        City = command.City,
        State = command.State,
        PostalCode = command.PostalCode,
        Country = command.Country,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static ShippingData ToDomain(this ShippingCommand command) => new()
    {
        Address = command.Address,
        City = command.City,
        State = command.State,
        PostalCode = command.PostalCode,
        Country = command.Country,
        ShippingCost = command.ShippingCost,
        ShippingMethod = command.ShippingMethod,
        Notes = command.Notes
    };

    public static OrderItemData ToDomain(this OrderItemCommand command) => new()
    {
        Id = 0, // Will be set by repository
        ProductSku = command.ProductSku,
        ProductName = command.ProductName,
        ProductDescription = command.ProductDescription,
        ProductImageUrl = command.ProductImageUrl,
        ProductCategory = command.ProductCategory,
        Quantity = command.Quantity,
        UnitPrice = command.UnitPrice,
        DiscountAmount = command.DiscountAmount,
        TotalPrice = (command.UnitPrice * command.Quantity) - command.DiscountAmount,
        Size = command.Size,
        Color = command.Color,
        AdditionalAttributes = command.AdditionalAttributes
    };
}