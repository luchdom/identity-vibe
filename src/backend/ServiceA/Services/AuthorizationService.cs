using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ServiceA.Configuration;
using System.Security.Claims;

namespace ServiceA.Services;

public class AuthorizationService(IOptions<AuthConfiguration> authConfig)
{
    private readonly AuthConfiguration _authConfig = authConfig.Value;

    public void RegisterPolicies(AuthorizationOptions options)
    {
        // Register UserIdentity policy
        if (_authConfig.AuthenticationProviders.UserIdentity.AuthorizationPolicies.UserIdentity.Scopes.Any())
        {
            options.AddPolicy("UserIdentity", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope");
                    if (scopeClaim == null) return false;

                    var scopes = scopeClaim.Value.Split(' ');
                    return _authConfig.AuthenticationProviders.UserIdentity.AuthorizationPolicies.UserIdentity.Scopes
                        .Any(requiredScope => scopes.Contains(requiredScope));
                });
            });
        }

        // Register ServiceIdentityRead policy
        if (_authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityRead.Scopes.Any())
        {
            options.AddPolicy("ServiceIdentityRead", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope");
                    if (scopeClaim == null) return false;

                    var scopes = scopeClaim.Value.Split(' ');
                    return _authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityRead.Scopes
                        .Any(requiredScope => scopes.Contains(requiredScope));
                });
            });
        }

        // Register ServiceIdentityCreate policy
        if (_authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityCreate.Scopes.Any())
        {
            options.AddPolicy("ServiceIdentityCreate", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope");
                    if (scopeClaim == null) return false;

                    var scopes = scopeClaim.Value.Split(' ');
                    return _authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityCreate.Scopes
                        .Any(requiredScope => scopes.Contains(requiredScope));
                });
            });
        }

        // Register ServiceIdentityUpdate policy
        if (_authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityUpdate.Scopes.Any())
        {
            options.AddPolicy("ServiceIdentityUpdate", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope");
                    if (scopeClaim == null) return false;

                    var scopes = scopeClaim.Value.Split(' ');
                    return _authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityUpdate.Scopes
                        .Any(requiredScope => scopes.Contains(requiredScope));
                });
            });
        }

        // Register ServiceIdentityDelete policy
        if (_authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityDelete.Scopes.Any())
        {
            options.AddPolicy("ServiceIdentityDelete", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope");
                    if (scopeClaim == null) return false;

                    var scopes = scopeClaim.Value.Split(' ');
                    return _authConfig.AuthenticationProviders.ServiceIdentity.AuthorizationPolicies.ServiceIdentityDelete.Scopes
                        .Any(requiredScope => scopes.Contains(requiredScope));
                });
            });
        }
    }
} 