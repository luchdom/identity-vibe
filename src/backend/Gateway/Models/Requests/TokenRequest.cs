namespace Gateway.Models.Requests;

/// <summary>
/// OAuth2 token request model for OpenIddict /connect/token endpoint
/// </summary>
public record TokenRequest
{
    public required string GrantType { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Scope { get; init; }
    public string? RefreshToken { get; init; }
}