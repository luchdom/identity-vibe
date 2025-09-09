using Gateway.Models.Requests;

namespace Gateway.Connectors.Interfaces;

public interface IAuthServerConnector
{
    Task<HttpResponseMessage> LoginAsync(LoginRequest request);
    Task<HttpResponseMessage> RegisterAsync(object registerRequest);
    Task<HttpResponseMessage> LogoutAsync(string? accessToken = null);
    Task<HttpResponseMessage> GetProfileAsync();
    Task<HttpResponseMessage> RefreshTokenAsync(string refreshToken);
}