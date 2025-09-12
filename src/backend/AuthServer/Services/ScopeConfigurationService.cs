using Microsoft.Extensions.Options;
using AuthServer.Configuration;
using Microsoft.AspNetCore.Identity;
using AuthServer.Data.Entities;

namespace AuthServer.Services;

public class ScopeConfigurationService(
    IOptions<ScopeConfiguration> scopeConfig,
    UserManager<AppUser> userManager)
{
    private readonly ScopeConfiguration _scopeConfig = scopeConfig.Value;

    public async Task<string[]> GetUserScopesAsync(AppUser user)
    {
        var scopes = new List<string>(_scopeConfig.UserScopes.DefaultScopes);

        // Get user roles and add role-specific scopes
        var userRoles = await userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            if (_scopeConfig.UserScopes.RoleScopes.ContainsKey(role))
            {
                scopes.AddRange(_scopeConfig.UserScopes.RoleScopes[role]);
            }
        }

        return scopes.Distinct().ToArray();
    }

    // Keep synchronous version for backward compatibility but make it safer
    public string[] GetUserScopes(AppUser user)
    {
        return GetUserScopesAsync(user).GetAwaiter().GetResult();
    }

    public string[] GetClientScopes(string clientId)
    {
        if (_scopeConfig.ServiceClients.Clients.TryGetValue(clientId, out var clientConfig))
        {
            return clientConfig.AllowedScopes;
        }

        return Array.Empty<string>();
    }

    public ServiceClientConfig? GetClientConfig(string clientId)
    {
        _scopeConfig.ServiceClients.Clients.TryGetValue(clientId, out var clientConfig);
        return clientConfig;
    }

    public IEnumerable<ServiceClientConfig> GetAllClients()
    {
        return _scopeConfig.ServiceClients.Clients.Values;
    }

    public bool IsValidClient(string clientId, string clientSecret)
    {
        if (_scopeConfig.ServiceClients.Clients.TryGetValue(clientId, out var clientConfig))
        {
            return clientConfig.ClientSecret == clientSecret;
        }

        return false;
    }

    public bool IsValidGrantType(string clientId, string grantType)
    {
        if (_scopeConfig.ServiceClients.Clients.TryGetValue(clientId, out var clientConfig))
        {
            return clientConfig.GrantTypes.Contains(grantType);
        }

        return false;
    }

    public bool IsValidScope(string clientId, string scope)
    {
        if (_scopeConfig.ServiceClients.Clients.TryGetValue(clientId, out var clientConfig))
        {
            return clientConfig.AllowedScopes.Contains(scope);
        }

        return false;
    }

    public string[] GetValidScopesForClient(string clientId, string[] requestedScopes)
    {
        if (_scopeConfig.ServiceClients.Clients.TryGetValue(clientId, out var clientConfig))
        {
            return requestedScopes.Where(scope => clientConfig.AllowedScopes.Contains(scope)).ToArray();
        }

        return Array.Empty<string>();
    }
} 