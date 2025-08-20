namespace ServiceA.Configuration;

public record ServiceIdentity
{
    public string Authority { get; init; } = string.Empty;
    public AuthorizationPolicies AuthorizationPolicies { get; init; } = new();
};