namespace AuthServer.Configuration;

public sealed record ScopeConfiguration
{
    public required List<ScopeSettings> Scopes { get; init; }
    public required UserScopes UserScopes { get; init; }
};
