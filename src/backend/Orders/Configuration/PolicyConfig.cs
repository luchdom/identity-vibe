namespace Orders.Configuration;

public record PolicyConfig
{
    public string[] Scopes { get; init; } = [];
};