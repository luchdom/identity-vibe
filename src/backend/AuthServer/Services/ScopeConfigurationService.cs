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
            if (_scopeConfig.UserScopes.RoleScopes.TryGetValue(role, out var scope))
            {
                scopes.AddRange(scope);
            }
        }

        return scopes.Distinct().ToArray();
    }

    // Keep synchronous version for backward compatibility but make it safer
    public string[] GetUserScopes(AppUser user)
    {
        return GetUserScopesAsync(user).GetAwaiter().GetResult();
    }

    public IEnumerable<ScopeSettings> GetAllScopes()
    {
        return _scopeConfig.Scopes;
    }

    public ScopeSettings? GetScopeSettings(string scopeName)
    {
        return _scopeConfig.Scopes.FirstOrDefault(s => s.Name == scopeName);
    }

    public bool IsValidScope(string scopeName)
    {
        return _scopeConfig.Scopes.Any(s => s.Name == scopeName);
    }

    public string[] GetScopesByResource(string resourceName)
    {
        return _scopeConfig.Scopes
            .Where(s => s.Resources.Contains(resourceName))
            .Select(s => s.Name)
            .ToArray();
    }
} 
