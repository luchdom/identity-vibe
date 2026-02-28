using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Configuration;

public sealed class ScopeConfigurationValidator : IValidateOptions<ScopeConfiguration>
{
    private static readonly HashSet<string> ProtocolScopes = new(StringComparer.Ordinal)
    {
        Scopes.OpenId,
        Scopes.Profile,
        Scopes.Email,
        Scopes.OfflineAccess,
        Scopes.Roles
    };

    public ValidateOptionsResult Validate(string? name, ScopeConfiguration options)
    {
        if (options.Scopes is null || options.Scopes.Count == 0)
        {
            return ValidateOptionsResult.Fail("ScopeConfiguration.Scopes must contain at least one scope.");
        }

        if (options.UserScopes is null)
        {
            return ValidateOptionsResult.Fail("ScopeConfiguration.UserScopes is required.");
        }

        if (options.UserScopes.DefaultScopes is null)
        {
            return ValidateOptionsResult.Fail("ScopeConfiguration.UserScopes.DefaultScopes is required.");
        }

        if (options.UserScopes.RoleScopes is null)
        {
            return ValidateOptionsResult.Fail("ScopeConfiguration.UserScopes.RoleScopes is required.");
        }

        var declaredScopes = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < options.Scopes.Count; i++)
        {
            var scope = options.Scopes[i];
            var prefix = $"ScopeConfiguration.Scopes[{i}]";

            if (string.IsNullOrWhiteSpace(scope.Name))
            {
                return ValidateOptionsResult.Fail($"{prefix}.Name is required.");
            }

            if (!declaredScopes.Add(scope.Name))
            {
                return ValidateOptionsResult.Fail($"Duplicate scope name '{scope.Name}' in scope configuration.");
            }

            if (string.IsNullOrWhiteSpace(scope.DisplayName))
            {
                return ValidateOptionsResult.Fail($"{prefix}.DisplayName is required.");
            }

            if (string.IsNullOrWhiteSpace(scope.Description))
            {
                return ValidateOptionsResult.Fail($"{prefix}.Description is required.");
            }

            if (scope.Resources is not null && scope.Resources.Any(string.IsNullOrWhiteSpace))
            {
                return ValidateOptionsResult.Fail($"{prefix}.Resources cannot contain empty values.");
            }
        }

        foreach (var scope in options.UserScopes.DefaultScopes)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                return ValidateOptionsResult.Fail("ScopeConfiguration.UserScopes.DefaultScopes cannot contain empty values.");
            }

            if (!declaredScopes.Contains(scope) && !ProtocolScopes.Contains(scope))
            {
                return ValidateOptionsResult.Fail($"Default scope '{scope}' is not declared in the scope catalog.");
            }
        }

        foreach (var (roleName, scopes) in options.UserScopes.RoleScopes)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return ValidateOptionsResult.Fail("ScopeConfiguration.UserScopes.RoleScopes contains an empty role name.");
            }

            if (scopes is null)
            {
                return ValidateOptionsResult.Fail($"Role scope mapping for '{roleName}' cannot be null.");
            }

            foreach (var scope in scopes)
            {
                if (string.IsNullOrWhiteSpace(scope))
                {
                    return ValidateOptionsResult.Fail($"Role scope mapping for '{roleName}' contains an empty scope.");
                }

                if (!declaredScopes.Contains(scope) && !ProtocolScopes.Contains(scope))
                {
                    return ValidateOptionsResult.Fail($"Role '{roleName}' references unknown scope '{scope}'.");
                }
            }
        }

        return ValidateOptionsResult.Success;
    }
}
