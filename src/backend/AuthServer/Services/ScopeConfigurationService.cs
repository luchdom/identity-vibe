using Microsoft.Extensions.Options;
using AuthServer.Configuration;
using AuthServer.Data.Entities;
using AuthServer.Data;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Services;

public class ScopeConfigurationService(
    IOptions<ScopeConfiguration> scopeConfig,
    AppDbContext dbContext)
{
    private readonly ScopeConfiguration _scopeConfig = scopeConfig.Value;
    private static readonly HashSet<string> ProtocolScopes = new(StringComparer.Ordinal)
    {
        Scopes.OpenId,
        Scopes.Profile,
        Scopes.Email,
        Scopes.OfflineAccess,
        Scopes.Roles
    };

    public async Task<string[]> GetUserScopesAsync(AppUser user)
    {
        var scopes = new List<string>(_scopeConfig.UserScopes.DefaultScopes);

        // DB-backed runtime source of truth for role -> scope assignments.
        var roleScopeAssignments = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles.Where(r => r.IsActive),
                ur => ur.RoleId,
                r => r.Id,
                (ur, role) => role.Id)
            .Join(dbContext.RoleScopes,
                roleId => roleId,
                rs => rs.RoleId,
                (_, roleScope) => roleScope.ScopeName)
            .Distinct()
            .ToListAsync();

        scopes.AddRange(roleScopeAssignments);

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

    public IEnumerable<ScopeSettings> GetAssignableRoleScopes()
    {
        return _scopeConfig.Scopes.Where(scope => !IsProtocolScope(scope.Name));
    }

    public ScopeSettings? GetScopeSettings(string scopeName)
    {
        return _scopeConfig.Scopes.FirstOrDefault(s => s.Name == scopeName);
    }

    public bool IsValidScope(string scopeName)
    {
        return _scopeConfig.Scopes.Any(s => s.Name == scopeName);
    }

    public bool IsAssignableRoleScope(string scopeName)
    {
        return !IsProtocolScope(scopeName) && IsValidScope(scopeName);
    }

    public string[] GetScopesByResource(string resourceName)
    {
        return _scopeConfig.Scopes
            .Where(s => s.Resources.Contains(resourceName))
            .Select(s => s.Name)
            .ToArray();
    }

    public async Task<string[]> GetActiveRoleNamesAsync(AppUser user)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles.Where(r => r.IsActive),
                ur => ur.RoleId,
                role => role.Id,
                (_, role) => role.Name!)
            .Distinct()
            .ToArrayAsync();
    }

    private static bool IsProtocolScope(string scopeName) => ProtocolScopes.Contains(scopeName);
} 
