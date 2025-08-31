using System.Security.Claims;

namespace Shared.Extensions;

/// <summary>
/// Claims processing extensions for user identity and authorization
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Gets user ID from claims using multiple fallback strategies
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ??
               user.FindFirst("id")?.Value ??
               user.FindFirst("user_id")?.Value ??
               user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Checks if user has admin privileges via roles or scopes
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || 
               user.HasClaim("scope", "admin.manage") ||
               user.HasClaim("role", "admin");
    }

    /// <summary>
    /// Gets all user roles
    /// </summary>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Distinct();
    }

    /// <summary>
    /// Gets all user scopes
    /// </summary>
    public static IEnumerable<string> GetScopes(this ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == "scope")
            .Select(c => c.Value)
            .Distinct();
    }

    /// <summary>
    /// Checks if user has specific scope
    /// </summary>
    public static bool HasScope(this ClaimsPrincipal user, string scope)
    {
        return user.HasClaim("scope", scope);
    }

    /// <summary>
    /// Checks if user has any of the specified scopes
    /// </summary>
    public static bool HasAnyScope(this ClaimsPrincipal user, params string[] scopes)
    {
        return scopes.Any(scope => user.HasScope(scope));
    }

    /// <summary>
    /// Gets user email from claims
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ??
               user.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets user name from claims
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ??
               user.FindFirst("name")?.Value ??
               user.FindFirst("username")?.Value;
    }
}