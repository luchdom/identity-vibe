using Microsoft.Extensions.Options;

namespace Shared.Authorization.Models;

public sealed class AuthorizationConfigValidator : IValidateOptions<AuthorizationConfig>
{
    public ValidateOptionsResult Validate(string? name, AuthorizationConfig options)
    {
        if (options.Policies is null || options.Policies.Count == 0)
        {
            return ValidateOptionsResult.Fail("Authorization:Policies must contain at least one policy.");
        }

        foreach (var (policyName, policyConfig) in options.Policies)
        {
            if (string.IsNullOrWhiteSpace(policyName))
            {
                return ValidateOptionsResult.Fail("Authorization:Policies contains an empty policy name.");
            }

            if (policyConfig is null)
            {
                return ValidateOptionsResult.Fail($"Authorization policy '{policyName}' is null.");
            }

            if (policyConfig.Scopes is null)
            {
                return ValidateOptionsResult.Fail($"Authorization policy '{policyName}'.Scopes cannot be null.");
            }

            if (policyConfig.Roles is null)
            {
                return ValidateOptionsResult.Fail($"Authorization policy '{policyName}'.Roles cannot be null.");
            }

            if (policyConfig.Scopes.Any(string.IsNullOrWhiteSpace))
            {
                return ValidateOptionsResult.Fail($"Authorization policy '{policyName}'.Scopes cannot contain empty values.");
            }

            if (policyConfig.Roles.Any(string.IsNullOrWhiteSpace))
            {
                return ValidateOptionsResult.Fail($"Authorization policy '{policyName}'.Roles cannot contain empty values.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
