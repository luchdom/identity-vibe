namespace Shared.Common;

/// <summary>
/// Categorizes errors for proper HTTP status code mapping and Problem Details classification
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Validation errors - invalid input data, format errors, required field missing
    /// Maps to HTTP 400 Bad Request
    /// </summary>
    Validation,

    /// <summary>
    /// Business logic violations - rules, constraints, workflows
    /// Maps to HTTP 400 Bad Request
    /// </summary>
    BusinessLogic,

    /// <summary>
    /// Resource not found - entity doesn't exist
    /// Maps to HTTP 404 Not Found
    /// </summary>
    NotFound,

    /// <summary>
    /// Resource conflicts - duplicate resources, concurrent modifications
    /// Maps to HTTP 409 Conflict
    /// </summary>
    Conflict,

    /// <summary>
    /// Authentication failures - invalid credentials, missing authentication
    /// Maps to HTTP 401 Unauthorized
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Authorization failures - authenticated but not authorized for operation
    /// Maps to HTTP 403 Forbidden
    /// </summary>
    Forbidden,

    /// <summary>
    /// Unexpected server-side errors - exceptions, system failures
    /// Maps to HTTP 500 Internal Server Error
    /// </summary>
    InternalServer,

    /// <summary>
    /// Service temporarily unavailable - overload, maintenance
    /// Maps to HTTP 503 Service Unavailable
    /// </summary>
    ServiceUnavailable,

    /// <summary>
    /// Rate limiting exceeded - too many requests
    /// Maps to HTTP 429 Too Many Requests
    /// </summary>
    RateLimit
}