using System.Text.Json.Serialization;

namespace AuthServer.IntegrationTests.Contracts;

public sealed class TokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}

