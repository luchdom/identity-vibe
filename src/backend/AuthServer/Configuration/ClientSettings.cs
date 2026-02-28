namespace AuthServer.Configuration;

public sealed record ClientSettings
{
    public required string ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public required string DisplayName { get; init; }
    public required string Type { get; init; }
    public List<string> RedirectUris { get; init; } = [];
    public List<string> PostLogoutRedirectUris { get; init; } = [];
    public required List<string> AllowedGrantTypes { get; init; }
    public required List<string> AllowedScopes { get; init; }
    public bool RequireConsent { get; init; }
    public bool RequirePkce { get; init; } = true;
}
