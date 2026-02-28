namespace AuthServer.Models.AdminRoles;

public sealed record AvailableScopeResponse
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Group { get; init; }
    public required IReadOnlyList<string> Resources { get; init; }
}
