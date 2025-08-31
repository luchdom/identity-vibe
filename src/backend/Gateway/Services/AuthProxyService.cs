using Gateway.Models.Requests;
using Gateway.Models.Responses;
using Gateway.Services.Interfaces;
using Gateway.Connectors.Interfaces;
using System.Text.Json;

namespace Gateway.Services;

/// <summary>
/// Service layer for authentication operations
/// Uses Connectors for external service communication as per clean architecture
/// </summary>
public class AuthProxyService(
    IAuthServerConnector authServerConnector,
    ILogger<AuthProxyService> logger) : IAuthProxyService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            logger.LogInformation("Service: Processing login request for {Email}", request.Email);
            
            // Use Connector for external HTTP call
            var response = await authServerConnector.LoginAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                logger.LogInformation("Service: Login successful for {Email}", request.Email);
                return authResponse ?? new AuthResponse { Success = false, Message = "Invalid response" };
            }
            
            logger.LogWarning("Service: Login failed for {Email}", request.Email);
            return new AuthResponse 
            { 
                Success = false, 
                Message = "Authentication failed" 
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service: Error during authentication proxy for {Email}", request.Email);
            return new AuthResponse 
            { 
                Success = false, 
                Message = "Authentication service unavailable" 
            };
        }
    }

    public async Task<AuthResponse> LogoutAsync()
    {
        try
        {
            logger.LogInformation("Service: Processing logout request");
            
            // Use Connector for external HTTP call
            var response = await authServerConnector.LogoutAsync();
            
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Service: Logout successful");
                return new AuthResponse { Success = true, Message = "Logout successful" };
            }
            
            logger.LogWarning("Service: Logout failed");
            return new AuthResponse { Success = false, Message = "Logout failed" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service: Error during logout proxy");
            return new AuthResponse { Success = false, Message = "Logout service unavailable" };
        }
    }

    public async Task<AuthResponse> GetProfileAsync()
    {
        try
        {
            logger.LogInformation("Service: Processing profile request");
            
            // Use Connector for external HTTP call
            var response = await authServerConnector.GetProfileAsync();
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var profileResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                logger.LogInformation("Service: Profile retrieval successful");
                return profileResponse ?? new AuthResponse { Success = false, Message = "Invalid response" };
            }
            
            logger.LogWarning("Service: Profile retrieval failed");
            return new AuthResponse { Success = false, Message = "Profile retrieval failed" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service: Error during profile retrieval proxy");
            return new AuthResponse { Success = false, Message = "Profile service unavailable" };
        }
    }
}