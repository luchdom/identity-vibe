namespace Shared.Common;

/// <summary>
/// Common error types for the Identity System
/// </summary>
public static class CommonErrors
{
    // Authentication Errors
    public static Error InvalidCredentials => new("AUTH_INVALID_CREDENTIALS", "Invalid email or password");
    public static Error UserNotFound => new("AUTH_USER_NOT_FOUND", "User not found");
    public static Error UserAlreadyExists => new("AUTH_USER_EXISTS", "User already exists");
    public static Error TokenInvalid => new("AUTH_TOKEN_INVALID", "Invalid or expired token");
    public static Error Unauthorized => new("AUTH_UNAUTHORIZED", "Unauthorized access");

    // Validation Errors  
    public static Error ValidationFailed(string details) => new("VALIDATION_FAILED", details);
    public static Error RequiredField(string fieldName) => new("REQUIRED_FIELD", $"{fieldName} is required");

    // Business Errors
    public static Error NotFound(string resource) => new("NOT_FOUND", $"{resource} not found");
    public static Error Conflict(string details) => new("CONFLICT", details);
    public static Error Forbidden => new("FORBIDDEN", "Insufficient permissions");

    // System Errors
    public static Error DatabaseError => new("DATABASE_ERROR", "Database operation failed");
    public static Error ExternalServiceError(string service) => new("EXTERNAL_SERVICE_ERROR", $"{service} is unavailable");
    public static Error UnexpectedError => new("UNEXPECTED_ERROR", "An unexpected error occurred");
}