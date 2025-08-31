using AuthServer.Entities;
using AuthServer.Entities.Mappers;
using AuthServer.Models.ViewModels;
using AuthServer.Repositories.Interfaces;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Shared.Common;

namespace AuthServer.Services;

public class UserService(
    IUserRepository userRepository,
    UserManager<User> userManager,
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<AuthenticatedUser>> GetUserByIdAsync(string id)
    {
        try
        {
            logger.LogDebug("Getting user by ID: {UserId}", id);

            var user = await userRepository.GetByIdAsync(id);
            if (user == null)
            {
                logger.LogWarning("User not found for ID: {UserId}", id);
                return Result<AuthenticatedUser>.Failure(CommonErrors.UserNotFound);
            }

            var roles = await userManager.GetRolesAsync(user);
            var authenticatedUser = user.ToDomain(roles);

            return Result<AuthenticatedUser>.Success(authenticatedUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return Result<AuthenticatedUser>.Failure("USER_RETRIEVAL_ERROR", "An error occurred while retrieving the user");
        }
    }

    public async Task<Result<AuthenticatedUser>> GetUserByEmailAsync(string email)
    {
        try
        {
            logger.LogDebug("Getting user by email: {Email}", email);

            var user = await userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                logger.LogWarning("User not found for email: {Email}", email);
                return Result<AuthenticatedUser>.Failure(CommonErrors.UserNotFound);
            }

            var roles = await userManager.GetRolesAsync(user);
            var authenticatedUser = user.ToDomain(roles);

            return Result<AuthenticatedUser>.Success(authenticatedUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by email: {Email}", email);
            return Result<AuthenticatedUser>.Failure("USER_RETRIEVAL_ERROR", "An error occurred while retrieving the user");
        }
    }
}