using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Orders.Entities;
using Orders.Entities.Mappers;
using Orders.Models.Enums;
using Orders.Models.ViewModels;
using Orders.Repositories.Interfaces;
using Shared.Common;

namespace Orders.Repositories;

public class OrdersRepository(
    OrdersDbContext context) : IOrdersRepository
{
    public async Task<Result<OrderData>> CreateOrderAsync(Order order, List<OrderItem> orderItems, OrderStatusHistory statusHistory)
    {
        try
        {
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Set OrderId for items and status history
            foreach (var item in orderItems)
            {
                item.OrderId = order.Id;
            }
            statusHistory.OrderId = order.Id;

            context.OrderItems.AddRange(orderItems);
            context.OrderStatusHistory.Add(statusHistory);
            order.OrderItems = orderItems;
            
            await context.SaveChangesAsync();

            // Update totals and reload with all related data
            order.UpdateTotals();
            await context.SaveChangesAsync();

            var fullOrder = await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstAsync(o => o.Id == order.Id);

            return Result<OrderData>.Success(fullOrder.ToDomain());
        }
        catch (Exception ex)
        {
            return Result<OrderData>.Failure("ORDER_CREATION_ERROR", $"An error occurred while creating the order: {ex.Message}");
        }
    }

    public async Task<Result<OrderData?>> GetOrderByIdAsync(int orderId, string? userId = null, bool includeHistory = false)
    {
        try
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

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync();
            
            return order != null 
                ? Result<OrderData?>.Success(order.ToDomain())
                : Result<OrderData?>.Success(null);
        }
        catch (Exception ex)
        {
            return Result<OrderData?>.Failure("GET_ORDER_ERROR", $"An error occurred while retrieving the order: {ex.Message}");
        }
    }

    public async Task<Result<(List<OrderData> Orders, int TotalCount)>> GetOrdersAsync(string userId, int page, int pageSize, OrderStatus? status = null)
    {
        try
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

            var domainOrders = orders.Select(o => o.ToDomain()).ToList();
            
            return Result<(List<OrderData>, int)>.Success((domainOrders, totalCount));
        }
        catch (Exception ex)
        {
            return Result<(List<OrderData>, int)>.Failure("GET_ORDERS_ERROR", $"An error occurred while retrieving orders: {ex.Message}");
        }
    }

    public async Task<Result<(List<OrderData> Orders, int TotalCount)>> GetAllOrdersAsync(int page, int pageSize, OrderStatus? status = null)
    {
        try
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

            var domainOrders = orders.Select(o => o.ToDomain()).ToList();
            
            return Result<(List<OrderData>, int)>.Success((domainOrders, totalCount));
        }
        catch (Exception ex)
        {
            return Result<(List<OrderData>, int)>.Failure("GET_ORDERS_ERROR", $"An error occurred while retrieving orders: {ex.Message}");
        }
    }

    public async Task<Result<OrderStatusData>> GetOrderStatusAsync(int orderId, string? userId = null)
    {
        try
        {
            var query = context.Orders
                .Include(o => o.StatusHistory)
                .Where(o => o.Id == orderId);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync();
            
            if (order == null)
            {
                return Result<OrderStatusData>.Failure("ORDER_NOT_FOUND", "Order not found or access denied");
            }

            var statusData = new OrderStatusData
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                TrackingNumber = order.TrackingNumber,
                LastUpdated = order.UpdatedAt,
                StatusHistory = order.StatusHistory
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => h.ToDomain())
                    .ToList()
            };

            return Result<OrderStatusData>.Success(statusData);
        }
        catch (Exception ex)
        {
            return Result<OrderStatusData>.Failure("GET_ORDER_STATUS_ERROR", $"An error occurred while retrieving order status: {ex.Message}");
        }
    }

    public async Task<Result<OrderData>> UpdateOrderAsync(int orderId, string userId, CustomerData? customer = null, List<OrderItemData>? items = null, ShippingData? shipping = null, string? specialInstructions = null)
    {
        try
        {
            var order = await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return Result<OrderData>.Failure("ORDER_NOT_FOUND", "Order not found or access denied");
            }

            if (!order.CanBeModified())
            {
                return Result<OrderData>.Failure("ORDER_CANNOT_BE_MODIFIED", 
                    $"Order in {order.Status} status cannot be modified");
            }

            // Update customer info if provided
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

            // Update shipping info if provided
            if (shipping != null)
            {
                order.ShippingAddress = shipping.Address;
                order.ShippingCity = shipping.City;
                order.ShippingState = shipping.State;
                order.ShippingPostalCode = shipping.PostalCode;
                order.ShippingCountry = shipping.Country;
                order.ShippingAmount = shipping.ShippingCost;
            }

            // Update special instructions
            if (specialInstructions != null)
            {
                order.SpecialInstructions = specialInstructions;
            }

            // Update items if provided
            if (items != null)
            {
                // Remove existing items
                context.OrderItems.RemoveRange(order.OrderItems);

                // Add new items
                var newItems = items.Select(item => item.ToEntity(order.Id)).ToList();
                context.OrderItems.AddRange(newItems);
                order.OrderItems = newItems;
            }

            // Update totals
            order.UpdateTotals();
            await context.SaveChangesAsync();

            return Result<OrderData>.Success(order.ToDomain());
        }
        catch (Exception ex)
        {
            return Result<OrderData>.Failure("ORDER_UPDATE_ERROR", $"An error occurred while updating the order: {ex.Message}");
        }
    }

    public async Task<Result<OrderData>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminUserId, string? reason = null, string? notes = null)
    {
        try
        {
            var order = await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Result<OrderData>.Failure("ORDER_NOT_FOUND", "Order not found");
            }

            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                return Result<OrderData>.Failure("INVALID_STATUS_TRANSITION", 
                    $"Cannot change status from {order.Status} to {newStatus}");
            }

            var previousStatus = order.Status;
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // Set specific timestamps based on status
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

            // Add status history entry
            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = newStatus,
                Reason = reason ?? $"Status changed to {newStatus}",
                Notes = notes,
                ChangedByUserId = adminUserId
            };

            context.OrderStatusHistory.Add(statusHistory);
            await context.SaveChangesAsync();

            return Result<OrderData>.Success(order.ToDomain());
        }
        catch (Exception ex)
        {
            return Result<OrderData>.Failure("ORDER_STATUS_UPDATE_ERROR", $"An error occurred while updating order status: {ex.Message}");
        }
    }

    public async Task<Result<OrderData>> CancelOrderAsync(int orderId, string userId, string? reason = null, string? notes = null)
    {
        try
        {
            var order = await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return Result<OrderData>.Failure("ORDER_NOT_FOUND", "Order not found or access denied");
            }

            if (!order.CanBeCancelled())
            {
                return Result<OrderData>.Failure("ORDER_CANNOT_BE_CANCELLED", 
                    $"Order in {order.Status} status cannot be cancelled");
            }

            var previousStatus = order.Status;
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // Add status history entry
            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = OrderStatus.Cancelled,
                Reason = reason ?? "Order cancelled by customer",
                Notes = notes,
                ChangedByUserId = userId
            };

            context.OrderStatusHistory.Add(statusHistory);
            await context.SaveChangesAsync();

            return Result<OrderData>.Success(order.ToDomain());
        }
        catch (Exception ex)
        {
            return Result<OrderData>.Failure("ORDER_CANCEL_ERROR", $"An error occurred while cancelling the order: {ex.Message}");
        }
    }

    public async Task<Result<OrderData>> AddTrackingNumberAsync(int orderId, string trackingNumber, string adminUserId)
    {
        try
        {
            var order = await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Result<OrderData>.Failure("ORDER_NOT_FOUND", "Order not found");
            }

            order.TrackingNumber = trackingNumber;
            order.UpdatedAt = DateTime.UtcNow;

            // Add status history entry
            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = order.Status,
                Reason = "Tracking number added",
                Notes = $"Tracking number: {trackingNumber}",
                ChangedByUserId = adminUserId
            };

            context.OrderStatusHistory.Add(statusHistory);
            await context.SaveChangesAsync();

            return Result<OrderData>.Success(order.ToDomain());
        }
        catch (Exception ex)
        {
            return Result<OrderData>.Failure("ADD_TRACKING_ERROR", $"An error occurred while adding tracking number: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId)
    {
        try
        {
            var order = await context.Orders
                .Where(o => o.Id == orderId && o.UserId == userId)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return Result<bool>.Failure("ORDER_NOT_FOUND", "Order not found or access denied");
            }

            return Result<bool>.Success(order.CanBeModified());
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("CHECK_MODIFY_ERROR", $"An error occurred while checking order modification status: {ex.Message}");
        }
    }

    public async Task<Result<Customer?>> FindOrCreateCustomerAsync(CustomerData customerData)
    {
        try
        {
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Email == customerData.Email);

            if (customer == null)
            {
                customer = customerData.ToEntity();
                context.Customers.Add(customer);
                await context.SaveChangesAsync();
            }
            else
            {
                // Update customer info if it has changed
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
            }

            return Result<Customer?>.Success(customer);
        }
        catch (Exception ex)
        {
            return Result<Customer?>.Failure("CUSTOMER_ERROR", $"An error occurred while processing customer information: {ex.Message}");
        }
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var prefix = "ORD";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        
        var orderNumber = $"{prefix}-{timestamp}-{random}";
        
        // Ensure uniqueness
        while (await context.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            random = new Random().Next(1000, 9999);
            orderNumber = $"{prefix}-{timestamp}-{random}";
        }

        return orderNumber;
    }

    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return currentStatus switch
        {
            OrderStatus.Draft => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => newStatus is OrderStatus.Processing or OrderStatus.Cancelled,
            OrderStatus.Processing => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus is OrderStatus.Delivered,
            OrderStatus.Delivered => false,
            OrderStatus.Cancelled => false,
            _ => false
        };
    }
}