using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(request => request.CancellationReason)
            .MaximumLength(500)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Notes)
            .MaximumLength(1000)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.IdempotencyKey)
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");
    }
}
