using AuthServer.Data.Entities;
using AuthServer.Models.Commands;
using AuthServer.Models.Results;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Shared.Common;

namespace AuthServer.Services;

public class AuthenticationService(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{

    public async Task<LoginResult> LoginAsync(LoginCommand command)
    {
        logger.LogInformation("Login attempt for user: {Email}", command.Email);

        // Find user by email
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            logger.LogWarning("Login failed - user not found: {Email}", command.Email);
            return new LoginResult { Success = false, ErrorMessage = "Invalid login attempt." };
        }

        // Check if user is active
        if (!user.IsActive)
        {
            logger.LogWarning("Login failed - user inactive: {Email}", command.Email);
            return new LoginResult { Success = false, ErrorMessage = "Your account has been disabled." };
        }

        // Attempt to sign in
        var result = await signInManager.PasswordSignInAsync(
            user.UserName ?? command.Email,
            command.Password,
            command.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User logged in successfully: {Email}", command.Email);
            return new LoginResult
            {
                Success = true,
                User = UserResult.FromEntity(user)
            };
        }

        if (result.RequiresTwoFactor)
        {
            logger.LogInformation("Two-factor authentication required for user: {Email}", command.Email);
            return new LoginResult { Success = false, ErrorMessage = "Two-factor authentication is not yet supported." };
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out: {Email}", command.Email);
            return new LoginResult { Success = false, ErrorMessage = "Your account has been locked out. Please try again later." };
        }

        logger.LogWarning("Login failed for user: {Email}", command.Email);
        return new LoginResult { Success = false, ErrorMessage = "Invalid login attempt." };
    }

    public async Task<RegisterResult> RegisterAsync(RegisterCommand command)
    {
        logger.LogInformation("Registration attempt for user: {Email}", command.Email);

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
        {
            logger.LogWarning("Registration failed - user already exists: {Email}", command.Email);
            return new RegisterResult { Success = false, ErrorMessage = "A user with this email already exists." };
        }

        var user = new AppUser
        {
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = true, // For demo purposes, skip email confirmation
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, command.Password);

        if (result.Succeeded)
        {
            logger.LogInformation("User registered successfully: {Email}", command.Email);

            // Add default User role
            await userManager.AddToRoleAsync(user, "User");

            // Automatically sign in the user after registration
            await signInManager.SignInAsync(user, isPersistent: false);

            return new RegisterResult
            {
                Success = true,
                User = UserResult.FromEntity(user)
            };
        }

        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
        logger.LogWarning("Registration failed for user: {Email}. Errors: {Errors}", command.Email, errorMessage);

        return new RegisterResult { Success = false, ErrorMessage = errorMessage };
    }

}
