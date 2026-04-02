namespace AuthServer.Models.AdminRoles;

public sealed record UpdateRoleRequest(string? Description, bool IsActive);
