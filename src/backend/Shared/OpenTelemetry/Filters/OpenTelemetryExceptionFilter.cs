using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.OpenTelemetry.Filters;

/// <summary>
/// Automatic exception enrichment filter that adds business context to OpenTelemetry traces
/// without requiring manual code changes in controllers
/// </summary>
public class OpenTelemetryExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        // Automatically enrich the trace with exception details
        activity.SetTag("error", true);
        activity.SetTag("error.type", context.Exception.GetType().Name);
        activity.SetTag("error.message", context.Exception.Message);
        
        // Add business context based on the controller/action
        var actionDescriptor = context.ActionDescriptor;
        activity.SetTag("controller", actionDescriptor.RouteValues["controller"]);
        activity.SetTag("action", actionDescriptor.RouteValues["action"]);
        
        // Add user context if available
        if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            activity.SetTag("user.id", context.HttpContext.User.Identity.Name);
            activity.SetTag("error.user_authenticated", true);
        }
        
        // Add security context for authentication/authorization errors
        if (IsSecurityRelated(context.Exception))
        {
            activity.SetTag("security.event", true);
            activity.SetTag("security.event.type", GetSecurityEventType(context.Exception));
        }

        // Add exception event with full details
        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, 
            new ActivityTagsCollection(new[]
            {
                new KeyValuePair<string, object?>("exception.type", context.Exception.GetType().FullName),
                new KeyValuePair<string, object?>("exception.message", context.Exception.Message),
                new KeyValuePair<string, object?>("exception.stacktrace", context.Exception.StackTrace)
            })));
    }

    private static bool IsSecurityRelated(Exception exception)
    {
        var typeName = exception.GetType().Name;
        return typeName.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Authorization", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSecurityEventType(Exception exception)
    {
        var typeName = exception.GetType().Name.ToLowerInvariant();
        
        if (typeName.Contains("authentication"))
            return "authentication_failure";
        if (typeName.Contains("authorization") || typeName.Contains("forbidden"))
            return "authorization_failure";
        if (typeName.Contains("unauthorized"))
            return "unauthorized_access";
            
        return "security_exception";
    }
}