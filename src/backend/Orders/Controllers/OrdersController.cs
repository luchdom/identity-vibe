using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Models.Requests;
using Orders.Models.Mappers;
using Orders.Models.Enums;
using Orders.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using Shared.Common;
using Shared.Extensions;

namespace Orders.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class OrdersController(
    IOrdersService ordersService,
    IIdempotencyService idempotencyService) : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "orders", timestamp = DateTime.UtcNow });
    }
    /// <summary>
    /// Creates a new order (idempotent)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "UserIdentity")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var correlationId = HttpContext.GetCorrelationId();
        var command = request.ToDomain(userId, correlationId);
        var result = await ordersService.CreateOrderAsync(command);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        // Store idempotency record if key was provided
        var idempotencyKey = HttpContext.Items["IdempotencyKey"] as string;
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var responseBody = JsonSerializer.Serialize(result.Value);
            await idempotencyService.CreateIdempotencyRecordAsync(
                idempotencyKey,
                userId,
                HttpContext.Request.Method,
                HttpContext.Request.Path,
                201,
                responseBody,
                result.Value.Order.Id,
                "Order",
                correlationId);
        }

        var response = result.ToPresentation();
        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Order.Id }, response);
    }

    /// <summary>
    /// Gets orders for the current user with pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "UserIdentity")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] OrderStatus? status = null)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        // Validate pagination parameters
        (page, pageSize) = ValidationExtensions.ValidatePagination(page, pageSize);

        var query = OrderRequestMappers.ToDomain(userId, page, pageSize, status);
        var result = await ordersService.GetOrdersAsync(query);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Gets all orders (admin only) with pagination
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = "ServiceIdentityDelete")]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] OrderStatus? status = null)
    {
        // Validate pagination parameters
        (page, pageSize) = ValidationExtensions.ValidatePagination(page, pageSize);

        var query = OrderRequestMappers.ToDomain(page, pageSize, status);
        var result = await ordersService.GetAllOrdersAsync(query);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific order by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "UserIdentity")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var isAdmin = HttpContext.IsAdmin();
        var query = OrderRequestMappers.ToDomain(id, userId, isAdmin);
        var result = await ordersService.GetOrderByIdAsync(query);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Updates an existing order (idempotent)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "UserIdentity")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var correlationId = HttpContext.GetCorrelationId();
        var command = request.ToDomain(id, userId, correlationId);
        var result = await ordersService.UpdateOrderAsync(command);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        // Store idempotency record if key was provided
        var idempotencyKey = HttpContext.Items["IdempotencyKey"] as string;
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var responseBody = JsonSerializer.Serialize(result.Value);
            await idempotencyService.CreateIdempotencyRecordAsync(
                idempotencyKey,
                userId,
                HttpContext.Request.Method,
                HttpContext.Request.Path,
                200,
                responseBody,
                result.Value.Order.Id,
                "Order",
                correlationId);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Cancels an order (idempotent)
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "UserIdentity")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest? request = null)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        request ??= new CancelOrderRequest();
        var correlationId = HttpContext.GetCorrelationId();
        var command = request.ToDomain(id, userId, correlationId);
        var result = await ordersService.CancelOrderAsync(command);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        // Store idempotency record if key was provided
        var idempotencyKey = HttpContext.Items["IdempotencyKey"] as string;
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var responseBody = JsonSerializer.Serialize(result.Value);
            await idempotencyService.CreateIdempotencyRecordAsync(
                idempotencyKey,
                userId,
                HttpContext.Request.Method,
                HttpContext.Request.Path,
                200,
                responseBody,
                result.Value.Order.Id,
                "Order",
                correlationId);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Gets order status and history
    /// </summary>
    [HttpGet("{id:int}/status")]
    [Authorize(Policy = "UserIdentity")]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var isAdmin = HttpContext.IsAdmin();
        var query = OrderRequestMappers.ToStatusQuery(id, userId, isAdmin);
        var result = await ordersService.GetOrderStatusAsync(query);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Updates order status (admin only)
    /// </summary>
    [HttpPost("{id:int}/status")]
    [AllowAnonymous]
    [Authorize(Policy = "ServiceIdentityUpdate")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var command = request.ToDomain(id, userId);
        var result = await ordersService.UpdateOrderStatusAsync(command);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

    /// <summary>
    /// Adds tracking number to an order (admin only)
    /// </summary>
    [HttpPost("{id:int}/tracking")]
    [Authorize(Policy = "ServiceIdentityUpdate")]
    public async Task<IActionResult> AddTrackingNumber(int id, [FromBody] AddTrackingNumberRequest request)
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var command = request.ToDomain(id, userId);
        var result = await ordersService.AddTrackingNumberAsync(command);

        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.ToPresentation();
        return Ok(response);
    }

}
