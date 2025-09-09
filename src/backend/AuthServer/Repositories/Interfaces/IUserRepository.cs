using AuthServer.Models.ViewModels;

namespace AuthServer.Repositories.Interfaces;

public interface IUserRepository
{
    Task<UserViewModel?> GetByIdAsync(string id);
    Task<UserViewModel?> GetByEmailAsync(string email);
    Task<UserViewModel> CreateAsync(UserViewModel userViewModel, string password);
    Task<UserViewModel> UpdateAsync(UserViewModel userViewModel);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string email);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string userId);
    Task<bool> ResetPasswordAsync(string userId, string token, string newPassword);
    Task<IList<string>> GetRolesAsync(string userId);
    Task<bool> AddToRoleAsync(string userId, string roleName);
    Task<bool> RemoveFromRoleAsync(string userId, string roleName);
}