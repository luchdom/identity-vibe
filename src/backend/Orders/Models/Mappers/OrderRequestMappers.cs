using Orders.Models.Requests;
using Orders.Models.Commands;
using Orders.Models.Queries;

namespace Orders.Models.Mappers;

public static class OrderRequestMappers
{
    public static CreateOrderCommand ToDomain(this CreateOrderRequest request, string userId, string? correlationId)
    {
        return new CreateOrderCommand
        {
            UserId = userId,
            CorrelationId = correlationId,
            Customer = request.Customer.ToCommand(),
            Items = request.Items.Select(item => item.ToCommand()).ToList(),
            Shipping = request.Shipping?.ToCommand(),
            SpecialInstructions = request.SpecialInstructions,
            IdempotencyKey = request.IdempotencyKey
        };
    }

    public static UpdateOrderCommand ToDomain(this UpdateOrderRequest request, int orderId, string userId, string? correlationId)
    {
        return new UpdateOrderCommand
        {
            OrderId = orderId,
            UserId = userId,
            CorrelationId = correlationId,
            Customer = request.Customer?.ToCommand(),
            Items = request.Items?.Select(item => item.ToCommand()).ToList(),
            Shipping = request.Shipping?.ToCommand(),
            SpecialInstructions = request.SpecialInstructions,
            IdempotencyKey = request.IdempotencyKey
        };
    }

    public static CancelOrderCommand ToDomain(this CancelOrderRequest request, int orderId, string userId, string? correlationId)
    {
        return new CancelOrderCommand
        {
            OrderId = orderId,
            UserId = userId,
            CorrelationId = correlationId,
            CancellationReason = request.CancellationReason,
            Notes = request.Notes
        };
    }

    public static UpdateOrderStatusCommand ToDomain(this UpdateOrderStatusRequest request, int orderId, string userId)
    {
        return new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            UserId = userId,
            Status = request.Status,
            Reason = request.Reason,
            Notes = request.Notes
        };
    }

    public static AddTrackingNumberCommand ToDomain(this AddTrackingNumberRequest request, int orderId, string userId)
    {
        return new AddTrackingNumberCommand
        {
            OrderId = orderId,
            UserId = userId,
            TrackingNumber = request.TrackingNumber
        };
    }

    public static GetOrdersQuery ToDomain(string userId, int page, int pageSize, Orders.Models.Enums.OrderStatus? status)
    {
        return new GetOrdersQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize,
            Status = status
        };
    }

    public static GetAllOrdersQuery ToDomain(int page, int pageSize, Orders.Models.Enums.OrderStatus? status)
    {
        return new GetAllOrdersQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status
        };
    }

    public static GetOrderByIdQuery ToDomain(int orderId, string userId, bool isAdmin)
    {
        return new GetOrderByIdQuery
        {
            OrderId = orderId,
            UserId = userId,
            IsAdminRequest = isAdmin
        };
    }

    public static GetOrderStatusQuery ToStatusQuery(int orderId, string userId, bool isAdmin)
    {
        return new GetOrderStatusQuery
        {
            OrderId = orderId,
            UserId = userId,
            IsAdminRequest = isAdmin
        };
    }

    // Extension methods for converting Request models to Command models
    public static CustomerCommand ToCommand(this CustomerInfo customerInfo) => new()
    {
        FirstName = customerInfo.FirstName,
        LastName = customerInfo.LastName,
        Email = customerInfo.Email,
        Phone = customerInfo.Phone,
        Address = customerInfo.Address,
        City = customerInfo.City,
        State = customerInfo.State,
        PostalCode = customerInfo.PostalCode,
        Country = customerInfo.Country
    };

    public static ShippingCommand ToCommand(this ShippingInfo shippingInfo) => new()
    {
        Address = shippingInfo.Address ?? string.Empty,
        City = shippingInfo.City ?? string.Empty,
        State = shippingInfo.State ?? string.Empty,
        PostalCode = shippingInfo.PostalCode ?? string.Empty,
        Country = shippingInfo.Country ?? string.Empty,
        ShippingCost = shippingInfo.ShippingCost,
        ShippingMethod = null, // Not in ShippingInfo - could be added later
        Notes = null
    };

    public static OrderItemCommand ToCommand(this OrderItemRequest item) => new()
    {
        ProductSku = item.ProductSku,
        ProductName = item.ProductName,
        ProductDescription = item.ProductDescription,
        ProductImageUrl = item.ProductImageUrl,
        ProductCategory = item.ProductCategory,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        DiscountAmount = item.DiscountAmount,
        Size = item.Size,
        Color = item.Color,
        AdditionalAttributes = item.AdditionalAttributes
    };
}