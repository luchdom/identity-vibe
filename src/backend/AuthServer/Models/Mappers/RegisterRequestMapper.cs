using AuthServer.Models.Requests;
using AuthServer.Models.Commands;

namespace AuthServer.Models.Mappers;

public static class RegisterRequestMapper
{
    public static RegisterUserCommand ToDomain(this RegisterRequest request)
    {
        return new RegisterUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
    }
}