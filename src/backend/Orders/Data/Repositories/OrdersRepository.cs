using Microsoft.EntityFrameworkCore;
using Orders.Data.Entities;
using Orders.Data.Entities.Mappers;
using Orders.Data.Repositories.Interfaces;
using Orders.Models.Enums;
using Orders.Models.Results;

namespace Orders.Data.Repositories;

public class OrdersRepository(OrdersDbContext context) : IOrdersRepository
{
    public async Task<OrderDetailsResult> CreateOrderAsync(Order order, List<OrderItem> orderItems, OrderStatusHistory statusHistory)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
        }

        statusHistory.OrderId = order.Id;
        context.OrderItems.AddRange(orderItems);
        context.OrderStatusHistory.Add(statusHistory);
        order.OrderItems = orderItems;

        await context.SaveChangesAsync();

        order.UpdateTotals();
        await context.SaveChangesAsync();

        var fullOrder = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .FirstAsync(o => o.Id == order.Id);

        return fullOrder.ToDomain();
    }

    public async Task<OrderDetailsResult?> GetOrderByIdAsync(int orderId, string? userId = null, bool includeHistory = false)
    {
        var query = context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (includeHistory)
        {
            query = query.Include(o => o.StatusHistory);
        }

        query = query.Where(o => o.Id == orderId);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(o => o.UserId == userId);
        }

        var order = await query.FirstOrDefaultAsync();
        return order?.ToDomain();
    }

    public async Task<(List<OrderDetailsResult> Orders, int TotalCount)> GetOrdersAsync(string userId, int page, int pageSize, OrderStatus? status = null)
    {
        var query = context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders.Select(order => order.ToDomain()).ToList(), totalCount);
    }

    public async Task<(List<OrderDetailsResult> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize, OrderStatus? status = null)
    {
        var query = context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders.Select(order => order.ToDomain()).ToList(), totalCount);
    }

    public async Task<OrderStatusDetailsResult?> GetOrderStatusAsync(int orderId, string? userId = null)
    {
        var query = context.Orders
            .Include(o => o.StatusHistory)
            .Where(o => o.Id == orderId);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(o => o.UserId == userId);
        }

        var order = await query.FirstOrDefaultAsync();
        if (order == null)
        {
            return null;
        }

        return new OrderStatusDetailsResult(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.TrackingNumber,
            order.UpdatedAt,
            order.StatusHistory
                .OrderByDescending(history => history.CreatedAt)
                .Select(history => history.ToDomain())
                .ToList());
    }

    public async Task<OrderDetailsResult?> UpdateOrderAsync(
        int orderId,
        string userId,
        CustomerResult? customer = null,
        List<OrderItemResult>? items = null,
        ShippingResult? shipping = null,
        string? specialInstructions = null)
    {
        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return null;
        }

        if (customer != null)
        {
            order.Customer.FirstName = customer.FirstName;
            order.Customer.LastName = customer.LastName;
            order.Customer.Email = customer.Email;
            order.Customer.Phone = customer.Phone;
            order.Customer.Address = customer.Address;
            order.Customer.City = customer.City;
            order.Customer.State = customer.State;
            order.Customer.PostalCode = customer.PostalCode;
            order.Customer.Country = customer.Country;
            order.Customer.UpdatedAt = DateTime.UtcNow;
        }

        if (shipping != null)
        {
            order.ShippingAddress = shipping.Address;
            order.ShippingCity = shipping.City;
            order.ShippingState = shipping.State;
            order.ShippingPostalCode = shipping.PostalCode;
            order.ShippingCountry = shipping.Country;
            order.ShippingAmount = shipping.ShippingCost;
        }

        if (specialInstructions != null)
        {
            order.SpecialInstructions = specialInstructions;
        }

        if (items != null)
        {
            context.OrderItems.RemoveRange(order.OrderItems);
            var newItems = items.Select(item => item.ToEntity(order.Id)).ToList();
            context.OrderItems.AddRange(newItems);
            order.OrderItems = newItems;
        }

        order.UpdateTotals();
        await context.SaveChangesAsync();

        return order.ToDomain();
    }

    public async Task<OrderDetailsResult?> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminUserId, string? reason = null, string? notes = null)
    {
        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return null;
        }

        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        switch (newStatus)
        {
            case OrderStatus.Shipped:
                order.ShippedAt = DateTime.UtcNow;
                break;
            case OrderStatus.Delivered:
                order.DeliveredAt = DateTime.UtcNow;
                break;
            case OrderStatus.Cancelled:
                order.CancelledAt = DateTime.UtcNow;
                break;
        }

        context.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            Reason = reason ?? $"Status changed to {newStatus}",
            Notes = notes,
            ChangedByUserId = adminUserId
        });

        await context.SaveChangesAsync();
        return order.ToDomain();
    }

    public async Task<OrderDetailsResult?> CancelOrderAsync(int orderId, string userId, string? reason = null, string? notes = null)
    {
        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return null;
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        context.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = OrderStatus.Cancelled,
            Reason = reason ?? "Order cancelled by customer",
            Notes = notes,
            ChangedByUserId = userId
        });

        await context.SaveChangesAsync();
        return order.ToDomain();
    }

    public async Task<OrderDetailsResult?> AddTrackingNumberAsync(int orderId, string trackingNumber, string adminUserId)
    {
        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return null;
        }

        order.TrackingNumber = trackingNumber;
        order.UpdatedAt = DateTime.UtcNow;

        context.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = order.Status,
            Reason = "Tracking number added",
            Notes = $"Tracking number: {trackingNumber}",
            ChangedByUserId = adminUserId
        });

        await context.SaveChangesAsync();
        return order.ToDomain();
    }

    public async Task<bool?> CanModifyOrderAsync(int orderId, string userId)
    {
        var order = await context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        return order?.CanBeModified();
    }

    public async Task<Customer> FindOrCreateCustomerAsync(CustomerResult customerData)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == customerData.Email);

        if (customer == null)
        {
            customer = customerData.ToEntity();
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            return customer;
        }

        customer.FirstName = customerData.FirstName;
        customer.LastName = customerData.LastName;
        customer.Email = customerData.Email;
        customer.Phone = customerData.Phone;
        customer.Address = customerData.Address;
        customer.City = customerData.City;
        customer.State = customerData.State;
        customer.PostalCode = customerData.PostalCode;
        customer.Country = customerData.Country;
        customer.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return customer;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        const string prefix = "ORD";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(1000, 9999);

        var orderNumber = $"{prefix}-{timestamp}-{random}";
        while (await context.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            random = Random.Shared.Next(1000, 9999);
            orderNumber = $"{prefix}-{timestamp}-{random}";
        }

        return orderNumber;
    }
}
