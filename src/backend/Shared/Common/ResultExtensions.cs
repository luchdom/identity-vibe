using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using FluentValidation.Results;

namespace Shared.Common;

public static class ResultExtensions
{
    private const string ErrorSeparator = "|::|";

    public static Result ToResult(this Error error)
    {
        return ErrorMapping.GetCategory(error.Code) switch
        {
            ErrorCategory.Validation => Result.Invalid(CreateValidationError(error)),
            ErrorCategory.NotFound => Result.NotFound([Encode(error)]),
            ErrorCategory.Conflict => Result.Conflict([Encode(error)]),
            ErrorCategory.Unauthorized => Result.Unauthorized([Encode(error)]),
            ErrorCategory.Forbidden => Result.Forbidden([Encode(error)]),
            ErrorCategory.ServiceUnavailable => Result.Unavailable([Encode(error)]),
            _ => Result.Error(Encode(error))
        };
    }

    public static Result<T> ToResult<T>(this Error error)
    {
        return ErrorMapping.GetCategory(error.Code) switch
        {
            ErrorCategory.Validation => Result<T>.Invalid(CreateValidationError(error)),
            ErrorCategory.NotFound => Result<T>.NotFound([Encode(error)]),
            ErrorCategory.Conflict => Result<T>.Conflict([Encode(error)]),
            ErrorCategory.Unauthorized => Result<T>.Unauthorized([Encode(error)]),
            ErrorCategory.Forbidden => Result<T>.Forbidden([Encode(error)]),
            ErrorCategory.ServiceUnavailable => Result<T>.Unavailable([Encode(error)]),
            _ => Result<T>.Error(Encode(error))
        };
    }

    public static Result Failure(Error error) => error.ToResult();

    public static Result<T> Failure<T>(Error error) => error.ToResult<T>();

    public static Result Failure(string code, string message) => new Error(code, message).ToResult();

    public static Result<T> Failure<T>(string code, string message) => new Error(code, message).ToResult<T>();

    public static Result<T> ToFailureResult<T>(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful non-generic result to a failed generic result.");
        }

