namespace AuthServer.Configuration;

public record UserScopes
{
    public string[] DefaultScopes { get; init; } = [];
    public Dictionary<string, string[]> RoleScopes { get; init; } = new();
};