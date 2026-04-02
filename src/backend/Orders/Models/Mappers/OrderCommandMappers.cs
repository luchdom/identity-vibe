using Orders.Models.Commands;
using Orders.Models.Results;

namespace Orders.Models.Mappers;

public static class OrderCommandMappers
{
    public static CustomerResult ToResultModel(this CustomerCommand command) =>
        new(
            0,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.Address,
            command.City,
            command.State,
            command.PostalCode,
            command.Country,
            DateTime.UtcNow,
            DateTime.UtcNow);

    public static ShippingResult ToResultModel(this ShippingCommand command) =>
        new(
            command.Address,
            command.City,
            command.State,
            command.PostalCode,
            command.Country,
            command.ShippingCost,
            command.ShippingMethod,
            command.Notes);

    public static OrderItemResult ToResultModel(this OrderItemCommand command) =>
        new(
            0,
            command.ProductSku,
            command.ProductName,
            command.ProductDescription,
            command.ProductImageUrl,
            command.ProductCategory,
            command.Quantity,
            command.UnitPrice,
            command.DiscountAmount,
            (command.UnitPrice * command.Quantity) - command.DiscountAmount,
            command.Size,
            command.Color,
            command.AdditionalAttributes);
}
