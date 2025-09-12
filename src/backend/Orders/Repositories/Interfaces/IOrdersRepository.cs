using Orders.Data.Entities;
using Orders.Models.Enums;
using Orders.Models.ViewModels;
using Shared.Common;

namespace Orders.Repositories.Interfaces;

public interface IOrdersRepository
{
    // Create operations
    Task<Result<OrderViewModel>> CreateOrderAsync(Order order, List<OrderItem> orderItems, OrderStatusHistory statusHistory);
    
    // Read operations
    Task<Result<OrderViewModel?>> GetOrderByIdAsync(int orderId, string? userId = null, bool includeHistory = false);
    Task<Result<(List<OrderViewModel> Orders, int TotalCount)>> GetOrdersAsync(string userId, int page, int pageSize, OrderStatus? status = null);
    Task<Result<(List<OrderViewModel> Orders, int TotalCount)>> GetAllOrdersAsync(int page, int pageSize, OrderStatus? status = null);
    Task<Result<OrderStatusViewModel>> GetOrderStatusAsync(int orderId, string? userId = null);
    
    // Update operations
    Task<Result<OrderViewModel>> UpdateOrderAsync(int orderId, string userId, CustomerViewModel? customer = null, List<OrderItemViewModel>? items = null, ShippingViewModel? shipping = null, string? specialInstructions = null);
    Task<Result<OrderViewModel>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminUserId, string? reason = null, string? notes = null);
    Task<Result<OrderViewModel>> CancelOrderAsync(int orderId, string userId, string? reason = null, string? notes = null);
    Task<Result<OrderViewModel>> AddTrackingNumberAsync(int orderId, string trackingNumber, string adminUserId);
    
    // Business validation
    Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId);
    Task<Result<Customer?>> FindOrCreateCustomerAsync(CustomerViewModel customerData);
    Task<string> GenerateOrderNumberAsync();
}