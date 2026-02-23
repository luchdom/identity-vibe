namespace Shared.Authorization.Models;

/// <summary>
/// Root configuration for authorization policies
/// </summary>
public class AuthorizationConfig
{
    /// <summary>
    /// Dictionary of policy name to policy configuration
    /// </summary>
    public Dictionary<string, PolicyConfig> Policies { get; set; } = new();
}

/// <summary>
/// Configuration for a single authorization policy
/// </summary>
public class PolicyConfig
{
    /// <summary>
    /// List of scopes that satisfy this policy (OR logic)
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// List of roles that satisfy this policy (OR logic)
    /// </summary>
    public List<string> Roles { get; set; } = new();
}