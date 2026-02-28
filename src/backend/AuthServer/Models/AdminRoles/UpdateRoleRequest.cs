namespace AuthServer.Models.AdminRoles;

public sealed record UpdateRoleRequest
{
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
}
