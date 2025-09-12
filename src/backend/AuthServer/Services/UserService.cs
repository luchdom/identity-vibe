using AuthServer.Models.ViewModels;
using AuthServer.Repositories.Interfaces;
using AuthServer.Services.Interfaces;
using Shared.Common;

namespace AuthServer.Services;

public class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<AuthenticatedUserViewModel>> GetUserByIdAsync(string id)
    {
        try
        {
            logger.LogDebug("Getting user by ID: {UserId}", id);

            var userViewModel = await userRepository.GetByIdAsync(id);
            if (userViewModel == null)
            {
                logger.LogWarning("User not found for ID: {UserId}", id);
                return Result<AuthenticatedUserViewModel>.Failure(CommonErrors.UserNotFound);
            }

            var roles = await userRepository.GetRolesAsync(id);
            
            var authenticatedUser = new AuthenticatedUserViewModel
            {
                Id = userViewModel.Id,
                Email = userViewModel.Email,
                FirstName = userViewModel.FirstName,
                LastName = userViewModel.LastName,
                Roles = roles.ToArray(),
                IsActive = userViewModel.IsActive,
                CreatedAt = userViewModel.CreatedAt
            };

            return Result<AuthenticatedUserViewModel>.Success(authenticatedUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return Result<AuthenticatedUserViewModel>.Failure("USER_RETRIEVAL_ERROR", "An error occurred while retrieving the user");
        }
    }

    public async Task<Result<AuthenticatedUserViewModel>> GetUserByEmailAsync(string email)
    {
        try
        {
            logger.LogDebug("Getting user by email: {Email}", email);

            var userViewModel = await userRepository.GetByEmailAsync(email);
            if (userViewModel == null)
            {
                logger.LogWarning("User not found for email: {Email}", email);
                return Result<AuthenticatedUserViewModel>.Failure(CommonErrors.UserNotFound);
            }

            var roles = await userRepository.GetRolesAsync(userViewModel.Id);
            
            var authenticatedUser = new AuthenticatedUserViewModel
            {
                Id = userViewModel.Id,
                Email = userViewModel.Email,
                FirstName = userViewModel.FirstName,
                LastName = userViewModel.LastName,
                Roles = roles.ToArray(),
                IsActive = userViewModel.IsActive,
                CreatedAt = userViewModel.CreatedAt
            };

            return Result<AuthenticatedUserViewModel>.Success(authenticatedUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by email: {Email}", email);
            return Result<AuthenticatedUserViewModel>.Failure("USER_RETRIEVAL_ERROR", "An error occurred while retrieving the user");
        }
    }
}