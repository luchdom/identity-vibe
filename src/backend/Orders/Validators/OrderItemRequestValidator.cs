using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(request => request.ProductSku)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.ProductName)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(255)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.ProductDescription)
            .MaximumLength(1000)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.ProductImageUrl)
            .MaximumLength(500)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.ProductCategory)
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .WithErrorCode("VALIDATION_FAILED")
            .WithMessage("Quantity must be at least 1.");

        RuleFor(request => request.UnitPrice)
            .GreaterThan(0)
            .WithErrorCode("VALIDATION_FAILED")
            .WithMessage("Unit price must be greater than 0.");

        RuleFor(request => request.DiscountAmount)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("VALIDATION_FAILED")
            .WithMessage("Discount amount cannot be negative.");

        RuleFor(request => request.Size)
            .MaximumLength(50)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.Color)
            .MaximumLength(50)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.AdditionalAttributes)
            .MaximumLength(500)
            .WithErrorCode("VALIDATION_FAILED");
    }
}
