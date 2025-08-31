using Orders.Entities;
using Orders.Models.Enums;
using Orders.Models.ViewModels;
using Shared.Common;

namespace Orders.Repositories.Interfaces;

public interface IOrdersRepository
{
    // Create operations
    Task<Result<OrderData>> CreateOrderAsync(Order order, List<OrderItem> orderItems, OrderStatusHistory statusHistory);
    
    // Read operations
    Task<Result<OrderData?>> GetOrderByIdAsync(int orderId, string? userId = null, bool includeHistory = false);
    Task<Result<(List<OrderData> Orders, int TotalCount)>> GetOrdersAsync(string userId, int page, int pageSize, OrderStatus? status = null);
    Task<Result<(List<OrderData> Orders, int TotalCount)>> GetAllOrdersAsync(int page, int pageSize, OrderStatus? status = null);
    Task<Result<OrderStatusData>> GetOrderStatusAsync(int orderId, string? userId = null);
    
    // Update operations
    Task<Result<OrderData>> UpdateOrderAsync(int orderId, string userId, CustomerData? customer = null, List<OrderItemData>? items = null, ShippingData? shipping = null, string? specialInstructions = null);
    Task<Result<OrderData>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminUserId, string? reason = null, string? notes = null);
    Task<Result<OrderData>> CancelOrderAsync(int orderId, string userId, string? reason = null, string? notes = null);
    Task<Result<OrderData>> AddTrackingNumberAsync(int orderId, string trackingNumber, string adminUserId);
    
    // Business validation
    Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId);
    Task<Result<Customer?>> FindOrCreateCustomerAsync(CustomerData customerData);
    Task<string> GenerateOrderNumberAsync();
}