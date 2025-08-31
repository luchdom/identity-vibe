using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.OpenTelemetry.Filters;

/// <summary>
/// Automatic action filter that enriches OpenTelemetry traces with business context
/// without requiring manual code changes in controllers
/// </summary>
public class OpenTelemetryActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        // Add business context to the trace
        var actionDescriptor = context.ActionDescriptor;
        activity.SetTag("controller", actionDescriptor.RouteValues["controller"]);
        activity.SetTag("action", actionDescriptor.RouteValues["action"]);
        
        // Add user context
        if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            activity.SetTag("user.id", context.HttpContext.User.Identity.Name);
            activity.SetTag("user.authenticated", true);
            
            // Add user claims for business context
            var claims = context.HttpContext.User.Claims;
            var roles = claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
            if (roles.Any())
            {
                activity.SetTag("user.roles", string.Join(",", roles));
            }
        }
        
        // Add request parameters (non-sensitive ones)
        foreach (var param in context.ActionArguments)
        {
            if (!IsSensitiveParameter(param.Key))
            {
                activity.SetTag($"action.parameter.{param.Key}", param.Value?.ToString());
            }
        }

        // Add business operation type based on HTTP method and controller
        var httpMethod = context.HttpContext.Request.Method;
        var controller = actionDescriptor.RouteValues["controller"]?.ToLower();
        var action = actionDescriptor.RouteValues["action"]?.ToLower();
        
        activity.SetTag("business.operation.type", GetBusinessOperationType(httpMethod, controller, action));
        
        // Add timing start event
        activity.AddEvent(new ActivityEvent($"{controller}.{action}.start", DateTimeOffset.UtcNow));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        var actionDescriptor = context.ActionDescriptor;
        var controller = actionDescriptor.RouteValues["controller"]?.ToLower();
        var action = actionDescriptor.RouteValues["action"]?.ToLower();
        
        // Add result information
        if (context.Result != null)
        {
            activity.SetTag("action.result.type", context.Result.GetType().Name);
        }
        
        // Add timing end event
        activity.AddEvent(new ActivityEvent($"{controller}.{action}.end", DateTimeOffset.UtcNow));
        
        // Add business success/failure context
        var isSuccess = context.Exception == null && 
                       (context.HttpContext.Response.StatusCode < 400);
        activity.SetTag("business.operation.success", isSuccess);
        
        if (!isSuccess && context.Exception == null)
        {
            // HTTP error without exception
            activity.SetTag("business.operation.failure_reason", "http_error");
        }
    }

    private static bool IsSensitiveParameter(string parameterName)
    {
        var sensitiveParams = new[] { "password", "secret", "key", "token", "auth" };
        return sensitiveParams.Any(sensitive => 
            parameterName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetBusinessOperationType(string httpMethod, string? controller, string? action)
    {
        return (controller, action, httpMethod.ToUpper()) switch
        {
            ("account", "login", _) => "authentication",
            ("account", "register", _) => "user_registration",
            ("account", "logout", _) => "user_logout",
            ("authorization", _, _) => "token_generation",
            ("data", _, "GET") => "data_read",
            ("data", _, "POST") => "data_create",
            ("data", _, "PUT") => "data_update",
            ("data", _, "DELETE") => "data_delete",
            (_, _, "GET") => "read_operation",
            (_, _, "POST") => "create_operation",
            (_, _, "PUT") => "update_operation",
            (_, _, "PATCH") => "update_operation",
            (_, _, "DELETE") => "delete_operation",
            _ => "unknown_operation"
        };
    }
}