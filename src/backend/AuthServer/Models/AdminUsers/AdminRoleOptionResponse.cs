namespace AuthServer.Models.AdminUsers;

public sealed record AdminRoleOptionResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public required string[] AssignedScopes { get; init; }
}
