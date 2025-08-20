namespace ServiceA.Configuration;

public record AuthenticationProviders
{
    public UserIdentity UserIdentity { get; init; } = new();
    public ServiceIdentity ServiceIdentity { get; init; } = new();
};