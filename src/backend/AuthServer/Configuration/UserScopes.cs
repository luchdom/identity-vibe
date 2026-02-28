namespace AuthServer.Configuration;

public sealed record UserScopes
{
    public required string[] DefaultScopes { get; init; }
    public required Dictionary<string, string[]> RoleScopes { get; init; }
};
