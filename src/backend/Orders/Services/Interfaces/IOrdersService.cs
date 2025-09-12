using Orders.Models.Commands;
using Orders.Models.Queries;
using Orders.Models.Results;
using Orders.Models.Enums;
using Orders.Models.ViewModels;
using Shared.Common;

namespace Orders.Services.Interfaces;

public interface IOrdersService
{
    /// <summary>
    /// Creates a new order with idempotency support
    /// </summary>
    Task<Result<OrderResult>> CreateOrderAsync(CreateOrderCommand command);

    /// <summary>
    /// Gets orders for a specific user with pagination
    /// </summary>
    Task<Result<OrdersListResult>> GetOrdersAsync(GetOrdersQuery query);

    /// <summary>
    /// Gets all orders (admin only) with pagination
    /// </summary>
    Task<Result<OrdersListResult>> GetAllOrdersAsync(GetAllOrdersQuery query);

    /// <summary>
    /// Gets a specific order by ID
    /// </summary>
    Task<Result<OrderResult>> GetOrderByIdAsync(GetOrderByIdQuery query);

    /// <summary>
    /// Updates an existing order (only allowed in Draft status)
    /// </summary>
    Task<Result<OrderResult>> UpdateOrderAsync(UpdateOrderCommand command);

    /// <summary>
    /// Cancels an order
    /// </summary>
    Task<Result<OrderResult>> CancelOrderAsync(CancelOrderCommand command);

    /// <summary>
    /// Gets order status and history
    /// </summary>
    Task<Result<OrderStatusResult>> GetOrderStatusAsync(GetOrderStatusQuery query);

    /// <summary>
    /// Updates order status (admin only)
    /// </summary>
    Task<Result<OrderResult>> UpdateOrderStatusAsync(UpdateOrderStatusCommand command);

    /// <summary>
    /// Adds tracking number to an order
    /// </summary>
    Task<Result<OrderResult>> AddTrackingNumberAsync(AddTrackingNumberCommand command);

    /// <summary>
    /// Validates if an order can be modified
    /// </summary>
    Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId);

    /// <summary>
    /// Calculates order totals including tax and shipping
    /// </summary>
    Task<Result<decimal>> CalculateOrderTotalAsync(List<OrderItemViewModel> items, decimal shippingCost = 0, decimal taxRate = 0.08m);
}