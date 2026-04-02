using FluentValidation.Results;
using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    private const decimal MinimumOrderAmount = 1.00m;
    private const int MaxItemsPerOrder = 100;

    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.Customer)
            .NotNull()
            .WithErrorCode("REQUIRED_FIELD");

        RuleFor(request => request.Customer)
            .SetValidator(new CustomerInfoValidator());

        RuleFor(request => request.Items)
            .NotNull()
            .WithErrorCode("REQUIRED_FIELD")
            .Must(items => items is { Count: > 0 })
            .WithErrorCode("NO_ITEMS")
            .WithMessage("Order must contain at least one item.")
            .Must(items => items is null || items.Count <= MaxItemsPerOrder)
            .WithErrorCode("TOO_MANY_ITEMS")
            .WithMessage($"Order cannot contain more than {MaxItemsPerOrder} items.");

        RuleForEach(request => request.Items)
            .SetValidator(new OrderItemRequestValidator())
            .When(request => request.Items is not null);

        When(request => request.Shipping is not null, () =>
        {
            RuleFor(request => request.Shipping!)
                .SetValidator(new ShippingInfoValidator());
        });

        RuleFor(request => request.SpecialInstructions)
            .MaximumLength(1000)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request.IdempotencyKey)
            .MaximumLength(100)
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(request => request)
            .Custom((request, context) =>
            {
                if (request.Items is null || request.Items.Count == 0)
                {
                    return;
                }

                var duplicateSku = request.Items
                    .Where(item => !string.IsNullOrWhiteSpace(item.ProductSku))
                    .GroupBy(item => item.ProductSku, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(group => group.Count() > 1);

                if (duplicateSku is not null)
                {
                    context.AddFailure(new ValidationFailure("Items", $"Duplicate product SKU found: {duplicateSku.Key}.")
                    {
                        ErrorCode = "DUPLICATE_SKU"
                    });
                }

                var subtotal = request.Items.Sum(item => (item.UnitPrice * item.Quantity) - item.DiscountAmount);
                if (subtotal < MinimumOrderAmount)
                {
                    context.AddFailure(new ValidationFailure("Items", $"Order must be at least ${MinimumOrderAmount:F2}.")
                    {
                        ErrorCode = "MINIMUM_ORDER_AMOUNT"
                    });
                }
            });
    }
}
