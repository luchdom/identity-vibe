namespace AuthServer.Models.AdminUsers;

public sealed record UpdateUserRolesRequest(string[] RoleNames);
