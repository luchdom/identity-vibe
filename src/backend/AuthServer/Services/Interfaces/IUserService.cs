using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface IUserService
{
    Task<Result<AuthenticatedUser>> GetUserByIdAsync(string userId);
    Task<Result<AuthenticatedUser>> GetUserByEmailAsync(string email);
}