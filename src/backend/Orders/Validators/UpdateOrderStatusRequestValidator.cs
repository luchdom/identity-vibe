using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Reason)
            .MaximumLength(500)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Notes)
            .MaximumLength(1000)
            .WithErrorCode("VALIDATION_FAILED");
    }
}
