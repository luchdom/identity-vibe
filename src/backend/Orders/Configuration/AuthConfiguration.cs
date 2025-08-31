namespace Orders.Configuration;

public record AuthConfiguration
{
    public AuthenticationProviders AuthenticationProviders { get; init; } = new();
};