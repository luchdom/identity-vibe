namespace AuthServer.Models.AdminRoles;

public sealed record CreateRoleRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}
