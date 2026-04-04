using Orders.Services.Interfaces;
using System.Text.Json;

namespace Orders.Middleware;

public class IdempotencyMiddleware(RequestDelegate next)
{
    private static readonly string[] IdempotentMethods = ["POST", "PUT"];
    private static readonly string[] IdempotentPaths = ["/orders", "/orders/"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldApplyIdempotency(context))
        {
            await next(context);
            return;
        }

        var idempotencyKey = GetIdempotencyKey(context);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var userId = GetUserId(context);
        if (string.IsNullOrWhiteSpace(userId))
        {
            await next(context);
            return;
        }

        var idempotencyService = context.RequestServices.GetRequiredService<IIdempotencyService>();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();

        var checkResult = await idempotencyService.CheckIdempotencyAsync(
            idempotencyKey,
            userId,
            context.Request.Method,
            context.Request.Path,
            correlationId);

        if (!checkResult.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = checkResult.ToError().Message }));
            return;
        }

        if (checkResult.Value != null)
        {
            context.Response.StatusCode = checkResult.Value.ResponseStatusCode;
            context.Response.ContentType = "application/json";

            if (!string.IsNullOrWhiteSpace(checkResult.Value.ResponseBody))
            {
                await context.Response.WriteAsync(checkResult.Value.ResponseBody);
            }

            return;
        }

        context.Items["IdempotencyKey"] = idempotencyKey;
        context.Items["IdempotencyUserId"] = userId;
        context.Items["IdempotencyCorrelationId"] = correlationId;

        await next(context);
    }

    private static bool ShouldApplyIdempotency(HttpContext context)
    {
        if (!IdempotentMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(path)
            && IdempotentPaths.Any(idempotentPath => path.StartsWith(idempotentPath, StringComparison.Ordinal));
    }

    private static string? GetIdempotencyKey(HttpContext context)
    {
        return context.Request.Headers["Idempotency-Key"].FirstOrDefault()
            ?? context.Request.Query["idempotencyKey"].FirstOrDefault();
    }

    private static string? GetUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value
            ?? user.FindFirst("user_id")?.Value
            ?? user.FindFirst("nameid")?.Value;
    }
}
