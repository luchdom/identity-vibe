using System.Text.Json.Serialization;

namespace Shared.Common;

/// <summary>
/// RFC 7807 compliant Problem Details model for consistent error responses
/// </summary>
public record ProblemDetail
{
    /// <summary>
    /// A URI reference that identifies the problem type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "about:blank";

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The HTTP status code
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    /// <summary>
    /// Correlation/Trace ID for request tracking
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Additional error details grouped by error code
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Additional extension members
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; init; }

    /// <summary>
    /// Creates a Problem Details instance for validation errors
    /// </summary>
    public static ProblemDetail Validation(
        string detail,
        string? instance = null,
        string? traceId = null,
        Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.ValidationError,
            Title = "Validation Error",
            Status = 400,
            Detail = detail,
            Instance = instance,
            TraceId = traceId,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a Problem Details instance for business logic errors
    /// </summary>
    public static ProblemDetail BusinessLogic(
        string detail,
        string? instance = null,
        string? traceId = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.BusinessLogicError,
            Title = "Business Logic Error",
            Status = 400,
            Detail = detail,
            Instance = instance,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a Problem Details instance for not found errors
    /// </summary>
    public static ProblemDetail NotFound(
        string detail,
        string? instance = null,
        string? traceId = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.NotFoundError,
            Title = "Resource Not Found",
            Status = 404,
            Detail = detail,
            Instance = instance,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a Problem Details instance for conflict errors
    /// </summary>
    public static ProblemDetail Conflict(
        string detail,
        string? instance = null,
        string? traceId = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.ConflictError,
            Title = "Resource Conflict",
            Status = 409,
            Detail = detail,
            Instance = instance,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a Problem Details instance for unauthorized errors
    /// </summary>
    public static ProblemDetail Unauthorized(
        string detail,
        string? instance = null,
        string? traceId = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.UnauthorizedError,
            Title = "Unauthorized",
            Status = 401,
            Detail = detail,
            Instance = instance,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a Problem Details instance for forbidden errors
    /// </summary>
    public static ProblemDetail Forbidden(
        string detail,
        string? instance = null,
        string? traceId = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.ForbiddenError,
            Title = "Forbidden",
            Status = 403,
            Detail = detail,
            Instance = instance,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a Problem Details instance for internal server errors
    /// </summary>
    public static ProblemDetail InternalServerError(
        string detail,
        string? instance = null,
        string? traceId = null)
    {
        return new ProblemDetail
        {
            Type = ProblemTypes.InternalServerError,
            Title = "Internal Server Error",
            Status = 500,
            Detail = detail,
            Instance = instance,
            TraceId = traceId
        };
    }
}