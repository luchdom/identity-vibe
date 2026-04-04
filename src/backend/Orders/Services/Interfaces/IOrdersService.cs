using Orders.Models.Commands;
using Orders.Models.Queries;
using Orders.Models.Results;

namespace Orders.Services.Interfaces;

public interface IOrdersService
{
    Task<Result<OrderResult>> CreateOrderAsync(CreateOrderCommand command);
    Task<Result<OrdersListResult>> GetOrdersAsync(GetOrdersQuery query);
    Task<Result<OrdersListResult>> GetAllOrdersAsync(GetAllOrdersQuery query);
    Task<Result<OrderResult>> GetOrderByIdAsync(GetOrderByIdQuery query);
    Task<Result<OrderResult>> UpdateOrderAsync(UpdateOrderCommand command);
    Task<Result<OrderResult>> CancelOrderAsync(CancelOrderCommand command);
    Task<Result<OrderStatusResult>> GetOrderStatusAsync(GetOrderStatusQuery query);
    Task<Result<OrderResult>> UpdateOrderStatusAsync(UpdateOrderStatusCommand command);
    Task<Result<OrderResult>> AddTrackingNumberAsync(AddTrackingNumberCommand command);
    Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId);
    Task<Result<decimal>> CalculateOrderTotalAsync(List<OrderItemResult> items, decimal shippingCost = 0, decimal taxRate = 0.08m);
}
