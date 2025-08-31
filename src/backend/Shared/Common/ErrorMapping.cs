namespace Shared.Common;

/// <summary>
/// Maps error codes to HTTP status codes and Problem Details categories
/// </summary>
public static class ErrorMapping
{
    /// <summary>
    /// Maps error codes to their corresponding error categories
    /// </summary>
    private static readonly Dictionary<string, ErrorCategory> ErrorCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Authentication & Authorization
        ["AUTH_INVALID_CREDENTIALS"] = ErrorCategory.Unauthorized,
        ["AUTH_USER_NOT_FOUND"] = ErrorCategory.Unauthorized,
        ["AUTH_TOKEN_INVALID"] = ErrorCategory.Unauthorized,
        ["INVALID_CREDENTIALS"] = ErrorCategory.Unauthorized,
        ["AUTH_UNAUTHORIZED"] = ErrorCategory.Unauthorized,
        ["FORBIDDEN"] = ErrorCategory.Forbidden,
        ["ACCESS_DENIED"] = ErrorCategory.Forbidden,

        // Validation Errors
        ["VALIDATION_FAILED"] = ErrorCategory.Validation,
        ["VALIDATION_ERROR"] = ErrorCategory.Validation,
        ["REQUIRED_FIELD"] = ErrorCategory.Validation,
        ["INVALID_FORMAT"] = ErrorCategory.Validation,
        ["INVALID_EMAIL"] = ErrorCategory.Validation,
        ["INVALID_PASSWORD"] = ErrorCategory.Validation,
        ["INVALID_INPUT"] = ErrorCategory.Validation,
        ["INVALID_REQUEST"] = ErrorCategory.Validation,

        // Business Logic
        ["BUSINESS_RULE_VIOLATION"] = ErrorCategory.BusinessLogic,
        ["INVALID_OPERATION"] = ErrorCategory.BusinessLogic,
        ["OPERATION_NOT_ALLOWED"] = ErrorCategory.BusinessLogic,
        ["INVALID_STATE"] = ErrorCategory.BusinessLogic,
        ["INVALID_ORDER_STATUS"] = ErrorCategory.BusinessLogic,
        ["ORDER_CANNOT_BE_CANCELLED"] = ErrorCategory.BusinessLogic,
        ["ORDER_CANNOT_BE_MODIFIED"] = ErrorCategory.BusinessLogic,

        // Not Found
        ["NOT_FOUND"] = ErrorCategory.NotFound,
        ["ORDER_NOT_FOUND"] = ErrorCategory.NotFound,
        ["USER_NOT_FOUND"] = ErrorCategory.NotFound,
        ["RESOURCE_NOT_FOUND"] = ErrorCategory.NotFound,

        // Conflicts
        ["CONFLICT"] = ErrorCategory.Conflict,
        ["AUTH_USER_EXISTS"] = ErrorCategory.Conflict,
        ["USER_ALREADY_EXISTS"] = ErrorCategory.Conflict,
        ["DUPLICATE_RESOURCE"] = ErrorCategory.Conflict,
        ["CONCURRENT_MODIFICATION"] = ErrorCategory.Conflict,

        // Server Errors
        ["DATABASE_ERROR"] = ErrorCategory.InternalServer,
        ["EXTERNAL_SERVICE_ERROR"] = ErrorCategory.InternalServer,
        ["UNEXPECTED_ERROR"] = ErrorCategory.InternalServer,
        ["GENERAL_ERROR"] = ErrorCategory.InternalServer,
        ["OPERATION_FAILED"] = ErrorCategory.InternalServer,
        ["SYSTEM_ERROR"] = ErrorCategory.InternalServer,

        // Service Unavailable
        ["SERVICE_UNAVAILABLE"] = ErrorCategory.ServiceUnavailable,
        ["TEMPORARY_UNAVAILABLE"] = ErrorCategory.ServiceUnavailable,

