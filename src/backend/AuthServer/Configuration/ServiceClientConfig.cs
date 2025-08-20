namespace AuthServer.Configuration;

public record ServiceClientConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string DisplayName { get; init; }
    public string Description { get; init; } = string.Empty;
    public string[] AllowedScopes { get; init; } = [];
    public string[] GrantTypes { get; init; } = [];
    public string[] RedirectUris { get; init; } = [];
};