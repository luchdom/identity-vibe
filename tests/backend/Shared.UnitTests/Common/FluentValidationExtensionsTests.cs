using Ardalis.Result;
using FluentAssertions;
using FluentValidation;
using Shared.Common;
using Xunit;

namespace Shared.UnitTests.Common;

public sealed class FluentValidationExtensionsTests
{
    private readonly CreateThingRequestValidator validator = new();

    [Fact]
    public async Task ValidateRequestAsync_returns_invalid_result_for_null_request()
    {
        var result = await validator.ValidateRequestAsync(request: null);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle(error => error.Identifier == "request");
    }

    [Fact]
    public async Task ValidateRequestAsync_maps_fluent_validation_errors_to_ardalis_invalid()
    {
        var result = await validator.ValidateRequestAsync(new CreateThingRequest("", "not-an-email"));

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(error => error.Identifier == "Name" && error.ErrorCode == "REQUIRED_FIELD");
        result.ValidationErrors.Should().Contain(error => error.Identifier == "Email" && error.ErrorCode == "INVALID_EMAIL");
    }

    private sealed record CreateThingRequest(string Name, string Email);

    private sealed class CreateThingRequestValidator : AbstractValidator<CreateThingRequest>
    {
        public CreateThingRequestValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithErrorCode("REQUIRED_FIELD");

            RuleFor(request => request.Email)
                .EmailAddress()
                .WithErrorCode("INVALID_EMAIL");
        }
    }
}