        // Rate Limiting
        ["RATE_LIMIT_EXCEEDED"] = ErrorCategory.RateLimit,
        ["TOO_MANY_REQUESTS"] = ErrorCategory.RateLimit,
    };

    /// <summary>
    /// Maps error categories to HTTP status codes
    /// </summary>
    private static readonly Dictionary<ErrorCategory, int> StatusCodeMap = new()
    {
        [ErrorCategory.Validation] = 400,
        [ErrorCategory.BusinessLogic] = 400,
        [ErrorCategory.NotFound] = 404,
        [ErrorCategory.Conflict] = 409,
        [ErrorCategory.Unauthorized] = 401,
        [ErrorCategory.Forbidden] = 403,
        [ErrorCategory.InternalServer] = 500,
        [ErrorCategory.ServiceUnavailable] = 503,
        [ErrorCategory.RateLimit] = 429
    };

    /// <summary>
    /// Maps error categories to Problem Details types
    /// </summary>
    private static readonly Dictionary<ErrorCategory, string> ProblemTypeMap = new()
    {
        [ErrorCategory.Validation] = ProblemTypes.ValidationError,
        [ErrorCategory.BusinessLogic] = ProblemTypes.BusinessLogicError,
        [ErrorCategory.NotFound] = ProblemTypes.NotFoundError,
        [ErrorCategory.Conflict] = ProblemTypes.ConflictError,
        [ErrorCategory.Unauthorized] = ProblemTypes.UnauthorizedError,
        [ErrorCategory.Forbidden] = ProblemTypes.ForbiddenError,
        [ErrorCategory.InternalServer] = ProblemTypes.InternalServerError,
        [ErrorCategory.ServiceUnavailable] = ProblemTypes.ServiceUnavailableError,
        [ErrorCategory.RateLimit] = ProblemTypes.RateLimitError
    };

    /// <summary>
    /// Maps error categories to Problem Details titles
    /// </summary>
    private static readonly Dictionary<ErrorCategory, string> ProblemTitleMap = new()
    {
        [ErrorCategory.Validation] = "Validation Error",
        [ErrorCategory.BusinessLogic] = "Business Logic Error",
        [ErrorCategory.NotFound] = "Resource Not Found",
        [ErrorCategory.Conflict] = "Resource Conflict",
        [ErrorCategory.Unauthorized] = "Unauthorized",
        [ErrorCategory.Forbidden] = "Forbidden",
        [ErrorCategory.InternalServer] = "Internal Server Error",
        [ErrorCategory.ServiceUnavailable] = "Service Unavailable",
        [ErrorCategory.RateLimit] = "Rate Limit Exceeded"
    };

    /// <summary>
    /// Gets the error category for a given error code
    /// </summary>
    /// <param name="errorCode">The error code to categorize</param>
    /// <returns>The error category, defaults to BusinessLogic for unknown codes</returns>
    public static ErrorCategory GetCategory(string errorCode)
    {
        return ErrorCategoryMap.GetValueOrDefault(errorCode, ErrorCategory.BusinessLogic);
    }

    /// <summary>
    /// Gets the HTTP status code for a given error category
    /// </summary>
    /// <param name="category">The error category</param>
    /// <returns>The HTTP status code</returns>
    public static int GetStatusCode(ErrorCategory category)
    {
        return StatusCodeMap[category];
    }

    /// <summary>
    /// Gets the HTTP status code for a given error code
    /// </summary>
    /// <param name="errorCode">The error code</param>
    /// <returns>The HTTP status code</returns>
    public static int GetStatusCode(string errorCode)
    {
        var category = GetCategory(errorCode);
        return GetStatusCode(category);
    }

    /// <summary>
    /// Gets the Problem Details type URI for a given error category
    /// </summary>
    /// <param name="category">The error category</param>
    /// <returns>The Problem Details type URI</returns>
    public static string GetProblemType(ErrorCategory category)
    {
        return ProblemTypeMap[category];
    }

    /// <summary>
    /// Gets the Problem Details type URI for a given error code
    /// </summary>
    /// <param name="errorCode">The error code</param>
    /// <returns>The Problem Details type URI</returns>
    public static string GetProblemType(string errorCode)
    {
        var category = GetCategory(errorCode);
        return GetProblemType(category);
    }

    /// <summary>
    /// Gets the Problem Details title for a given error category
    /// </summary>
    /// <param name="category">The error category</param>
    /// <returns>The Problem Details title</returns>
    public static string GetProblemTitle(ErrorCategory category)
    {
        return ProblemTitleMap[category];
    }

    /// <summary>
    /// Gets the Problem Details title for a given error code
    /// </summary>
    /// <param name="errorCode">The error code</param>
    /// <returns>The Problem Details title</returns>
    public static string GetProblemTitle(string errorCode)
    {
        var category = GetCategory(errorCode);
        return GetProblemTitle(category);
    }
}