using AuthServer.Models.Commands;
using AuthServer.Models.Requests;

namespace AuthServer.Models.Mappers;

public static class RegisterMapper
{
    public static RegisterCommand ToDomain(this RegisterRequest request)
    {
        return new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword
        };
    }
}