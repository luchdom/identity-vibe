namespace Shared.Authorization.Models;

/// <summary>
/// Root configuration for authorization policies
/// </summary>
public sealed record AuthorizationConfig
{
    /// <summary>
    /// Dictionary of policy name to policy configuration
    /// </summary>
    public required Dictionary<string, PolicyConfig> Policies { get; init; }
}

/// <summary>
/// Configuration for a single authorization policy
/// </summary>
public sealed record PolicyConfig
{
    /// <summary>
    /// List of scopes that satisfy this policy (OR logic)
    /// </summary>
    public List<string> Scopes { get; init; } = [];

    /// <summary>
    /// List of roles that satisfy this policy (OR logic)
    /// </summary>
    public List<string> Roles { get; init; } = [];
}
