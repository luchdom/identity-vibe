namespace ServiceA.Configuration;

public record UserIdentity
{
    public string Authority { get; init; } = string.Empty;
    public AuthorizationPolicies AuthorizationPolicies { get; init; } = new();
};