        return Decode(result.Status, result.Errors, result.ValidationErrors).ToResult<T>();
    }

    public static Error ToError<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot extract an error from a successful result.");
        }

        return Decode(result.Status, result.Errors, result.ValidationErrors);
    }

    public static Error ToError(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot extract an error from a successful result.");
        }

        return Decode(result.Status, result.Errors, result.ValidationErrors);
    }

    public static Result<T> ToTypedResult<T>(this Result result)
    {
        if (result.IsSuccess)
        {
            return Result<T>.Success(default!);
        }

        return result.Status == ResultStatus.Invalid
            ? Result<T>.Invalid(result.ValidationErrors)
            : Decode(result.Status, result.Errors, result.ValidationErrors).ToResult<T>();
    }

    public static Result<TOut> ToFailureResult<TIn, TOut>(this Result<TIn> result)
    {
        if (result.IsSuccess)
        {
            return Result<TOut>.Success(default!);
        }

        return result.Status == ResultStatus.Invalid
            ? Result<TOut>.Invalid(result.ValidationErrors)
            : Decode(result.Status, result.Errors, result.ValidationErrors).ToResult<TOut>();
    }

    public static IActionResult ToActionResultWithProblemDetails<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    public static IActionResult ToActionResultWithProblemDetails(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    public static IActionResult ToActionResultWithProblemDetails<T>(
        this Result<T> result,
        HttpContext context,
        Func<T, object> successMapper)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(successMapper(result.Value));
        }

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    public static IActionResult ToActionResultWithProblemDetails<T>(
        this Result<T> result,
        HttpContext context,
        Func<T, IActionResult> successMapper)
    {
        if (result.IsSuccess)
        {
            return successMapper(result.Value);
        }

        var problemDetails = result.ToProblemDetails(context);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    public static ProblemDetail ToProblemDetails<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to problem details.");
        }

        return CreateProblemDetails(
            Decode(result.Status, result.Errors, result.ValidationErrors),
            context,
            BuildValidationErrors(result.ValidationErrors));
    }

    public static ProblemDetail ToProblemDetails(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to problem details.");
        }

        return CreateProblemDetails(
            Decode(result.Status, result.Errors, result.ValidationErrors),
            context,
            BuildValidationErrors(result.ValidationErrors));
    }

    public static Result<T> ValidationFailure<T>(params (string Field, string Message)[] errors)
    {
        var validationErrors = errors.Select(error => new ValidationError
        {
            Identifier = error.Field,
            ErrorCode = "VALIDATION_FAILED",
            ErrorMessage = error.Message
        });

        return Result<T>.Invalid(validationErrors);
    }

    public static Result ValidationFailure(FluentValidation.Results.ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);
        return Result.Invalid(validationResult.Errors.Select(ToValidationError));
    }

    public static Result<T> ValidationFailure<T>(FluentValidation.Results.ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);
        return Result<T>.Invalid(validationResult.Errors.Select(ToValidationError));
    }

    public static Result<T> ValidationFailure<T>(ModelStateDictionary modelState)
    {
        var validationErrors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(
                entry => entry.Value!.Errors.Select(error => new ValidationError
                {
                    Identifier = entry.Key,
                    ErrorCode = "VALIDATION_FAILED",
                    ErrorMessage = error.ErrorMessage
                }));

        return Result<T>.Invalid(validationErrors);
    }

    public static Result<T> Unauthorized<T>(string message = "Access denied") =>
        Failure<T>("AUTH_UNAUTHORIZED", message);

    public static Result<T> Forbidden<T>(string message = "Insufficient permissions") =>
        Failure<T>("FORBIDDEN", message);

    public static Result<T> NotFound<T>(string resource = "Resource") =>
        Failure<T>("NOT_FOUND", $"{resource} not found");

    public static Result<T> RequireUserId<T>(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? Failure<T>("INVALID_REQUEST", "User ID not found in token")
            : Result<T>.Success(default!);
    }

    public static Result<T> RequireNonEmptyValue<T>(string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Failure<T>("REQUIRED_FIELD", $"{fieldName} is required")
            : Result<T>.Success(default!);
    }

    public static Result<T> RequirePositiveId<T>(int value, string fieldName = "id")
    {
        return value <= 0
            ? Failure<T>("INVALID_REQUEST", $"{fieldName} must be greater than zero")
            : Result<T>.Success(default!);
    }

    private static ValidationError CreateValidationError(Error error)
    {
        return new ValidationError
        {
            Identifier = error.Code,
            ErrorCode = error.Code,
            ErrorMessage = error.Message
        };
    }

    private static ValidationError ToValidationError(ValidationFailure failure)
    {
        return new ValidationError
        {
            Identifier = failure.PropertyName,
            ErrorCode = string.IsNullOrWhiteSpace(failure.ErrorCode) ? "VALIDATION_FAILED" : failure.ErrorCode,
            ErrorMessage = failure.ErrorMessage
        };
    }

    private static string Encode(Error error) => $"{error.Code}{ErrorSeparator}{error.Message}";

    private static Error Decode(
        ResultStatus status,
        IEnumerable<string> errors,
        IEnumerable<ValidationError> validationErrors)
    {
        if (status == ResultStatus.Invalid)
        {
            var validationError = validationErrors.FirstOrDefault();
            if (validationError != null)
            {
                var code = validationError.ErrorCode
                    ?? validationError.Identifier
                    ?? "VALIDATION_FAILED";
                return new Error(code, validationError.ErrorMessage);
            }
        }

        var firstError = errors.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstError))
        {
            return Decode(firstError);
        }

        return status switch
        {
            ResultStatus.NotFound => CommonErrors.NotFound("Resource"),
            ResultStatus.Conflict => CommonErrors.Conflict("Resource conflict"),
            ResultStatus.Unauthorized => CommonErrors.Unauthorized,
            ResultStatus.Forbidden => CommonErrors.Forbidden,
            ResultStatus.Unavailable => CommonErrors.ExternalServiceError("Dependency"),
            ResultStatus.Invalid => CommonErrors.ValidationFailed("Validation failed"),
            _ => CommonErrors.UnexpectedError
        };
    }

    private static Error Decode(string encoded)
    {
        var parts = encoded.Split(ErrorSeparator, 2, StringSplitOptions.None);
        return parts.Length == 2
            ? new Error(parts[0], parts[1])
            : new Error("GENERAL_ERROR", encoded);
    }

    private static Dictionary<string, string[]>? BuildValidationErrors(IEnumerable<ValidationError> validationErrors)
    {
        var errors = validationErrors
            .GroupBy(error => error.Identifier ?? error.ErrorCode ?? "VALIDATION_FAILED", StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        return errors.Count == 0 ? null : errors;
    }

    private static ProblemDetail CreateProblemDetails(
        Error error,
        HttpContext context,
        Dictionary<string, string[]>? validationErrors = null)
    {
        var category = ErrorMapping.GetCategory(error.Code);

        return new ProblemDetail
        {
            Type = ErrorMapping.GetProblemType(category),
            Title = ErrorMapping.GetProblemTitle(category),
            Status = ErrorMapping.GetStatusCode(category),
            Detail = error.Message,
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null,
            TraceId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
                ?? context.TraceIdentifier,
            Errors = validationErrors,
            Extensions = new Dictionary<string, object>
            {
                ["errorCode"] = error.Code
            }
        };
    }
}
