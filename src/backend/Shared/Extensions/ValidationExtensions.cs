using System.Text.RegularExpressions;
using Shared.Common;

namespace Shared.Extensions;

/// <summary>
/// Common validation extensions for business rules
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    public static (int Page, int PageSize) ValidatePagination(int page, int pageSize, int maxPageSize = 100)
    {
        return (
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, maxPageSize)
        );
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    /// <summary>
    /// Validates required string field
    /// </summary>
    public static Result<string> RequireNonEmpty(this string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result<string>.Failure("REQUIRED_FIELD", $"{fieldName} is required")
            : Result<string>.Success(value);
    }

    /// <summary>
    /// Validates string length
    /// </summary>
    public static Result<string> ValidateLength(this string value, int minLength, int maxLength, string fieldName)
    {
        if (value.Length < minLength)
            return Result<string>.Failure("VALIDATION_FAILED", $"{fieldName} must be at least {minLength} characters");
        
        if (value.Length > maxLength)
            return Result<string>.Failure("VALIDATION_FAILED", $"{fieldName} cannot exceed {maxLength} characters");
        
        return Result<string>.Success(value);
    }

    /// <summary>
    /// Validates decimal range
    /// </summary>
    public static Result<decimal> ValidateRange(this decimal value, decimal min, decimal max, string fieldName)
    {
        if (value < min)
            return Result<decimal>.Failure("VALIDATION_FAILED", $"{fieldName} must be at least {min}");
        
        if (value > max)
            return Result<decimal>.Failure("VALIDATION_FAILED", $"{fieldName} cannot exceed {max}");
        
        return Result<decimal>.Success(value);
    }

    /// <summary>
    /// Validates collection has items
    /// </summary>
    public static Result<T[]> RequireNonEmpty<T>(this IEnumerable<T> collection, string fieldName)
    {
        var array = collection.ToArray();
        return array.Length == 0
            ? Result<T[]>.Failure("VALIDATION_FAILED", $"{fieldName} cannot be empty")
            : Result<T[]>.Success(array);
    }
}