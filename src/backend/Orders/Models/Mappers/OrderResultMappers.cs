using Orders.Models.Results;
using Orders.Models.Responses;
using Orders.Models.ViewModels;
using Orders.Models.Enums;
using Shared.Common;

namespace Orders.Models.Mappers;

public static class OrderResultMappers
{
    // Result<T> → Response extension methods for Controllers
    public static OrderResponse ToPresentation(this Result<OrderResult> result)
    {
        return result.Value.Order.ToResponse();
    }
    
    public static OrderListResponse ToPresentation(this Result<OrdersListResult> result)
    {
        return result.Value.ToPresentation();
    }
    
    public static OrderStatusResponse ToPresentation(this Result<OrderStatusResult> result)
    {
        return result.Value.ToPresentation();
    }
    
    // Direct Result → Response mappings
    public static OrderResponse ToPresentation(this OrderResult result)
    {
        return result.Order.ToResponse();
    }
    
    // Core ViewModel → Response mapping
    public static OrderResponse ToResponse(this OrderViewModel order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            Customer = order.Customer.ToResponse(),
            Items = order.Items.Select(item => item.ToResponse()).ToList(),
            SubtotalAmount = order.SubtotalAmount,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            TrackingNumber = order.TrackingNumber,
            PaymentMethod = order.PaymentMethod,
            PaymentReference = order.PaymentReference,
            Shipping = order.Shipping?.ToResponse(),
            SpecialInstructions = order.SpecialInstructions,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            ItemCount = order.Items.Sum(i => i.Quantity),
            CanBeCancelled = order.Status is Orders.Models.Enums.OrderStatus.Draft or Orders.Models.Enums.OrderStatus.Confirmed,
            CanBeModified = order.Status == Orders.Models.Enums.OrderStatus.Draft,
            StatusHistory = order.StatusHistory.Select(h => h.ToResponse()).ToList()
        };
    }

    public static OrderListResponse ToPresentation(this OrdersListResult result)
    {
        return new OrderListResponse
        {
            Orders = result.Orders.Select(order => order.ToSummaryResponse()).ToList(),
            TotalCount = result.Pagination.TotalCount,
            PageNumber = result.Pagination.PageNumber,
            PageSize = result.Pagination.PageSize,
            TotalPages = result.Pagination.TotalPages,
            HasPreviousPage = result.Pagination.HasPreviousPage,
            HasNextPage = result.Pagination.HasNextPage
        };
    }
    
    public static OrderSummaryResponse ToSummaryResponse(this OrderViewModel order)
    {
        return new OrderSummaryResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            CustomerName = $"{order.Customer.FirstName} {order.Customer.LastName}",
            CustomerEmail = order.Customer.Email,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            ItemCount = order.Items.Sum(i => i.Quantity),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ShippedAt = order.ShippedAt,
            CanBeCancelled = order.Status is OrderStatus.Draft or OrderStatus.Confirmed,
            CanBeModified = order.Status == OrderStatus.Draft
        };
    }

    public static OrderStatusResponse ToPresentation(this OrderStatusResult result)
    {
        return result.StatusData.ToResponse();
    }
    
    // ViewModel → Response extension methods
    public static CustomerResponse ToResponse(this CustomerViewModel customer) => new()
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        FullName = $"{customer.FirstName} {customer.LastName}".Trim(),
        Email = customer.Email,
        Phone = customer.Phone,
        Address = customer.Address,
        City = customer.City,
        State = customer.State,
        PostalCode = customer.PostalCode,
        Country = customer.Country,
        FullAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.PostalCode}, {customer.Country}".Replace(", , ", ", ").Trim()
    };
    
    public static OrderItemResponse ToResponse(this OrderItemViewModel item) => new()
    {
        Id = item.Id,
        ProductSku = item.ProductSku,
        ProductName = item.ProductName,
        DisplayName = GetDisplayName(item),
        ProductDescription = item.ProductDescription,
        ProductImageUrl = item.ProductImageUrl,
        ProductCategory = item.ProductCategory,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        DiscountAmount = item.DiscountAmount,
        LineTotal = item.TotalPrice,
        Size = item.Size,
        Color = item.Color,
        AdditionalAttributes = item.AdditionalAttributes
    };
    
    private static string GetDisplayName(OrderItemViewModel item)
    {
        var displayName = item.ProductName;
        
        if (!string.IsNullOrEmpty(item.Size) || !string.IsNullOrEmpty(item.Color))
        {
            var attributes = new List<string>();
            if (!string.IsNullOrEmpty(item.Size)) attributes.Add($"Size: {item.Size}");
            if (!string.IsNullOrEmpty(item.Color)) attributes.Add($"Color: {item.Color}");
            displayName += $" ({string.Join(", ", attributes)})";
        }
        
        return displayName;
    }
    
    public static ShippingResponse ToResponse(this ShippingViewModel shipping) => new()
    {
        Address = shipping.Address,
        City = shipping.City,
        State = shipping.State,
        PostalCode = shipping.PostalCode,
        Country = shipping.Country,
        FullAddress = $"{shipping.Address}, {shipping.City}, {shipping.State} {shipping.PostalCode}, {shipping.Country}".Replace(", , ", ", ").Trim(),
        ShippingCost = shipping.ShippingCost,
        ShippingMethod = shipping.ShippingMethod,
        Notes = shipping.Notes
    };
    
    public static OrderStatusHistoryResponse ToResponse(this OrderStatusHistoryViewModel history) => new()
    {
        Id = history.Id,
        FromStatus = history.FromStatus,
        ToStatus = history.ToStatus,
        StatusChange = $"{history.FromStatus} → {history.ToStatus}",
        Reason = history.Reason,
        Notes = history.Notes,
        ChangedByUserId = history.ChangedByUserId,
        ChangedByUserName = null, // Not available in domain model
        CreatedAt = history.CreatedAt
    };
    
    public static OrderStatusResponse ToResponse(this OrderStatusViewModel statusData) => new()
    {
        OrderId = statusData.OrderId,
        OrderNumber = statusData.OrderNumber,
        CurrentStatus = statusData.Status,
        TrackingNumber = statusData.TrackingNumber,
        LastUpdated = statusData.LastUpdated,
        StatusHistory = statusData.StatusHistory.Select(h => h.ToResponse()).ToList()
    };
}