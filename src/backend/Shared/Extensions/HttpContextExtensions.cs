using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Shared.Extensions;

/// <summary>
/// HTTP Context extensions for common request processing patterns
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the user ID from JWT claims using multiple fallback strategies
    /// </summary>
    public static string? GetUserId(this HttpContext context)
    {
        return context.User.GetUserId();
    }

    /// <summary>
    /// Checks if the current user has admin privileges
    /// </summary>
    public static bool IsAdmin(this HttpContext context)
    {
        return context.User.IsAdmin();
    }

    /// <summary>
    /// Gets correlation ID from various request sources
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
               context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
               context.TraceIdentifier;
    }

    /// <summary>
    /// Gets client IP address with proxy support
    /// </summary>
    public static string? GetClientIpAddress(this HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets user agent string
    /// </summary>
    public static string? GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Creates request context object for command mapping
    /// </summary>
    public static RequestContext GetRequestContext(this HttpContext context)
    {
        return new RequestContext
        {
            UserId = context.GetUserId(),
            CorrelationId = context.GetCorrelationId(),
            IpAddress = context.GetClientIpAddress(),
            UserAgent = context.GetUserAgent(),
            IsAdmin = context.IsAdmin()
        };
    }
}

/// <summary>
/// Request context data for command mapping
/// </summary>
public record RequestContext
{
    public string? UserId { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsAdmin { get; init; }
}