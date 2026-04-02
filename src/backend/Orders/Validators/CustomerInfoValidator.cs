using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class CustomerInfoValidator : AbstractValidator<CustomerInfo>
{
    public CustomerInfoValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.LastName)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Email)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .EmailAddress()
            .WithErrorCode("INVALID_EMAIL")
            .MaximumLength(255)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Phone)
            .MaximumLength(20)
            .WithErrorCode("VALIDATION_FAILED");

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
    }
}
