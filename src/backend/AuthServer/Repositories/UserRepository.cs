using AuthServer.Data.Entities;
using AuthServer.Data.Entities.Mappers;
using AuthServer.Models.ViewModels;
using AuthServer.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Repositories;

public class UserRepository(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager) : IUserRepository
{
    public async Task<UserViewModel?> GetByIdAsync(string id)
    {
        var entity = await userManager.FindByIdAsync(id);
        return entity?.ToDomain();
    }

    public async Task<UserViewModel?> GetByEmailAsync(string email)
    {
        var entity = await userManager.FindByEmailAsync(email);
        return entity?.ToDomain();
    }

    public async Task<UserViewModel> CreateAsync(UserViewModel userViewModel, string password)
    {
        var entity = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = userViewModel.Email,
            UserName = userViewModel.UserName,
            FirstName = userViewModel.FirstName,
            LastName = userViewModel.LastName,
            EmailConfirmed = userViewModel.EmailConfirmed,
            PhoneNumber = userViewModel.PhoneNumber,
            PhoneNumberConfirmed = userViewModel.PhoneNumberConfirmed,
            TwoFactorEnabled = userViewModel.TwoFactorEnabled,
            CreatedAt = DateTime.UtcNow,
            IsActive = userViewModel.IsActive,
            LockoutEnabled = userViewModel.LockoutEnabled
        };

        var result = await userManager.CreateAsync(entity, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return entity.ToDomain();
    }

    public async Task<UserViewModel> UpdateAsync(UserViewModel userViewModel)
    {
        var entity = await userManager.FindByIdAsync(userViewModel.Id);
        if (entity == null)
        {
            throw new InvalidOperationException($"User with ID {userViewModel.Id} not found");
        }

        entity.UpdateEntity(userViewModel);
        
        var result = await userManager.UpdateAsync(entity);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        return entity.ToDomain();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var entity = await userManager.FindByIdAsync(id);
        if (entity == null) return false;
        
        var result = await userManager.DeleteAsync(entity);
        return result.Succeeded;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var entity = await userManager.FindByEmailAsync(email);
        return entity != null;
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null) return false;

        return await userManager.CheckPasswordAsync(entity, password);
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null) return false;

        var result = await userManager.ChangePasswordAsync(entity, currentPassword, newPassword);
        return result.Succeeded;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string userId)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        return await userManager.GeneratePasswordResetTokenAsync(entity);
    }

    public async Task<bool> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null) return false;

        var result = await userManager.ResetPasswordAsync(entity, token, newPassword);
        return result.Succeeded;
    }

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null) return new List<string>();

        return await userManager.GetRolesAsync(entity);
    }

    public async Task<bool> AddToRoleAsync(string userId, string roleName)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null) return false;

        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists) return false;

        var result = await userManager.AddToRoleAsync(entity, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveFromRoleAsync(string userId, string roleName)
    {
        var entity = await userManager.FindByIdAsync(userId);
        if (entity == null) return false;

        var result = await userManager.RemoveFromRoleAsync(entity, roleName);
        return result.Succeeded;
    }
}