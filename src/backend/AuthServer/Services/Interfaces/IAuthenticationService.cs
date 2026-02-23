using AuthServer.Models.Commands;
using AuthServer.Models.Results;

namespace AuthServer.Services.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginCommand command);
    Task<RegisterResult> RegisterAsync(RegisterCommand command);
}
