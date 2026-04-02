using System.Text.RegularExpressions;
using AuthServer.Models.AdminRoles;
using FluentValidation;

namespace AuthServer.Validators.AdminRoles;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    private static readonly Regex RoleNameRegex = new("^[A-Za-z0-9._-]{2,64}$", RegexOptions.Compiled);

    public CreateRoleRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithErrorCode("REQUIRED_FIELD")
            .Must(name => RoleNameRegex.IsMatch(name.Trim()))
            .WithMessage("Role name must be 2-64 chars and use letters, numbers, dot, dash, or underscore.")
            .WithErrorCode("INVALID_ROLE_NAME");

        RuleFor(request => request.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .WithErrorCode("MAX_LENGTH");
    }
}
