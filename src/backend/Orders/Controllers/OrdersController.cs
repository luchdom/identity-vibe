using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Models.Enums;
using Orders.Models.Mappers;
using Orders.Models.Requests;
using Orders.Services.Interfaces;
using Shared.Extensions;
using System.Text.Json;

namespace Orders.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class OrdersController(
    IOrdersService ordersService,
    IIdempotencyService idempotencyService,
    IValidator<CreateOrderRequest> createOrderValidator,
    IValidator<UpdateOrderRequest> updateOrderValidator,
    IValidator<CancelOrderRequest> cancelOrderValidator,
    IValidator<UpdateOrderStatusRequest> updateOrderStatusValidator,
    IValidator<AddTrackingNumberRequest> addTrackingNumberValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "orders.write")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest? request)
    {
        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var validationResult = await createOrderValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var correlationId = HttpContext.GetCorrelationId();
        var userId = HttpContext.GetUserId()!;
        var result = await ordersService.CreateOrderAsync(validationResult.Value.ToDomain(userId, correlationId));

        if (!result.IsSuccess)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var idempotencyKey = HttpContext.Items["IdempotencyKey"] as string;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await idempotencyService.CreateIdempotencyRecordAsync(
                idempotencyKey,
                userId,
                HttpContext.Request.Method,
                HttpContext.Request.Path,
                StatusCodes.Status201Created,
                JsonSerializer.Serialize(result.Value),
                result.Value.Order.Id,
                "Order",
                correlationId);
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Order.Id }, result.ToPresentation());
    }

    [HttpGet]
    [Authorize(Policy = "orders.read")]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] OrderStatus? status = null)
    {
        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        (page, pageSize) = ValidationExtensions.ValidatePagination(page, pageSize);
        var result = await ordersService.GetOrdersAsync(OrderRequestMappers.ToDomain(HttpContext.GetUserId()!, page, pageSize, status));
        return result.ToActionResultWithProblemDetails(HttpContext, value => value.ToPresentation());
    }

    [HttpGet("all")]
    [Authorize(Policy = "orders.manage")]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] OrderStatus? status = null)
    {
        (page, pageSize) = ValidationExtensions.ValidatePagination(page, pageSize);
        var result = await ordersService.GetAllOrdersAsync(OrderRequestMappers.ToDomain(page, pageSize, status));
        return result.ToActionResultWithProblemDetails(HttpContext, value => value.ToPresentation());
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "orders.read")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        if (id <= 0)
        {
            return ResultExtensions.ValidationFailure<object>(("id", "id must be greater than zero."))
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await ordersService.GetOrderByIdAsync(OrderRequestMappers.ToDomain(id, HttpContext.GetUserId()!, HttpContext.IsAdmin()));
        return result.ToActionResultWithProblemDetails(HttpContext, value => value.ToPresentation());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "orders.write")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest? request)
    {
        if (id <= 0)
        {
            return ResultExtensions.ValidationFailure<object>(("id", "id must be greater than zero."))
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var validationResult = await updateOrderValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var correlationId = HttpContext.GetCorrelationId();
        var userId = HttpContext.GetUserId()!;
        var result = await ordersService.UpdateOrderAsync(validationResult.Value.ToDomain(id, userId, correlationId));

        if (!result.IsSuccess)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var idempotencyKey = HttpContext.Items["IdempotencyKey"] as string;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await idempotencyService.CreateIdempotencyRecordAsync(
                idempotencyKey,
                userId,
                HttpContext.Request.Method,
                HttpContext.Request.Path,
                StatusCodes.Status200OK,
                JsonSerializer.Serialize(result.Value),
                result.Value.Order.Id,
                "Order",
                correlationId);
        }

        return Ok(result.ToPresentation());
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "orders.write")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest? request = null)
    {
        if (id <= 0)
        {
            return ResultExtensions.ValidationFailure<object>(("id", "id must be greater than zero."))
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        request ??= new CancelOrderRequest();
        var validationResult = await cancelOrderValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var correlationId = HttpContext.GetCorrelationId();
        var userId = HttpContext.GetUserId()!;
        var result = await ordersService.CancelOrderAsync(validationResult.Value.ToDomain(id, userId, correlationId));

        if (!result.IsSuccess)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var idempotencyKey = HttpContext.Items["IdempotencyKey"] as string;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await idempotencyService.CreateIdempotencyRecordAsync(
                idempotencyKey,
                userId,
                HttpContext.Request.Method,
                HttpContext.Request.Path,
                StatusCodes.Status200OK,
                JsonSerializer.Serialize(result.Value),
                result.Value.Order.Id,
                "Order",
                correlationId);
        }

        return Ok(result.ToPresentation());
    }

    [HttpGet("{id:int}/status")]
    [Authorize(Policy = "orders.read")]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        if (id <= 0)
        {
            return ResultExtensions.ValidationFailure<object>(("id", "id must be greater than zero."))
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await ordersService.GetOrderStatusAsync(OrderRequestMappers.ToStatusQuery(id, HttpContext.GetUserId()!, HttpContext.IsAdmin()));
        return result.ToActionResultWithProblemDetails(HttpContext, value => value.ToPresentation());
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Policy = "orders.manage")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest? request)
    {
        if (id <= 0)
        {
            return ResultExtensions.ValidationFailure<object>(("id", "id must be greater than zero."))
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var validationResult = await updateOrderStatusValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await ordersService.UpdateOrderStatusAsync(validationResult.Value.ToDomain(id, HttpContext.GetUserId()!));
        return result.ToActionResultWithProblemDetails(HttpContext, value => value.ToPresentation());
    }

    [HttpPost("{id:int}/tracking")]
    [Authorize(Policy = "orders.manage")]
    public async Task<IActionResult> AddTrackingNumber(int id, [FromBody] AddTrackingNumberRequest? request)
    {
        if (id <= 0)
        {
            return ResultExtensions.ValidationFailure<object>(("id", "id must be greater than zero."))
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var userIdResult = Shared.Common.ResultExtensions.RequireUserId<object>(HttpContext.GetUserId());
        if (!userIdResult.IsSuccess)
        {
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var validationResult = await addTrackingNumberValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await ordersService.AddTrackingNumberAsync(validationResult.Value.ToDomain(id, HttpContext.GetUserId()!));
        return result.ToActionResultWithProblemDetails(HttpContext, value => value.ToPresentation());
    }
}
