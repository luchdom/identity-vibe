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
            logger.LogInformation("Connector: Making HTTP call to AuthServer login endpoint");
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_authServerBaseUrl}/Account/login", content);
            
            logger.LogInformation("Connector: AuthServer login response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer login endpoint");
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

    public async Task<HttpResponseMessage> LogoutAsync()
    {
        try
        {
            logger.LogInformation("Connector: Making HTTP call to AuthServer logout endpoint");
            
            var response = await httpClient.PostAsync($"{_authServerBaseUrl}/Account/logout", null);
            
            logger.LogInformation("Connector: AuthServer logout response status: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connector: Error calling AuthServer logout endpoint");
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