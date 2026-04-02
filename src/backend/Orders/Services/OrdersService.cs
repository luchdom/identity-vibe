using Orders.Data.Entities;
using Orders.Data.Entities.Mappers;
using Orders.Data.Repositories.Interfaces;
using Orders.Models.Commands;
using Orders.Models.Enums;
using Orders.Models.Mappers;
using Orders.Models.Queries;
using Orders.Models.Results;
using Orders.Services.Interfaces;
using Shared.Common;

namespace Orders.Services;

public class OrdersService(IOrdersRepository ordersRepository) : IOrdersService
{
    private const decimal DefaultTaxRate = 0.08m;

    public async Task<Result<OrderResult>> CreateOrderAsync(CreateOrderCommand command)
    {
        try
        {
            var customer = await ordersRepository.FindOrCreateCustomerAsync(command.Customer.ToResultModel());
            var orderNumber = await ordersRepository.GenerateOrderNumberAsync();
            var domainItems = command.Items.Select(item => item.ToResultModel()).ToList();

            var subtotal = domainItems.Sum(item => item.TotalPrice);
            var tax = subtotal * DefaultTaxRate;
            var shippingAmount = command.Shipping?.ShippingCost ?? 0;

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = customer.Id,
                UserId = command.UserId,
                Status = OrderStatus.Draft,
                SubtotalAmount = subtotal,
                TaxAmount = tax,
                ShippingAmount = shippingAmount,
                TotalAmount = subtotal + tax + shippingAmount,
                SpecialInstructions = command.SpecialInstructions,
                CorrelationId = command.CorrelationId,
                IdempotencyKey = command.IdempotencyKey,
                Source = "API"
            };

            if (command.Shipping != null)
            {
                var shipping = command.Shipping.ToResultModel();
                order.ShippingAddress = shipping.Address;
                order.ShippingCity = shipping.City;
                order.ShippingState = shipping.State;
                order.ShippingPostalCode = shipping.PostalCode;
                order.ShippingCountry = shipping.Country;
            }

            var orderItems = domainItems.Select(item => item.ToEntity(0)).ToList();
            var statusHistory = new OrderStatusHistory
            {
                OrderId = 0,
                FromStatus = OrderStatus.Draft,
                ToStatus = OrderStatus.Draft,
                Reason = "Order created",
                ChangedByUserId = command.UserId,
                CorrelationId = command.CorrelationId
            };

            var createdOrder = await ordersRepository.CreateOrderAsync(order, orderItems, statusHistory);
            return Result<OrderResult>.Success(new OrderResult
            {
                Order = createdOrder,
                CorrelationId = command.CorrelationId
            });
        }
        catch (Exception ex)
        {
            return new Error("ORDER_CREATION_ERROR", $"An error occurred while creating the order: {ex.Message}")
                .ToResult<OrderResult>();
        }
    }

    public async Task<Result<OrdersListResult>> GetOrdersAsync(GetOrdersQuery query)
    {
        try
        {
            var (orders, totalCount) = await ordersRepository.GetOrdersAsync(query.UserId, query.Page, query.PageSize, query.Status);
            return Result<OrdersListResult>.Success(CreateOrdersListResult(query.Page, query.PageSize, orders, totalCount));
        }
        catch (Exception ex)
        {
            return new Error("GET_ORDERS_ERROR", $"An error occurred while retrieving orders: {ex.Message}")
                .ToResult<OrdersListResult>();
        }
    }

    public async Task<Result<OrdersListResult>> GetAllOrdersAsync(GetAllOrdersQuery query)
    {
        try
        {
            var (orders, totalCount) = await ordersRepository.GetAllOrdersAsync(query.Page, query.PageSize, query.Status);
            return Result<OrdersListResult>.Success(CreateOrdersListResult(query.Page, query.PageSize, orders, totalCount));
        }
        catch (Exception ex)
        {
            return new Error("GET_ORDERS_ERROR", $"An error occurred while retrieving orders: {ex.Message}")
                .ToResult<OrdersListResult>();
        }
    }

    public async Task<Result<OrderResult>> GetOrderByIdAsync(GetOrderByIdQuery query)
    {
        try
        {
            var userId = query.IsAdminRequest ? null : query.UserId;
            var order = await ordersRepository.GetOrderByIdAsync(query.OrderId, userId, includeHistory: true);

            if (order == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<OrderResult>();
            }

            return Result<OrderResult>.Success(new OrderResult { Order = order });
        }
        catch (Exception ex)
        {
            return new Error("GET_ORDER_ERROR", $"An error occurred while retrieving the order: {ex.Message}")
                .ToResult<OrderResult>();
        }
    }

    public async Task<Result<OrderResult>> UpdateOrderAsync(UpdateOrderCommand command)
    {
        try
        {
            var existingOrder = await ordersRepository.GetOrderByIdAsync(command.OrderId, command.UserId, includeHistory: true);
            if (existingOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<OrderResult>();
            }

            if (!existingOrder.CanBeModified)
            {
                return new Error("ORDER_CANNOT_BE_MODIFIED", $"Order in {existingOrder.Status} status cannot be modified")
                    .ToResult<OrderResult>();
            }

            var updatedOrder = await ordersRepository.UpdateOrderAsync(
                command.OrderId,
                command.UserId,
                command.Customer?.ToResultModel(),
                command.Items?.Select(item => item.ToResultModel()).ToList(),
                command.Shipping?.ToResultModel(),
                command.SpecialInstructions);

            if (updatedOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<OrderResult>();
            }

            return Result<OrderResult>.Success(new OrderResult
            {
                Order = updatedOrder,
                CorrelationId = command.CorrelationId
            });
        }
        catch (Exception ex)
        {
            return new Error("ORDER_UPDATE_ERROR", $"An error occurred while updating the order: {ex.Message}")
                .ToResult<OrderResult>();
        }
    }

    public async Task<Result<OrderResult>> CancelOrderAsync(CancelOrderCommand command)
    {
        try
        {
            var existingOrder = await ordersRepository.GetOrderByIdAsync(command.OrderId, command.UserId, includeHistory: true);
            if (existingOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<OrderResult>();
            }

            if (!existingOrder.CanBeCancelled)
            {
                return new Error("ORDER_CANNOT_BE_CANCELLED", $"Order in {existingOrder.Status} status cannot be cancelled")
                    .ToResult<OrderResult>();
            }

            var cancelledOrder = await ordersRepository.CancelOrderAsync(
                command.OrderId,
                command.UserId,
                command.CancellationReason,
                command.Notes);

            if (cancelledOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<OrderResult>();
            }

            return Result<OrderResult>.Success(new OrderResult
            {
                Order = cancelledOrder,
                CorrelationId = command.CorrelationId
            });
        }
        catch (Exception ex)
        {
            return new Error("ORDER_CANCEL_ERROR", $"An error occurred while cancelling the order: {ex.Message}")
                .ToResult<OrderResult>();
        }
    }

    public async Task<Result<OrderStatusResult>> GetOrderStatusAsync(GetOrderStatusQuery query)
    {
        try
        {
            var userId = query.IsAdminRequest ? null : query.UserId;
            var statusData = await ordersRepository.GetOrderStatusAsync(query.OrderId, userId);

            if (statusData == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<OrderStatusResult>();
            }

            return Result<OrderStatusResult>.Success(new OrderStatusResult { StatusData = statusData });
        }
        catch (Exception ex)
        {
            return new Error("GET_ORDER_STATUS_ERROR", $"An error occurred while retrieving order status: {ex.Message}")
                .ToResult<OrderStatusResult>();
        }
    }

    public async Task<Result<OrderResult>> UpdateOrderStatusAsync(UpdateOrderStatusCommand command)
    {
        try
        {
            var existingOrder = await ordersRepository.GetOrderByIdAsync(command.OrderId, includeHistory: true);
            if (existingOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found").ToResult<OrderResult>();
            }

            if (!IsValidStatusTransition(existingOrder.Status, command.Status))
            {
                return new Error("INVALID_STATUS_TRANSITION", $"Cannot change status from {existingOrder.Status} to {command.Status}")
                    .ToResult<OrderResult>();
            }

            var updatedOrder = await ordersRepository.UpdateOrderStatusAsync(
                command.OrderId,
                command.Status,
                command.UserId,
                command.Reason,
                command.Notes);

            if (updatedOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found").ToResult<OrderResult>();
            }

            return Result<OrderResult>.Success(new OrderResult { Order = updatedOrder });
        }
        catch (Exception ex)
        {
            return new Error("ORDER_STATUS_UPDATE_ERROR", $"An error occurred while updating order status: {ex.Message}")
                .ToResult<OrderResult>();
        }
    }

    public async Task<Result<OrderResult>> AddTrackingNumberAsync(AddTrackingNumberCommand command)
    {
        try
        {
            var existingOrder = await ordersRepository.GetOrderByIdAsync(command.OrderId, includeHistory: true);
            if (existingOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found").ToResult<OrderResult>();
            }

            var updatedOrder = await ordersRepository.AddTrackingNumberAsync(command.OrderId, command.TrackingNumber, command.UserId);
            if (updatedOrder == null)
            {
                return new Error("ORDER_NOT_FOUND", "Order not found").ToResult<OrderResult>();
            }

            return Result<OrderResult>.Success(new OrderResult { Order = updatedOrder });
        }
        catch (Exception ex)
        {
            return new Error("ADD_TRACKING_ERROR", $"An error occurred while adding tracking number: {ex.Message}")
                .ToResult<OrderResult>();
        }
    }

    public async Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId)
    {
        try
        {
            var canModify = await ordersRepository.CanModifyOrderAsync(orderId, userId);
            return canModify.HasValue
                ? Result<bool>.Success(canModify.Value)
                : new Error("ORDER_NOT_FOUND", "Order not found or access denied").ToResult<bool>();
        }
        catch (Exception ex)
        {
            return new Error("CHECK_MODIFY_ERROR", $"An error occurred while checking order modification status: {ex.Message}")
                .ToResult<bool>();
        }
    }

    public Task<Result<decimal>> CalculateOrderTotalAsync(List<OrderItemResult> items, decimal shippingCost = 0, decimal taxRate = DefaultTaxRate)
    {
        try
        {
            var subtotal = items.Sum(item => item.TotalPrice);
            var tax = subtotal * taxRate;
            return Task.FromResult(Result<decimal>.Success(subtotal + tax + shippingCost));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new Error("CALCULATION_ERROR", $"An error occurred while calculating order total: {ex.Message}")
                .ToResult<decimal>());
        }
    }

    private static OrdersListResult CreateOrdersListResult(int page, int pageSize, List<OrderDetailsResult> orders, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new OrdersListResult
        {
            Orders = orders,
            Pagination = new PaginationResult(
                totalCount,
                page,
                pageSize,
                totalPages,
                page > 1,
                page < totalPages)
        };
    }

    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return currentStatus switch
        {
            OrderStatus.Draft => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => newStatus is OrderStatus.Processing or OrderStatus.Cancelled,
            OrderStatus.Processing => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus == OrderStatus.Delivered,
            OrderStatus.Delivered => false,
            OrderStatus.Cancelled => false,
            _ => false
        };
    }
}
