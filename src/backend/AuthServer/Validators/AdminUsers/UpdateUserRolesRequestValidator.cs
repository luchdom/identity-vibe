using AuthServer.Models.AdminUsers;
using FluentValidation;

namespace AuthServer.Validators.AdminUsers;

public sealed class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(request => request.RoleNames)
            .NotNull()
            .WithErrorCode("REQUIRED_FIELD");

        RuleForEach(request => request.RoleNames)
            .Must(roleName => !string.IsNullOrWhiteSpace(roleName))
            .WithMessage("Role names cannot be empty.")
            .WithErrorCode("REQUIRED_FIELD");
    }
}
