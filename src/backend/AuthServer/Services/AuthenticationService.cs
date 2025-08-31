using AuthServer.Entities;
using AuthServer.Entities.Mappers;
using AuthServer.Models.Commands;
using AuthServer.Models.ViewModels;
using AuthServer.Repositories.Interfaces;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Shared.Common;

namespace AuthServer.Services;

public class AuthenticationService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IUserRepository userRepository,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public async Task<Result<AuthenticationData>> AuthenticateAsync(AuthenticateUserCommand command)
    {
        try
        {
            logger.LogInformation("Authentication attempt for {Email}", command.Email);

            var user = await userRepository.GetByEmailAsync(command.Email);
            if (user == null)
            {
                logger.LogWarning("Authentication failed: User not found for {Email}", command.Email);
                return Result<AuthenticationData>.Failure(CommonErrors.UserNotFound);
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                logger.LogWarning("Authentication failed: Invalid credentials for {Email}", command.Email);
                return Result<AuthenticationData>.Failure(CommonErrors.InvalidCredentials);
            }

            // Get user roles
            var roles = await userManager.GetRolesAsync(user);
            var authenticatedUser = user.ToDomain(roles);

            // Generate token (simplified - should use proper JWT service)
            var token = GenerateToken(authenticatedUser);
            
            var authData = new AuthenticationData
            {
                User = authenticatedUser,
                AccessToken = token,
                GrantedScopes = ["openid", "profile", "email"]
            };

            logger.LogInformation("Authentication successful for {Email}", command.Email);
            return Result<AuthenticationData>.Success(authData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Authentication error for {Email}", command.Email);
            return Result<AuthenticationData>.Failure("AUTH_ERROR", "An error occurred during authentication");
        }
    }

    public async Task<Result<RegistrationData>> RegisterAsync(RegisterUserCommand command)
    {
        try
        {
            logger.LogInformation("Registration attempt for {Email}", command.Email);

            // Check if user already exists
            if (await userRepository.ExistsAsync(command.Email))
            {
                logger.LogWarning("Registration failed: User already exists for {Email}", command.Email);
                return Result<RegistrationData>.Failure(CommonErrors.UserAlreadyExists);
            }

            // Validate password confirmation
            if (command.Password != command.ConfirmPassword)
            {
                return Result<RegistrationData>.Failure("PASSWORD_MISMATCH", "Passwords do not match");
            }

            // Create new user entity
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = command.Email,
                UserName = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Create user with password
            var result = await userManager.CreateAsync(user, command.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Registration failed for {Email}: {Errors}", command.Email, errors);
                return Result<RegistrationData>.Failure("REGISTRATION_FAILED", errors);
            }

            // Add default role
            await userManager.AddToRoleAsync(user, "User");

            var authenticatedUser = user.ToDomain(["User"]);
            var registrationData = new RegistrationData
            {
                User = authenticatedUser
            };

            logger.LogInformation("Registration successful for {Email}", command.Email);
            return Result<RegistrationData>.Success(registrationData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registration error for {Email}", command.Email);
            return Result<RegistrationData>.Failure("REGISTRATION_ERROR", "An error occurred during registration");
        }
    }

    public async Task<Result> LogoutAsync(string userId)
    {
        try
        {
            logger.LogInformation("Logout attempt for user {UserId}", userId);
            
            await signInManager.SignOutAsync();
            
            logger.LogInformation("Logout successful for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logout error for user {UserId}", userId);
            return Result.Failure("LOGOUT_ERROR", "An error occurred during logout");
        }
    }

    private static string GenerateToken(AuthenticatedUser user)
    {
        // Simplified token generation - should use proper JWT service
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{DateTime.UtcNow:O}"));
    }
}