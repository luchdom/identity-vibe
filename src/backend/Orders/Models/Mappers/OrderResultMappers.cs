using Orders.Models.Responses;
using Orders.Models.Results;

namespace Orders.Models.Mappers;

public static class OrderResultMappers
{
    public static OrderResponse ToPresentation(this Result<OrderResult> result) => result.Value.Order.ToResponse();

    public static OrderListResponse ToPresentation(this Result<OrdersListResult> result) => result.Value.ToPresentation();

    public static OrderStatusResponse ToPresentation(this Result<OrderStatusResult> result) => result.Value.ToPresentation();

    public static OrderResponse ToPresentation(this OrderResult result) => result.Order.ToResponse();

    public static OrderResponse ToResponse(this OrderDetailsResult order)
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
            ItemCount = order.Items.Sum(item => item.Quantity),
            CanBeCancelled = order.CanBeCancelled,
            CanBeModified = order.CanBeModified,
            StatusHistory = order.StatusHistory.Select(history => history.ToResponse()).ToList()
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

    public static OrderSummaryResponse ToSummaryResponse(this OrderDetailsResult order)
    {
        return new OrderSummaryResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            CustomerName = $"{order.Customer.FirstName} {order.Customer.LastName}".Trim(),
            CustomerEmail = order.Customer.Email,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            ItemCount = order.Items.Sum(item => item.Quantity),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ShippedAt = order.ShippedAt,
            CanBeCancelled = order.CanBeCancelled,
            CanBeModified = order.CanBeModified
        };
    }

    public static OrderStatusResponse ToPresentation(this OrderStatusResult result) => result.StatusData.ToResponse();

    public static CustomerResponse ToResponse(this CustomerResult customer)
    {
        return new CustomerResponse
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
    }

    public static OrderItemResponse ToResponse(this OrderItemResult item)
    {
        return new OrderItemResponse
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
    }

    public static ShippingResponse ToResponse(this ShippingResult shipping)
    {
        return new ShippingResponse
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
    }

    public static OrderStatusHistoryResponse ToResponse(this OrderStatusHistoryResult history)
    {
        return new OrderStatusHistoryResponse
        {
            Id = history.Id,
            FromStatus = history.FromStatus,
            ToStatus = history.ToStatus,
            StatusChange = $"{history.FromStatus} -> {history.ToStatus}",
            Reason = history.Reason,
            Notes = history.Notes,
            ChangedByUserId = history.ChangedByUserId,
            ChangedByUserName = null,
            CreatedAt = history.CreatedAt
        };
    }

    public static OrderStatusResponse ToResponse(this OrderStatusDetailsResult status)
    {
        return new OrderStatusResponse
        {
            OrderId = status.OrderId,
            OrderNumber = status.OrderNumber,
            CurrentStatus = status.Status,
            TrackingNumber = status.TrackingNumber,
            LastUpdated = status.LastUpdated,
            StatusHistory = status.StatusHistory.Select(history => history.ToResponse()).ToList()
        };
    }

    private static string GetDisplayName(OrderItemResult item)
    {
        var displayName = item.ProductName;

        if (!string.IsNullOrWhiteSpace(item.Size) || !string.IsNullOrWhiteSpace(item.Color))
        {
            var attributes = new List<string>();

            if (!string.IsNullOrWhiteSpace(item.Size))
            {
                attributes.Add($"Size: {item.Size}");
            }

            if (!string.IsNullOrWhiteSpace(item.Color))
            {
                attributes.Add($"Color: {item.Color}");
            }

            displayName += $" ({string.Join(", ", attributes)})";
        }

        return displayName;
    }
}
