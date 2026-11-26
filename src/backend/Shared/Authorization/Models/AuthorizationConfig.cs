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
