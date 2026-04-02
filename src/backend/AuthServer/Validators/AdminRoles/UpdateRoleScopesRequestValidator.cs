using AuthServer.Models.AdminRoles;
using FluentValidation;

namespace AuthServer.Validators.AdminRoles;

public sealed class UpdateRoleScopesRequestValidator : AbstractValidator<UpdateRoleScopesRequest>
{
    public UpdateRoleScopesRequestValidator()
    {
        RuleFor(request => request.ScopeNames)
            .NotNull()
            .WithErrorCode("REQUIRED_FIELD");

        RuleForEach(request => request.ScopeNames)
            .Must(scopeName => !string.IsNullOrWhiteSpace(scopeName))
            .WithMessage("Scope names cannot be empty.")
            .WithErrorCode("REQUIRED_FIELD");
    }
}
