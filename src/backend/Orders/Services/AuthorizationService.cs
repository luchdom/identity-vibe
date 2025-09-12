namespace Orders.Services;

public class AuthorizationService(IConfiguration config)
{
    public void RegisterPolicies(IServiceCollection services)
    {
        var authConfig = config.GetSection("Auth:AuthenticationProviders");
        foreach (var provider in authConfig.GetChildren())
        {
            var policies = provider.GetSection("AuthorizationPolicies");
            foreach (var policy in policies.GetChildren())
            {
                var scopes = policy.GetSection("Scopes").Get<string[]>();
                if (scopes != null && scopes.Length > 0)
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy(policy.Key, policyBuilder =>
                            policyBuilder.RequireClaim("scope", scopes));
                    });
                }
            }
        }
    }
} 