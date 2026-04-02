using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class AddTrackingNumberRequestValidator : AbstractValidator<AddTrackingNumberRequest>
{
    public AddTrackingNumberRequestValidator()
    {
        RuleFor(request => request.TrackingNumber)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");
    }
}
