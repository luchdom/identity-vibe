using Gateway.Connectors.Interfaces;
using Gateway.Models.Requests;
using System.Text;
using System.Text.Json;

namespace Gateway.Connectors;

/// <summary>
/// Connector for making HTTP calls to AuthServer service
/// Handles all external service communication as per clean architecture
/// </summary>
public class AuthServerConnector(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AuthServerConnector> logger) : IAuthServerConnector
{
    private readonly string _authServerBaseUrl = configuration["Services:AuthServer:BaseUrl"] ?? "http://authserver:8080";

    public async Task<HttpResponseMessage> LoginAsync(LoginRequest request)
    {
        try
        {
            logger.LogInformation("Connector: Making HTTP call to AuthServer OAuth2 token endpoint");
            
            // Create OAuth2 token request for password grant
            var tokenRequest = new TokenRequest
            {
                GrantType = "password",
                ClientId = "gateway-bff",
                ClientSecret = "gateway-secret",
                Username = request.Email,
                Password = request.Password,
                Scope = "openid profile email orders.read orders.write profile.read profile.write"
            };
            
            // Create form-encoded content for OAuth2 token request
            var formParams = new List<KeyValuePair<string, string>>
            {
                new("grant_type", tokenRequest.GrantType),
                new("client_id", tokenRequest.ClientId),
                new("client_secret", tokenRequest.ClientSecret),
                new("username", tokenRequest.Username!),
                new("password", tokenRequest.Password!),
                new("scope", tokenRequest.Scope!)
            };
            
            var content = new FormUrlEncodedContent(formParams);
            
            var response = await httpClient.PostAsync($"{_authServerBaseUrl}/connect/token", content);
            
            logger.LogInformation("Connector: AuthServer OAuth2 token response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer OAuth2 token endpoint");
            throw;
        }
    }

    public async Task<HttpResponseMessage> RegisterAsync(object registerRequest)
    {
        try
        {
            logger.LogInformation("Connector: Making HTTP call to AuthServer register endpoint");
            
            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_authServerBaseUrl}/Account/register", content);
            
            logger.LogInformation("Connector: AuthServer register response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer register endpoint");
            throw;
        }
    }

    public async Task<HttpResponseMessage> LogoutAsync(string? accessToken = null)
    {
        try
        {
            logger.LogInformation("Connector: Making HTTP call to AuthServer OpenIddict logout endpoint");
            
            // Create logout request with optional access token
            var formParams = new List<KeyValuePair<string, string>>();
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                formParams.Add(new("access_token", accessToken));
            }
            
            var content = new FormUrlEncodedContent(formParams);
            
            var response = await httpClient.PostAsync($"{_authServerBaseUrl}/connect/logout", content);
            
            logger.LogInformation("Connector: AuthServer OpenIddict logout response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer OpenIddict logout endpoint");
            throw;
        }
    }

    public async Task<HttpResponseMessage> GetProfileAsync()
    {
        try
        {
            logger.LogInformation("Connector: Making HTTP call to AuthServer profile endpoint");
            
            var response = await httpClient.GetAsync($"{_authServerBaseUrl}/Account/profile");
            
            logger.LogInformation("Connector: AuthServer profile response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer profile endpoint");
            throw;
        }
    }

    public async Task<HttpResponseMessage> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            logger.LogInformation("Connector: Making HTTP call to AuthServer refresh token endpoint");
            
            var request = new { RefreshToken = refreshToken };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_authServerBaseUrl}/Account/refresh", content);
            
            logger.LogInformation("Connector: AuthServer refresh token response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer refresh token endpoint");
            throw;
        }
    }
}