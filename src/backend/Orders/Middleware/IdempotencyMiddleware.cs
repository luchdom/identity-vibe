using Orders.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Orders.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    // Only apply idempotency to these HTTP methods and endpoints
    private static readonly string[] IdempotentMethods = { "POST", "PUT" };
    private static readonly string[] IdempotentPaths = { "/orders", "/orders/" };

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this request should be handled for idempotency
        if (!ShouldApplyIdempotency(context))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = GetIdempotencyKey(context);
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // No idempotency key provided, proceed normally
            await _next(context);
            return;
        }

        var userId = GetUserId(context);
        if (string.IsNullOrEmpty(userId))
        {
            // User not authenticated, proceed normally (auth will handle this)
            await _next(context);
            return;
        }

        var idempotencyService = context.RequestServices.GetRequiredService<IIdempotencyService>();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();

        // Check if this idempotency key has been used before
        var checkResult = await idempotencyService.CheckIdempotencyAsync(
            idempotencyKey, 
            userId, 
            context.Request.Method, 
            context.Request.Path,
            correlationId);

        if (checkResult.IsFailure)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = checkResult.Error.Message }));
            return;
        }

        var existingRecord = checkResult.Value;
        if (existingRecord != null)
        {
            // Return the cached response
            context.Response.StatusCode = existingRecord.ResponseStatusCode;
            context.Response.ContentType = "application/json";
            
            if (!string.IsNullOrEmpty(existingRecord.ResponseBody))
            {
                await context.Response.WriteAsync(existingRecord.ResponseBody);
            }
            return;
        }

        // Store the idempotency key in the context for the controller to use
        context.Items["IdempotencyKey"] = idempotencyKey;
        context.Items["IdempotencyUserId"] = userId;
        context.Items["IdempotencyCorrelationId"] = correlationId;

        await _next(context);
    }

    private static bool ShouldApplyIdempotency(HttpContext context)
    {
        if (!IdempotentMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
            return false;

        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            return false;

        return IdempotentPaths.Any(idempotentPath => 
            path.StartsWith(idempotentPath.ToLowerInvariant()));
    }

    private static string? GetIdempotencyKey(HttpContext context)
    {
        // Try header first
        var headerKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerKey))
            return headerKey;

        // Try query parameter as fallback
        return context.Request.Query["idempotencyKey"].FirstOrDefault();
    }

    private static string? GetUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        // Try different claim types that might contain the user ID
        return user.FindFirst("sub")?.Value ??
               user.FindFirst("id")?.Value ??
               user.FindFirst("user_id")?.Value ??
               user.FindFirst("nameid")?.Value;
    }
}