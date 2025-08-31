using AuthServer.Models.Requests;
using AuthServer.Models.Commands;

namespace AuthServer.Models.Mappers;

public static class LoginRequestMapper
{
    public static AuthenticateUserCommand ToDomain(this LoginRequest request)
    {
        return new AuthenticateUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            RememberMe = request.RememberMe
        };
    }
}