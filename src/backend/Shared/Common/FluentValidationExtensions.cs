using FluentValidation;
using FluentValidation.Results;

namespace Shared.Common;

public static class FluentValidationExtensions
{
    public static async Task<Result<T>> ValidateRequestAsync<T>(
        this IValidator<T> validator,
        T? request,
        CancellationToken cancellationToken = default,
        string requestName = "request")
        where T : class
    {
        if (request is null)
        {
            return ResultExtensions.ValidationFailure<T>((requestName, $"{requestName} is required."));
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        return validationResult.IsValid
            ? Result<T>.Success(request)
            : validationResult.ToInvalidResult<T>();
    }

    public static Result<T> ToInvalidResult<T>(this ValidationResult validationResult)
    {
        return Result<T>.Invalid(ToValidationErrors(validationResult.Errors));
    }

    private static IEnumerable<ValidationError> ToValidationErrors(IEnumerable<ValidationFailure> failures)
    {
        return failures.Select(failure => new ValidationError
        {
            Identifier = failure.PropertyName,
            ErrorCode = string.IsNullOrWhiteSpace(failure.ErrorCode)
                ? "VALIDATION_FAILED"
                : failure.ErrorCode,
            ErrorMessage = failure.ErrorMessage
        });
    }
}
