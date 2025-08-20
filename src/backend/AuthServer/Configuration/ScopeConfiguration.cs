namespace AuthServer.Configuration;

public record ScopeConfiguration
{
    public UserScopes UserScopes { get; init; } = new();
    public ServiceClients ServiceClients { get; init; } = new();
};