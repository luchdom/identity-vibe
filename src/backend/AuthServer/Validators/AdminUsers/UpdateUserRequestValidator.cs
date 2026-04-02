using AuthServer.Models.AdminUsers;
using FluentValidation;

namespace AuthServer.Validators.AdminUsers;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        When(
            request => request.Email is not null,
            () =>
            {
                RuleFor(request => request.Email!)
                    .NotEmpty()
                    .WithErrorCode("REQUIRED_FIELD")
                    .EmailAddress()
                    .WithErrorCode("INVALID_EMAIL");
            });
    }
}
