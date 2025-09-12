using AuthServer.Models.Commands;
using AuthServer.Models.ViewModels;
using AuthServer.Repositories.Interfaces;
using AuthServer.Services.Interfaces;
using Shared.Common;

namespace AuthServer.Services;

public class AuthenticationService(
    IUserRepository userRepository,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{

    public async Task<Result<RegistrationDataViewModel>> RegisterAsync(RegisterUserCommand command)
    {
        try
        {
            logger.LogInformation("Registration attempt for {Email}", command.Email);

            if (await userRepository.ExistsAsync(command.Email))
            {
                logger.LogWarning("Registration failed: User already exists for {Email}", command.Email);
                return Result<RegistrationDataViewModel>.Failure(CommonErrors.UserAlreadyExists);
            }

            if (command.Password != command.ConfirmPassword)
            {
                return Result<RegistrationDataViewModel>.Failure("PASSWORD_MISMATCH", "Passwords do not match");
            }

            var newUserViewModel = new UserViewModel
            {
                Id = Guid.NewGuid().ToString(),
                Email = command.Email,
                UserName = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                EmailConfirmed = true,
                PhoneNumber = null,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                IsActive = true,
                LockoutEnd = null,
                LockoutEnabled = true,
                AccessFailedCount = 0
            };

            var createdUser = await userRepository.CreateAsync(newUserViewModel, command.Password);
            
            await userRepository.AddToRoleAsync(createdUser.Id, "User");

            var authenticatedUser = new AuthenticatedUserViewModel
            {
                Id = createdUser.Id,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                Roles = ["User"],
                IsActive = createdUser.IsActive,
                CreatedAt = createdUser.CreatedAt
            };
            
            var registrationData = new RegistrationDataViewModel
            {
                User = authenticatedUser
            };

            logger.LogInformation("Registration successful for {Email}", command.Email);
            return Result<RegistrationDataViewModel>.Success(registrationData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registration error for {Email}", command.Email);
            return Result<RegistrationDataViewModel>.Failure("REGISTRATION_ERROR", "An error occurred during registration");
        }
    }

    public Task<Result> LogoutAsync(string userId)
    {
        try
        {
            logger.LogInformation("Logout attempt for user {UserId}", userId);
            
            logger.LogInformation("Logout successful for user {UserId}", userId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logout error for user {UserId}", userId);
            return Task.FromResult(Result.Failure("LOGOUT_ERROR", "An error occurred during logout"));
        }
    }

}