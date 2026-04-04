using Orders.Models.Results;

namespace Orders.Data.Entities.Mappers;

public static class OrderEntityMappers
{
    public static OrderDetailsResult ToDomain(this Order entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        OrderNumber = entity.OrderNumber,
        Status = entity.Status,
        TotalAmount = entity.TotalAmount,
        Items = entity.OrderItems.Select(item => item.ToDomain()).ToArray(),
        Customer = entity.Customer.ToDomain(),
        Shipping = entity.ToDomainShipping(),
        TrackingNumber = entity.TrackingNumber,
        SpecialInstructions = entity.SpecialInstructions,
        InternalNotes = entity.InternalNotes,
        SubtotalAmount = entity.SubtotalAmount,
        TaxAmount = entity.TaxAmount,
        ShippingAmount = entity.ShippingAmount,
        DiscountAmount = entity.DiscountAmount,
        Currency = entity.Currency,
        PaymentMethod = entity.PaymentMethod,
        PaymentReference = entity.PaymentReference,
        Source = entity.Source,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        ShippedAt = entity.ShippedAt,
        DeliveredAt = entity.DeliveredAt,
        CancelledAt = entity.CancelledAt,
        StatusHistory = entity.StatusHistory.Select(history => history.ToDomain()).ToArray()
    };

    public static CustomerResult ToDomain(this Customer entity) =>
        new(
            entity.Id,
            entity.FirstName,
            entity.LastName,
            entity.Email,
            entity.Phone,
            entity.Address,
            entity.City,
            entity.State,
            entity.PostalCode,
            entity.Country,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static OrderItemResult ToDomain(this OrderItem entity) =>
        new(
            entity.Id,
            entity.ProductSku,
            entity.ProductName,
            entity.ProductDescription,
            entity.ProductImageUrl,
            entity.ProductCategory,
            entity.Quantity,
            entity.UnitPrice,
            entity.DiscountAmount,
            entity.LineTotal,
            entity.Size,
            entity.Color,
            entity.AdditionalAttributes);

    public static OrderStatusHistoryResult ToDomain(this OrderStatusHistory entity) =>
        new(
            entity.Id,
            entity.OrderId,
            entity.FromStatus,
            entity.ToStatus,
            entity.Reason,
            entity.Notes,
            entity.ChangedByUserId,
            entity.CorrelationId,
            entity.CreatedAt);

    public static ShippingResult? ToDomainShipping(this Order entity)
    {
        return string.IsNullOrWhiteSpace(entity.ShippingAddress)
            ? null
            : new ShippingResult(
                entity.ShippingAddress ?? string.Empty,
                entity.ShippingCity ?? string.Empty,
                entity.ShippingState ?? string.Empty,
                entity.ShippingPostalCode ?? string.Empty,
                entity.ShippingCountry ?? string.Empty,
                entity.ShippingAmount,
                null,
                null);
    }

    public static Customer ToEntity(this CustomerResult customer) => new()
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Email = customer.Email,
        Phone = customer.Phone,
        Address = customer.Address,
        City = customer.City,
        State = customer.State,
        PostalCode = customer.PostalCode,
        Country = customer.Country,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt
    };

    public static OrderItem ToEntity(this OrderItemResult item, int orderId)
    {
        var entity = new OrderItem
        {
            Id = item.Id,
            OrderId = orderId,
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

        entity.CalculateLineTotal();
        return entity;
    }
}
