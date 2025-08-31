using Orders.Models.ViewModels;
using Orders.Models.Enums;

namespace Orders.Entities.Mappers;

public static class OrderEntityMappers
{
    // Entity → Domain mapping
    public static OrderData ToDomain(this Order entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        OrderNumber = entity.OrderNumber,
        Status = entity.Status,
        TotalAmount = entity.TotalAmount,
        Items = entity.OrderItems.Select(item => item.ToDomain()).ToList(),
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
        StatusHistory = entity.StatusHistory.Select(h => h.ToDomain()).ToList()
    };

    public static CustomerData ToDomain(this Customer entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Email = entity.Email,
        Phone = entity.Phone,
        Address = entity.Address,
        City = entity.City,
        State = entity.State,
        PostalCode = entity.PostalCode,
        Country = entity.Country,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static OrderItemData ToDomain(this OrderItem entity) => new()
    {
        Id = entity.Id,
        ProductSku = entity.ProductSku,
        ProductName = entity.ProductName,
        ProductDescription = entity.ProductDescription,
        ProductImageUrl = entity.ProductImageUrl,
        ProductCategory = entity.ProductCategory,
        Quantity = entity.Quantity,
        UnitPrice = entity.UnitPrice,
        DiscountAmount = entity.DiscountAmount,
        TotalPrice = entity.LineTotal,
        Size = entity.Size,
        Color = entity.Color,
        AdditionalAttributes = entity.AdditionalAttributes
    };

    public static OrderStatusHistoryData ToDomain(this OrderStatusHistory entity) => new()
    {
        Id = entity.Id,
        OrderId = entity.OrderId,
        FromStatus = entity.FromStatus,
        ToStatus = entity.ToStatus,
        Reason = entity.Reason,
        Notes = entity.Notes,
        ChangedByUserId = entity.ChangedByUserId,
        CorrelationId = entity.CorrelationId,
        CreatedAt = entity.CreatedAt
    };

    public static ShippingData? ToDomainShipping(this Order entity)
    {
        if (string.IsNullOrEmpty(entity.ShippingAddress))
            return null;
            
        return new ShippingData
        {
            Address = entity.ShippingAddress ?? string.Empty,
            City = entity.ShippingCity ?? string.Empty,
            State = entity.ShippingState ?? string.Empty,
            PostalCode = entity.ShippingPostalCode ?? string.Empty,
            Country = entity.ShippingCountry ?? string.Empty,
            ShippingCost = entity.ShippingAmount,
            ShippingMethod = null, // Not stored in Order entity
            Notes = null // Not stored in Order entity
        };
    }

    // Domain → Entity mapping
    public static Customer ToEntity(this CustomerData domainCustomer) => new()
    {
        Id = domainCustomer.Id,
        FirstName = domainCustomer.FirstName,
        LastName = domainCustomer.LastName,
        Email = domainCustomer.Email,
        Phone = domainCustomer.Phone,
        Address = domainCustomer.Address,
        City = domainCustomer.City,
        State = domainCustomer.State,
        PostalCode = domainCustomer.PostalCode,
        Country = domainCustomer.Country,
        CreatedAt = domainCustomer.CreatedAt,
        UpdatedAt = domainCustomer.UpdatedAt
    };

    public static OrderItem ToEntity(this OrderItemData domainItem, int orderId)
    {
        var entity = new OrderItem
        {
            Id = domainItem.Id,
            OrderId = orderId,
            ProductSku = domainItem.ProductSku,
            ProductName = domainItem.ProductName,
            ProductDescription = domainItem.ProductDescription,
            ProductImageUrl = domainItem.ProductImageUrl,
            ProductCategory = domainItem.ProductCategory,
            Quantity = domainItem.Quantity,
            UnitPrice = domainItem.UnitPrice,
            DiscountAmount = domainItem.DiscountAmount,
            Size = domainItem.Size,
            Color = domainItem.Color,
            AdditionalAttributes = domainItem.AdditionalAttributes
        };
        
        entity.CalculateLineTotal();
        return entity;
    }
}