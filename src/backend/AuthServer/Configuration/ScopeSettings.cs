namespace AuthServer.Configuration;

public sealed record ScopeSettings
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public List<string> Resources { get; init; } = [];
}
