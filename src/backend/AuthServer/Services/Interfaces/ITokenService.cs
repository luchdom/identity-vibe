using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface ITokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(AuthenticatedUser user, string[] scopes);
    Task<Result<bool>> ValidateTokenAsync(string token);
}