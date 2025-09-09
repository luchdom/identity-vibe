using AuthServer.Models.Commands;
using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Result<RegistrationData>> RegisterAsync(RegisterUserCommand command);
    Task<Result> LogoutAsync(string userId);
}