namespace AuthServer.Models.AdminRoles;

public sealed record UpdateRoleScopesRequest(List<string> ScopeNames);
