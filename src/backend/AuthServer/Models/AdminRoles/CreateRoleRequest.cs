namespace AuthServer.Models.AdminRoles;

public sealed record CreateRoleRequest(string Name, string? Description, bool IsActive);
