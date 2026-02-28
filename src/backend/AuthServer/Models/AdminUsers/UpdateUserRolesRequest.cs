namespace AuthServer.Models.AdminUsers;

public sealed record UpdateUserRolesRequest
{
    public required string[] RoleNames { get; init; }
}
