using Gateway.Models.Requests;
using Gateway.Models.Responses;

namespace Gateway.Services.Interfaces;

public interface IAuthProxyService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> LogoutAsync();
    Task<AuthResponse> GetProfileAsync();
}