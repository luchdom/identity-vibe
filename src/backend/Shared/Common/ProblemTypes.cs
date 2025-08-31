namespace Shared.Common;

/// <summary>
/// Standard problem type URIs for RFC 7807 Problem Details
/// </summary>
public static class ProblemTypes
{
    /// <summary>
    /// Base URI for application-specific problem types
    /// </summary>
    private const string BaseUri = "https://tools.ietf.org/html/rfc7231#section-";

    /// <summary>
    /// Validation error - client provided invalid data
    /// </summary>
    public const string ValidationError = BaseUri + "6.5.1";

    /// <summary>
    /// Business logic error - request violates business rules
    /// </summary>
    public const string BusinessLogicError = BaseUri + "6.5.1";

    /// <summary>
    /// Resource not found error
    /// </summary>
    public const string NotFoundError = BaseUri + "6.5.4";

    /// <summary>
    /// Resource conflict error - typically for duplicate resources
    /// </summary>
    public const string ConflictError = BaseUri + "6.5.8";

    /// <summary>
    /// Unauthorized error - authentication required or failed
    /// </summary>
    public const string UnauthorizedError = BaseUri + "6.5.1";

    /// <summary>
    /// Forbidden error - authenticated but not authorized
    /// </summary>
    public const string ForbiddenError = BaseUri + "6.5.3";

    /// <summary>
    /// Internal server error - unexpected server-side error
    /// </summary>
    public const string InternalServerError = BaseUri + "6.6.1";

    /// <summary>
    /// Service unavailable error - temporary server overload
    /// </summary>
    public const string ServiceUnavailableError = BaseUri + "6.6.4";

    /// <summary>
    /// Rate limit exceeded error
    /// </summary>
    public const string RateLimitError = BaseUri + "6.5.11";
}