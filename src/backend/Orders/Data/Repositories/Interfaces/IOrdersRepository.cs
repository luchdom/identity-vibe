using Orders.Data.Entities;
using Orders.Models.Enums;
using Orders.Models.Results;

namespace Orders.Data.Repositories.Interfaces;

public interface IOrdersRepository
{
    Task<OrderDetailsResult> CreateOrderAsync(Order order, List<OrderItem> orderItems, OrderStatusHistory statusHistory);
    Task<OrderDetailsResult?> GetOrderByIdAsync(int orderId, string? userId = null, bool includeHistory = false);
    Task<(List<OrderDetailsResult> Orders, int TotalCount)> GetOrdersAsync(string userId, int page, int pageSize, OrderStatus? status = null);
    Task<(List<OrderDetailsResult> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize, OrderStatus? status = null);
    Task<OrderStatusDetailsResult?> GetOrderStatusAsync(int orderId, string? userId = null);
    Task<OrderDetailsResult?> UpdateOrderAsync(int orderId, string userId, CustomerResult? customer = null, List<OrderItemResult>? items = null, ShippingResult? shipping = null, string? specialInstructions = null);
    Task<OrderDetailsResult?> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminUserId, string? reason = null, string? notes = null);
    Task<OrderDetailsResult?> CancelOrderAsync(int orderId, string userId, string? reason = null, string? notes = null);
    Task<OrderDetailsResult?> AddTrackingNumberAsync(int orderId, string trackingNumber, string adminUserId);
    Task<bool?> CanModifyOrderAsync(int orderId, string userId);
    Task<Customer> FindOrCreateCustomerAsync(CustomerResult customerData);
    Task<string> GenerateOrderNumberAsync();
}
