using FluentValidation.Results;
using FluentValidation;
using Orders.Models.Requests;

namespace Orders.Validators;

public sealed class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    private const decimal MinimumOrderAmount = 1.00m;
    private const int MaxItemsPerOrder = 100;

    public UpdateOrderRequestValidator()
    {
        When(request => request.Customer is not null, () =>
        {
            RuleFor(request => request.Customer!)
                .SetValidator(new CustomerInfoValidator());
        });

        RuleFor(request => request.Items)
            .Must(items => items is null || items.Count > 0)
            .WithErrorCode("NO_ITEMS")
            .WithMessage("Updated items cannot be empty.")
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
                if (request.Items is null)
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
                if (request.Items.Count > 0 && subtotal < MinimumOrderAmount)
                {
                    context.AddFailure(new ValidationFailure("Items", $"Order must be at least ${MinimumOrderAmount:F2}.")
                    {
                        ErrorCode = "MINIMUM_ORDER_AMOUNT"
                    });
                }
            });
    }
}
