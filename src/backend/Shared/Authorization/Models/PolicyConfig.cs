namespace Shared.Authorization.Models;

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
