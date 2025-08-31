namespace Orders.Configuration;

public record AuthorizationPolicies
{
    public PolicyConfig UserIdentity { get; init; } = new();
    public PolicyConfig ServiceIdentityRead { get; init; } = new();
    public PolicyConfig ServiceIdentityCreate { get; init; } = new();
    public PolicyConfig ServiceIdentityUpdate { get; init; } = new();
    public PolicyConfig ServiceIdentityDelete { get; init; } = new();
};