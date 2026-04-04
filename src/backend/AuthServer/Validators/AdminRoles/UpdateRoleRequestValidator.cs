using AuthServer.Models.AdminRoles;
using FluentValidation;

namespace AuthServer.Validators.AdminRoles;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(request => request.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .WithErrorCode("MAX_LENGTH");
    }
}
