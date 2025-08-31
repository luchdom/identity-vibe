using Orders.Entities;
using Orders.Entities.Mappers;
using Orders.Models.Commands;
using Orders.Models.Enums;
using Orders.Models.Mappers;
using Orders.Models.Queries;
using Orders.Models.Results;
using Orders.Models.ViewModels;
using Orders.Repositories.Interfaces;
using Orders.Services.Interfaces;
using Shared.Common;

namespace Orders.Services;

public class OrdersService(
    IOrdersRepository ordersRepository) : IOrdersService
{
    // Business constants
    private const decimal DefaultTaxRate = 0.08m; // 8% tax
    private const decimal MinimumOrderAmount = 1.00m;
    private const int MaxItemsPerOrder = 100;

    public async Task<Result<OrderResult>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Validate business rules
        var validationResult = ValidateCreateOrderCommand(command);
        if (validationResult.IsFailure)
            return Result<OrderResult>.Failure(validationResult.Error);

        // Find or create customer using domain model
        var customerData = command.Customer.ToDomain();
        var customerResult = await ordersRepository.FindOrCreateCustomerAsync(customerData);
        if (customerResult.IsFailure)
            return Result<OrderResult>.Failure(customerResult.Error);

        var customer = customerResult.Value!;

        // Generate order number
        var orderNumber = await ordersRepository.GenerateOrderNumberAsync();

        // Convert items to domain
        var domainItems = command.Items.Select(item => item.ToDomain()).ToList();
        
        // Calculate totals
        var subtotal = domainItems.Sum(item => item.TotalPrice);
        var tax = subtotal * DefaultTaxRate;
        var shipping = command.Shipping?.ShippingCost ?? 0;
        var total = subtotal + tax + shipping;

        // Create order entity
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            UserId = command.UserId,
            Status = OrderStatus.Draft,
            SubtotalAmount = subtotal,
            TaxAmount = tax,
            ShippingAmount = shipping,
            TotalAmount = total,
            SpecialInstructions = command.SpecialInstructions,
            CorrelationId = command.CorrelationId,
            IdempotencyKey = command.IdempotencyKey,
            Source = "API"
        };

        // Set shipping info if provided
        if (command.Shipping != null)
        {
            var shippingData = command.Shipping.ToDomain();
            order.ShippingAddress = shippingData.Address;
            order.ShippingCity = shippingData.City;
            order.ShippingState = shippingData.State;
            order.ShippingPostalCode = shippingData.PostalCode;
            order.ShippingCountry = shippingData.Country;
        }

        // Create order items entities
        var orderItems = domainItems.Select(item => item.ToEntity(0)).ToList(); // OrderId will be set by repository

        // Create initial status history
        var statusHistory = new OrderStatusHistory
        {
            OrderId = 0, // Will be set by repository
            FromStatus = OrderStatus.Draft,
            ToStatus = OrderStatus.Draft,
            Reason = "Order created",
            ChangedByUserId = command.UserId,
            CorrelationId = command.CorrelationId
        };

        // Use repository for data persistence
        var repositoryResult = await ordersRepository.CreateOrderAsync(order, orderItems, statusHistory);
        if (repositoryResult.IsFailure)
            return Result<OrderResult>.Failure(repositoryResult.Error);

        // Return domain result
        return Result<OrderResult>.Success(new OrderResult
        {
            Order = repositoryResult.Value,
            CorrelationId = command.CorrelationId
        });
    }

    public async Task<Result<OrdersListResult>> GetOrdersAsync(GetOrdersQuery query)
    {
        var repositoryResult = await ordersRepository.GetOrdersAsync(query.UserId, query.Page, query.PageSize, query.Status);
        if (repositoryResult.IsFailure)
            return Result<OrdersListResult>.Failure(repositoryResult.Error);

        var (orders, totalCount) = repositoryResult.Value;
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        
        var paginationData = new PaginationData
        {
            TotalCount = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = query.Page > 1,
            HasNextPage = query.Page < totalPages
        };

        return Result<OrdersListResult>.Success(new OrdersListResult
        {
            Orders = orders,
            Pagination = paginationData
        });
    }

    public async Task<Result<OrdersListResult>> GetAllOrdersAsync(GetAllOrdersQuery query)
    {
        var repositoryResult = await ordersRepository.GetAllOrdersAsync(query.Page, query.PageSize, query.Status);
        if (repositoryResult.IsFailure)
            return Result<OrdersListResult>.Failure(repositoryResult.Error);

        var (orders, totalCount) = repositoryResult.Value;
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        
        var paginationData = new PaginationData
        {
            TotalCount = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = query.Page > 1,
            HasNextPage = query.Page < totalPages
        };

        return Result<OrdersListResult>.Success(new OrdersListResult
        {
            Orders = orders,
            Pagination = paginationData
        });
    }

    public async Task<Result<OrderResult>> GetOrderByIdAsync(GetOrderByIdQuery query)
    {
        var userId = query.IsAdminRequest ? null : query.UserId;
        var repositoryResult = await ordersRepository.GetOrderByIdAsync(query.OrderId, userId, includeHistory: true);
        
        if (repositoryResult.IsFailure)
            return Result<OrderResult>.Failure(repositoryResult.Error);

        if (repositoryResult.Value == null)
        {
            return Result<OrderResult>.Failure("ORDER_NOT_FOUND", "Order not found or access denied");
        }

        return Result<OrderResult>.Success(new OrderResult
        {
            Order = repositoryResult.Value
        });
    }

    public async Task<Result<OrderResult>> UpdateOrderAsync(UpdateOrderCommand command)
    {
        // Convert command data to domain models
        var customerData = command.Customer?.ToDomain();
        var itemsData = command.Items?.Select(item => item.ToDomain()).ToList();
        var shippingData = command.Shipping?.ToDomain();

        // Use repository for update
        var repositoryResult = await ordersRepository.UpdateOrderAsync(
            command.OrderId, 
            command.UserId, 
            customerData, 
            itemsData, 
            shippingData, 
            command.SpecialInstructions);

        if (repositoryResult.IsFailure)
            return Result<OrderResult>.Failure(repositoryResult.Error);

        return Result<OrderResult>.Success(new OrderResult
        {
            Order = repositoryResult.Value,
            CorrelationId = command.CorrelationId
        });
    }

    public async Task<Result<OrderResult>> CancelOrderAsync(CancelOrderCommand command)
    {
        var repositoryResult = await ordersRepository.CancelOrderAsync(
            command.OrderId, 
            command.UserId, 
            command.CancellationReason, 
            command.Notes);

        if (repositoryResult.IsFailure)
            return Result<OrderResult>.Failure(repositoryResult.Error);

        return Result<OrderResult>.Success(new OrderResult
        {
            Order = repositoryResult.Value,
            CorrelationId = command.CorrelationId
        });
    }

    public async Task<Result<OrderStatusResult>> GetOrderStatusAsync(GetOrderStatusQuery query)
    {
        var userId = query.IsAdminRequest ? null : query.UserId;
        var repositoryResult = await ordersRepository.GetOrderStatusAsync(query.OrderId, userId);
        
        if (repositoryResult.IsFailure)
            return Result<OrderStatusResult>.Failure(repositoryResult.Error);

        return Result<OrderStatusResult>.Success(new OrderStatusResult
        {
            StatusData = repositoryResult.Value
        });
    }

    public async Task<Result<OrderResult>> UpdateOrderStatusAsync(UpdateOrderStatusCommand command)
    {
        var repositoryResult = await ordersRepository.UpdateOrderStatusAsync(
            command.OrderId, 
            command.Status, 
            command.UserId, 
            command.Reason, 
            command.Notes);

        if (repositoryResult.IsFailure)
            return Result<OrderResult>.Failure(repositoryResult.Error);

        return Result<OrderResult>.Success(new OrderResult
        {
            Order = repositoryResult.Value
        });
    }

    public async Task<Result<OrderResult>> AddTrackingNumberAsync(AddTrackingNumberCommand command)
    {
        var repositoryResult = await ordersRepository.AddTrackingNumberAsync(
            command.OrderId, 
            command.TrackingNumber, 
            command.UserId);

        if (repositoryResult.IsFailure)
            return Result<OrderResult>.Failure(repositoryResult.Error);

        return Result<OrderResult>.Success(new OrderResult
        {
            Order = repositoryResult.Value
        });
    }

    public async Task<Result<bool>> CanModifyOrderAsync(int orderId, string userId)
    {
        return await ordersRepository.CanModifyOrderAsync(orderId, userId);
    }

    public async Task<Result<decimal>> CalculateOrderTotalAsync(List<OrderItemData> items, decimal shippingCost = 0, decimal taxRate = DefaultTaxRate)
    {
        try
        {
            var subtotal = items.Sum(item => item.TotalPrice);
            var tax = subtotal * taxRate;
            var total = subtotal + tax + shippingCost;

            return await Task.FromResult(Result<decimal>.Success(total));
        }
        catch (Exception)
        {
            return Result<decimal>.Failure("CALCULATION_ERROR", "An error occurred while calculating order total");
        }
    }

    #region Private Helper Methods

    private static Result ValidateCreateOrderCommand(CreateOrderCommand command)
    {
        if (command.Items.Count == 0)
        {
            return Result.Failure("NO_ITEMS", "Order must contain at least one item");
        }

        if (command.Items.Count > MaxItemsPerOrder)
        {
            return Result.Failure("TOO_MANY_ITEMS", $"Order cannot contain more than {MaxItemsPerOrder} items");
        }

        var subtotal = command.Items.Sum(item => (item.UnitPrice * item.Quantity) - item.DiscountAmount);
        if (subtotal < MinimumOrderAmount)
        {
            return Result.Failure("MINIMUM_ORDER_AMOUNT", $"Order must be at least ${MinimumOrderAmount:F2}");
        }

        // Check for duplicate SKUs
        var duplicateSku = command.Items
            .GroupBy(item => item.ProductSku)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSku != null)
        {
            return Result.Failure("DUPLICATE_SKU", $"Duplicate product SKU found: {duplicateSku.Key}");
        }

        return Result.Success();
    }

    #endregion
}