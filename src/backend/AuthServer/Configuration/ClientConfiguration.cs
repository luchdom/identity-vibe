namespace AuthServer.Configuration;

public sealed record ClientConfiguration
{
    public required List<ClientSettings> Clients { get; init; }
}
