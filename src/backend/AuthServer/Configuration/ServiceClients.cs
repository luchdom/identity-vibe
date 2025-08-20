namespace AuthServer.Configuration;

public record ServiceClients
{
    public Dictionary<string, ServiceClientConfig> Clients { get; init; } = new();
};