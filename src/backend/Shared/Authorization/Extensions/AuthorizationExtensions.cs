using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Authorization.Models;
using Shared.Extensions;

namespace Shared.Authorization.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies from configuration
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds authorization policies from configuration with OR logic between scopes and roles.
    /// Resource APIs using OAuth/OIDC should generally prefer scope-only policies and leave Roles empty.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration containing Authorization:Policies section</param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when Authorization:Policies configuration is missing</exception>
    public static IServiceCollection AddConfigurablePolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authorization").Get<AuthorizationConfig>();

        if (authConfig?.Policies == null)
        {
            throw new InvalidOperationException("Authorization:Policies configuration section not found in appsettings.json");
        }

        services.AddAuthorization(options =>
        {
            foreach (var (policyName, policyConfig) in authConfig.Policies)
            {
                options.AddPolicy(policyName, policy =>
                    policy.RequireAssertion(context =>
                    {
                        // Must be authenticated
                        if (!context.User.Identity?.IsAuthenticated == true)
                            return false;

                        // Check scopes (any match) - for service-to-service calls
                        var hasScope = policyConfig.Scopes.Count == 0 ||
                                      policyConfig.Scopes.Any(scope => context.User.HasScope(scope));

                        // Check roles (any match) - for user calls
                        var hasRole = policyConfig.Roles.Count == 0 ||
                                     policyConfig.Roles.Any(role => context.User.IsInRole(role));

                        // OR logic: Either scope OR role must be satisfied
                        // If both lists are empty, only authentication is required
                        if (policyConfig.Scopes.Count == 0 && policyConfig.Roles.Count == 0)
                            return true;

                        return hasScope || hasRole;
                    }));
            }
        });

        return services;
    }
}
