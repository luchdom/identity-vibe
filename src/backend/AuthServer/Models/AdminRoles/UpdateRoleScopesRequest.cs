namespace AuthServer.Models.AdminRoles;

public sealed record UpdateRoleScopesRequest
{
    public required List<string> ScopeNames { get; init; }
}
