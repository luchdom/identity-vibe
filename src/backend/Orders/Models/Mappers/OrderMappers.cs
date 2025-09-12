using Orders.Data.Entities;
using Orders.Models.Requests;
using Orders.Models.Responses;
using Orders.Models.Enums;

namespace Orders.Models.Mappers;

public static class OrderMappers
{
    // Order Entity to Response
    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            Customer = order.Customer.ToResponse(),
            Items = order.OrderItems.Select(item => item.ToResponse()).ToList(),
            SubtotalAmount = order.SubtotalAmount,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            TrackingNumber = order.TrackingNumber,
            PaymentMethod = order.PaymentMethod,
            PaymentReference = order.PaymentReference,
            Shipping = order.ToShippingResponse(),
            SpecialInstructions = order.SpecialInstructions,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            ItemCount = order.ItemCount,
            CanBeCancelled = order.CanBeCancelled(),
            CanBeModified = order.CanBeModified(),
            StatusHistory = order.StatusHistory.Select(h => h.ToResponse()).OrderByDescending(h => h.CreatedAt).ToList()
        };
    }

    // Order Entity to Summary Response
    public static OrderSummaryResponse ToSummaryResponse(this Order order)
    {
        return new OrderSummaryResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            CustomerName = order.Customer.FullName,
            CustomerEmail = order.Customer.Email,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            ItemCount = order.ItemCount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ShippedAt = order.ShippedAt,
            CanBeCancelled = order.CanBeCancelled(),
            CanBeModified = order.CanBeModified()
        };
    }

    // Order Entity to Status Response
    public static OrderStatusResponse ToStatusResponse(this Order order)
    {
        return new OrderStatusResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CurrentStatus = order.Status,
            TrackingNumber = order.TrackingNumber,
            LastUpdated = order.UpdatedAt,
            StatusHistory = order.StatusHistory.Select(h => h.ToResponse()).OrderByDescending(h => h.CreatedAt).ToList()
        };
    }

    // Customer Entity to Response
    public static CustomerResponse ToResponse(this Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode,
            Country = customer.Country,
            FullAddress = customer.FullAddress
        };
    }

    // OrderItem Entity to Response
    public static OrderItemResponse ToResponse(this OrderItem orderItem)
    {
        return new OrderItemResponse
        {
            Id = orderItem.Id,
            ProductSku = orderItem.ProductSku,
            ProductName = orderItem.ProductName,
            DisplayName = orderItem.GetDisplayName(),
            ProductDescription = orderItem.ProductDescription,
            ProductImageUrl = orderItem.ProductImageUrl,
            ProductCategory = orderItem.ProductCategory,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            DiscountAmount = orderItem.DiscountAmount,
            LineTotal = orderItem.LineTotal,
            Size = orderItem.Size,
            Color = orderItem.Color,
            AdditionalAttributes = orderItem.AdditionalAttributes
        };
    }

    // Order Entity to Shipping Response
    public static ShippingResponse? ToShippingResponse(this Order order)
    {
        if (string.IsNullOrEmpty(order.ShippingAddress))
            return null;

        return new ShippingResponse
        {
            Address = order.ShippingAddress,
            City = order.ShippingCity,
            State = order.ShippingState,
            PostalCode = order.ShippingPostalCode,
            Country = order.ShippingCountry,
            FullAddress = order.FullShippingAddress
        };
    }

    // OrderStatusHistory Entity to Response
    public static OrderStatusHistoryResponse ToResponse(this OrderStatusHistory history)
    {
        return new OrderStatusHistoryResponse
        {
            Id = history.Id,
            FromStatus = history.FromStatus,
            ToStatus = history.ToStatus,
            StatusChange = history.StatusChange,
            Reason = history.Reason,
            Notes = history.Notes,
            ChangedByUserId = history.ChangedByUserId,
            ChangedByUserName = history.ChangedByUserName,
            CreatedAt = history.CreatedAt
        };
    }

    // Request to Entity mappings
    public static Customer ToEntity(this CustomerInfo customerInfo)
    {
        return new Customer
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
    }

    public static OrderItem ToEntity(this OrderItemRequest itemRequest, int orderId)
    {
        var orderItem = new OrderItem
        {
            OrderId = orderId,
            ProductSku = itemRequest.ProductSku,
            ProductName = itemRequest.ProductName,
            ProductDescription = itemRequest.ProductDescription,
            ProductImageUrl = itemRequest.ProductImageUrl,
            ProductCategory = itemRequest.ProductCategory,
            Quantity = itemRequest.Quantity,
            UnitPrice = itemRequest.UnitPrice,
            DiscountAmount = itemRequest.DiscountAmount,
            Size = itemRequest.Size,
            Color = itemRequest.Color,
            AdditionalAttributes = itemRequest.AdditionalAttributes
        };

        orderItem.CalculateLineTotal();
        return orderItem;
    }

    public static void UpdateFromRequest(this Customer customer, CustomerInfo customerInfo)
    {
        customer.FirstName = customerInfo.FirstName;
        customer.LastName = customerInfo.LastName;
        customer.Email = customerInfo.Email;
        customer.Phone = customerInfo.Phone;
        customer.Address = customerInfo.Address;
        customer.City = customerInfo.City;
        customer.State = customerInfo.State;
        customer.PostalCode = customerInfo.PostalCode;
        customer.Country = customerInfo.Country;
        customer.UpdatedAt = DateTime.UtcNow;
    }

    public static void UpdateShippingFromRequest(this Order order, ShippingInfo? shippingInfo)
    {
        if (shippingInfo == null)
        {
            order.ShippingAddress = null;
            order.ShippingCity = null;
            order.ShippingState = null;
            order.ShippingPostalCode = null;
            order.ShippingCountry = null;
            order.ShippingAmount = 0;
        }
        else
        {
            order.ShippingAddress = shippingInfo.Address;
            order.ShippingCity = shippingInfo.City;
            order.ShippingState = shippingInfo.State;
            order.ShippingPostalCode = shippingInfo.PostalCode;
            order.ShippingCountry = shippingInfo.Country;
            order.ShippingAmount = shippingInfo.ShippingCost;
        }
        order.UpdatedAt = DateTime.UtcNow;
    }
}