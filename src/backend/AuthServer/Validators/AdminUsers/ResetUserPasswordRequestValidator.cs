using AuthServer.Models.AdminUsers;
using FluentValidation;

namespace AuthServer.Validators.AdminUsers;

public sealed class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD");
    }
}
