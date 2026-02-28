namespace AuthServer.Models.AdminRoles;

public sealed record AdminRoleResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int AssignedScopeCount { get; init; }
    public required IReadOnlyList<string> AssignedScopes { get; init; }
}
