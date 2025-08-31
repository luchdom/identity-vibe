using AuthServer.Models.ViewModels;

namespace AuthServer.Models.ViewModels;

/// <summary>
/// Domain-specific authentication result
/// </summary>
public record AuthenticationData
{
    public required AuthenticatedUser User { get; init; }
    public required string AccessToken { get; init; }
    public required string[] GrantedScopes { get; init; }
}