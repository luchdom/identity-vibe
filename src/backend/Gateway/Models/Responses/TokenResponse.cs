using System.Text.Json.Serialization;

namespace Gateway.Models.Responses;

/// <summary>
/// OAuth2 token response model from OpenIddict /connect/token endpoint
/// </summary>
public record TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }
    
    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }
    
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }
    
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
    
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
    
    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }
    
    // Error response properties
    [JsonPropertyName("error")]
    public string? Error { get; init; }
    
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}