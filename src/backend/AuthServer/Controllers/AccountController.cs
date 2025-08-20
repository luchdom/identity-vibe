using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthServer.Data;
using AuthServer.Models;
using System.Security.Claims;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ILogger<AccountController> logger) : ControllerBase
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly SignInManager<AppUser> _signInManager = signInManager;
    private readonly ILogger<AccountController> _logger = logger;

    [HttpPost("register")]
    public async Task<ActionResult<AccountResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new AccountResponse
            {
                Success = false,
                Message = "Registration failed",
                Errors = errors
            });
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true // For demo purposes, skip email confirmation
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password for {Email}", request.Email);

            return Ok(new AccountResponse
            {
                Success = true,
                Message = "User registered successfully"
            });
        }

        var registrationErrors = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(new AccountResponse
        {
            Success = false,
            Message = "Registration failed",
            Errors = registrationErrors
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccountResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new AccountResponse
            {
                Success = false,
                Message = "Login failed",
                Errors = errors
            });
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email, 
            request.Password, 
            request.RememberMe, 
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in: {Email}", request.Email);

            return Ok(new AccountResponse
            {
                Success = true,
                Message = "Login successful"
            });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", request.Email);
            return BadRequest(new AccountResponse
            {
                Success = false,
                Message = "Account is locked out"
            });
        }

        return BadRequest(new AccountResponse
        {
            Success = false,
            Message = "Invalid login attempt"
        });
    }

    [HttpPost("logout")]
    public async Task<ActionResult<AccountResponse>> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");

        return Ok(new AccountResponse
        {
            Success = true,
            Message = "Logout successful"
        });
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<AccountResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new AccountResponse
            {
                Success = false,
                Message = "Invalid request",
                Errors = errors
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return Ok(new AccountResponse
            {
                Success = true,
                Message = "If the email exists, a password reset link has been sent"
            });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // In a real application, you would send this token via email
        // For demo purposes, we'll return it in the response
        _logger.LogInformation("Password reset token generated for {Email}: {Token}", request.Email, token);

        return Ok(new AccountResponse
        {
            Success = true,
            Message = "Password reset link has been sent to your email",
            // In production, remove this line and send the token via email
            Errors = new List<string> { $"Reset Token: {token}" }
        });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<AccountResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new AccountResponse
            {
                Success = false,
                Message = "Invalid request",
                Errors = errors
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new AccountResponse
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successful for {Email}", request.Email);

            return Ok(new AccountResponse
            {
                Success = true,
                Message = "Password has been reset successfully"
            });
        }

        var resetErrors = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(new AccountResponse
        {
            Success = false,
            Message = "Password reset failed",
            Errors = resetErrors
        });
    }

    [HttpGet("profile")]
    public async Task<ActionResult<object>> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.CreatedAt,
            user.IsActive
        });
    }
} 