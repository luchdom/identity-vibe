using AuthServer.Models.Commands;
using AuthServer.Models.Requests;
using AuthServer.Models.Results;
using Shared.Common;

namespace AuthServer.Models.Mappers;

public static class LoginMapper
{
    public static LoginCommand ToDomain(this LoginRequest request)
    {
        return new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            RememberMe = request.RememberMe
        };
    }

    public static LoginRequest ToPresentation(this Result<LoginResult> result, LoginRequest originalRequest)
    {
        return new LoginRequest
        {
            Email = originalRequest.Email,
            RememberMe = originalRequest.RememberMe,
            ReturnUrl = originalRequest.ReturnUrl
        };
    }
}