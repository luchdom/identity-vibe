using FluentAssertions;
using Orders.Models.Requests;
using Orders.Validators;
using Xunit;

namespace Orders.UnitTests.Validators;

public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator validator = new();

    [Fact]
    public void Validate_rejects_duplicate_skus()
    {
        var request = CreateRequest(
            new OrderItemRequest("SKU-1", "First", null, null, null, 1, 10m, 0, null, null, null),
            new OrderItemRequest("SKU-1", "Second", null, null, null, 1, 15m, 0, null, null, null));

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "DUPLICATE_SKU");
    }

    [Fact]
    public void Validate_rejects_orders_below_minimum_amount()
    {
        var request = CreateRequest(
            new OrderItemRequest("SKU-1", "First", null, null, null, 1, 0.50m, 0, null, null, null));

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "MINIMUM_ORDER_AMOUNT");
    }

    private static CreateOrderRequest CreateRequest(params OrderItemRequest[] items)
    {
        return new CreateOrderRequest(
            new CustomerInfo("Test", "Buyer", "buyer@example.test", null, null, null, null, null, null),
            items.ToList(),
            null,
            null,
            null);
    }
}
