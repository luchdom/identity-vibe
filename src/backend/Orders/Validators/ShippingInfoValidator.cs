using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class ShippingInfoValidator : AbstractValidator<ShippingInfo>
{
    public ShippingInfoValidator()
    {
        RuleFor(request => request.Address)
            .MaximumLength(255)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.City)
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.State)
            .MaximumLength(50)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.PostalCode)
            .MaximumLength(20)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Country)
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.ShippingCost)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("VALIDATION_FAILED")
            .WithMessage("Shipping cost cannot be negative.");
    }
}
