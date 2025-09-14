using AuthServer.Models.Commands;
using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Result<RegistrationDataViewModel>> RegisterAsync(RegisterUserCommand command);
}
