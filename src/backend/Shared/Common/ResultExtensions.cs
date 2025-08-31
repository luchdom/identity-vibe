using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Common;

/// <summary>
/// Extension methods for Result types to integrate with ASP.NET Core
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts Result<T> to IActionResult with proper HTTP status codes
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        return result.Match<IActionResult>(
            onSuccess: value => controller.Ok(value),
            onFailure: error => MapErrorToActionResult(error, controller)
        );
    }

    /// <summary>
    /// Converts Result to IActionResult with proper HTTP status codes
    /// </summary>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        return result.Match<IActionResult>(
            onSuccess: () => controller.Ok(),
            onFailure: error => MapErrorToActionResult(error, controller)
        );
    }

    /// <summary>
    /// Converts Result<T> to IActionResult with custom success response
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, object> successMapper)
    {
        return result.Match<IActionResult>(
            onSuccess: value => controller.Ok(successMapper(value)),
            onFailure: error => MapErrorToActionResult(error, controller)
        );
    }

    /// <summary>
    /// Converts Result<T> to IActionResult with custom success status
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, IActionResult> successMapper)
    {
        return result.Match(
            onSuccess: successMapper,
            onFailure: error => MapErrorToActionResult(error, controller)
        );
    }

    /// <summary>
    /// Executes async operation and wraps exceptions in Result
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure("OPERATION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Executes async operation and wraps exceptions in Result
    /// </summary>
    public static async Task<Result> TryAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("OPERATION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Combines multiple results into one - succeeds only if all succeed
    /// </summary>
    public static Result<T[]> Combine<T>(params Result<T>[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToArray();
        if (failures.Any())
        {
            var errors = failures.Select(f => f.Error.Message);
            return Result<T[]>.Failure("MULTIPLE_FAILURES", string.Join("; ", errors));
        }

        var values = results.Select(r => r.Value).ToArray();
        return Result<T[]>.Success(values);
    }

    /// <summary>
    /// Maps error codes to appropriate HTTP status codes
    /// </summary>
    private static IActionResult MapErrorToActionResult(Error error, ControllerBase controller)
    {
        var errorResponse = new { 
            success = false, 
            message = error.Message, 
            code = error.Code 
        };

        return error.Code switch
        {
            // Authentication & Authorization
            "AUTH_INVALID_CREDENTIALS" or "AUTH_USER_NOT_FOUND" => controller.BadRequest(errorResponse),
            "AUTH_UNAUTHORIZED" or "FORBIDDEN" => controller.Forbid(),
            "AUTH_TOKEN_INVALID" => controller.Unauthorized(errorResponse),

            // Validation & Client Errors
            "VALIDATION_FAILED" or "REQUIRED_FIELD" => controller.BadRequest(errorResponse),
            "NOT_FOUND" => controller.NotFound(errorResponse),
            "CONFLICT" or "AUTH_USER_EXISTS" => controller.Conflict(errorResponse),

            // Server Errors
            "DATABASE_ERROR" or "EXTERNAL_SERVICE_ERROR" or "UNEXPECTED_ERROR" => 
                controller.StatusCode(500, errorResponse),

            // Default to BadRequest for unknown codes
            _ => controller.BadRequest(errorResponse)
        };
    }

    /// <summary>
    /// Converts Result<T> to IActionResult using Problem Details format
    /// </summary>
    /// <typeparam name="T">The result value type</typeparam>
    /// <param name="result">The result to convert</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <returns>IActionResult with Problem Details on failure or Ok on success</returns>
    public static IActionResult ToActionResultWithProblemDetails<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    /// <summary>
    /// Converts Result to IActionResult using Problem Details format
    /// </summary>
    /// <param name="result">The result to convert</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <returns>IActionResult with Problem Details on failure or Ok on success</returns>
    public static IActionResult ToActionResultWithProblemDetails(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
            return new OkResult();

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    /// <summary>
    /// Converts Result<T> to IActionResult using Problem Details format with custom success response
    /// </summary>
    /// <typeparam name="T">The result value type</typeparam>
    /// <param name="result">The result to convert</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <param name="successMapper">Function to map success value to response object</param>
    /// <returns>IActionResult with Problem Details on failure or custom success response</returns>
    public static IActionResult ToActionResultWithProblemDetails<T>(this Result<T> result, HttpContext context, Func<T, object> successMapper)
    {
        if (result.IsSuccess)
            return new OkObjectResult(successMapper(result.Value));

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    /// <summary>
    /// Converts Result<T> to IActionResult using Problem Details format with custom success action result
    /// </summary>
    /// <typeparam name="T">The result value type</typeparam>
    /// <param name="result">The result to convert</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <param name="successMapper">Function to map success value to IActionResult</param>
    /// <returns>IActionResult with Problem Details on failure or custom success result</returns>
    public static IActionResult ToActionResultWithProblemDetails<T>(this Result<T> result, HttpContext context, Func<T, IActionResult> successMapper)
    {
        if (result.IsSuccess)
            return successMapper(result.Value);

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    /// <summary>
    /// Converts Result<T> failure to Problem Details
    /// </summary>
    /// <typeparam name="T">The result value type</typeparam>
    /// <param name="result">The failed result</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <returns>ProblemDetail instance</returns>
    public static ProblemDetail ToProblemDetails<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert successful result to Problem Details");

        return CreateProblemDetails(result.Error, context);
    }

    /// <summary>
    /// Converts Result failure to Problem Details
    /// </summary>
    /// <param name="result">The failed result</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <returns>ProblemDetail instance</returns>
    public static ProblemDetail ToProblemDetails(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert successful result to Problem Details");

        return CreateProblemDetails(result.Error, context);
    }

    /// <summary>
    /// Creates Problem Details from error and HTTP context
    /// </summary>
    /// <param name="error">The error to convert</param>
    /// <param name="context">The HTTP context for correlation ID and instance path</param>
    /// <returns>ProblemDetail instance</returns>
    private static ProblemDetail CreateProblemDetails(Error error, HttpContext context)
    {
        var category = ErrorMapping.GetCategory(error.Code);
        var statusCode = ErrorMapping.GetStatusCode(category);
        var problemType = ErrorMapping.GetProblemType(category);
        var title = ErrorMapping.GetProblemTitle(category);

        // Get correlation ID from various sources
        var traceId = GetTraceId(context);
        
        // Get instance path
        var instance = GetInstancePath(context);

        // Create errors dictionary for validation-like errors
        Dictionary<string, string[]>? errors = null;
        if (category == ErrorCategory.Validation)
        {
            errors = new Dictionary<string, string[]>
            {
                [error.Code] = new[] { error.Message }
            };
        }

        return new ProblemDetail
        {
            Type = problemType,
            Title = title,
            Status = statusCode,
            Detail = error.Message,
            Instance = instance,
            TraceId = traceId,
            Errors = errors
        };
    }

    /// <summary>
    /// Gets trace/correlation ID from HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>Trace ID string or null</returns>
    private static string? GetTraceId(HttpContext context)
    {
        // Try various correlation ID headers and sources
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
               context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
               context.TraceIdentifier;
    }

    /// <summary>
    /// Gets instance path from HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>Instance path string or null</returns>
    private static string? GetInstancePath(HttpContext context)
    {
        return context.Request.Path.HasValue ? context.Request.Path.Value : null;
    }

    /// <summary>
    /// Creates a validation failure result with multiple errors
    /// </summary>
    public static Result<T> ValidationFailure<T>(params (string Field, string Message)[] errors)
    {
        var errorMessages = errors.Select(e => $"{e.Field}: {e.Message}");
        return Result<T>.Failure("VALIDATION_FAILED", string.Join("; ", errorMessages));
    }

    /// <summary>
    /// Creates a validation failure result from model state
    /// </summary>
    public static Result<T> ValidationFailure<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
        
        return Result<T>.Failure("VALIDATION_FAILED", string.Join("; ", errors));
    }

    /// <summary>
    /// Creates unauthorized result
    /// </summary>
    public static Result<T> Unauthorized<T>(string message = "Access denied")
    {
        return Result<T>.Failure("UNAUTHORIZED", message);
    }

    /// <summary>
    /// Creates forbidden result  
    /// </summary>
    public static Result<T> Forbidden<T>(string message = "Insufficient permissions")
    {
        return Result<T>.Failure("FORBIDDEN", message);
    }

    /// <summary>
    /// Creates not found result
    /// </summary>
    public static Result<T> NotFound<T>(string resource = "Resource")
    {
        return Result<T>.Failure("NOT_FOUND", $"{resource} not found");
    }

    /// <summary>
    /// Requires user ID and creates failure result if not present
    /// </summary>
    public static Result<T> RequireUserId<T>(string? userId)
    {
        return string.IsNullOrEmpty(userId) 
            ? Result<T>.Failure("INVALID_REQUEST", "User ID not found in token")
            : Result<T>.Success(default(T)!);
    }
}