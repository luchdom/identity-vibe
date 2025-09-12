using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface IUserService
{
    Task<Result<AuthenticatedUserViewModel>> GetUserByIdAsync(string userId);
    Task<Result<AuthenticatedUserViewModel>> GetUserByEmailAsync(string email);
}