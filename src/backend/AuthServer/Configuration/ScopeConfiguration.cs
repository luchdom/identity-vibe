namespace AuthServer.Configuration;

public record ScopeConfiguration
{
    public List<ScopeSettings> Scopes { get; init; } = new();
    public UserScopes UserScopes { get; init; } = new();
};