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

    public static Result<string> RequireNonEmpty(this string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? CommonErrors.RequiredField(fieldName).ToResult<string>()
            : Result<string>.Success(value);
    }

    public static Result<string> ValidateLength(this string value, int minLength, int maxLength, string fieldName)
    {
        if (value.Length < minLength)
        {
            return CommonErrors.ValidationFailed($"{fieldName} must be at least {minLength} characters").ToResult<string>();
        }

        if (value.Length > maxLength)
        {
            return CommonErrors.ValidationFailed($"{fieldName} cannot exceed {maxLength} characters").ToResult<string>();
        }

        return Result<string>.Success(value);
    }

    public static Result<decimal> ValidateRange(this decimal value, decimal min, decimal max, string fieldName)
    {
        if (value < min)
        {
            return CommonErrors.ValidationFailed($"{fieldName} must be at least {min}").ToResult<decimal>();
        }

        if (value > max)
        {
            return CommonErrors.ValidationFailed($"{fieldName} cannot exceed {max}").ToResult<decimal>();
        }

        return Result<decimal>.Success(value);
    }

    public static Result<T[]> RequireNonEmpty<T>(this IEnumerable<T> collection, string fieldName)
    {
        var array = collection.ToArray();
        return array.Length == 0
            ? CommonErrors.ValidationFailed($"{fieldName} cannot be empty").ToResult<T[]>()
            : Result<T[]>.Success(array);
    }
}
