using AuthServer.Models.AdminUsers;
using FluentValidation;

namespace AuthServer.Validators.AdminUsers;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .EmailAddress()
            .WithErrorCode("INVALID_EMAIL");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD");

        RuleFor(request => request.RoleNames)
            .NotNull()
            .WithErrorCode("REQUIRED_FIELD")
            .Must(roleNames => roleNames.Any(roleName => !string.IsNullOrWhiteSpace(roleName)))
            .WithMessage("At least one role is required.")
            .WithErrorCode("REQUIRED_FIELD");
    }
}
