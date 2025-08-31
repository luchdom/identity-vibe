using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthServer.Models.Requests;
using AuthServer.Models.Mappers;
using AuthServer.Services.Interfaces;
using Shared.Logging.Services;
using Shared.Common;
using Shared.Extensions;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(
    IAuthenticationService authenticationService,
    IUserService userService,
    ICorrelationIdService correlationIdService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = request.ToDomain();
        var result = await authenticationService.RegisterAsync(command);
        
        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.Value.ToPresentation();
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = request.ToDomain();
        var result = await authenticationService.AuthenticateAsync(command);
        
        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        var response = result.Value.ToPresentation();
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var result = await authenticationService.LogoutAsync(userId);
        
        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        return Ok(new { success = true, message = "Logout successful" });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = HttpContext.GetUserId();
        var userIdResult = ResultExtensions.RequireUserId<object>(userId);
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResultWithProblemDetails(HttpContext);

        var result = await userService.GetUserByIdAsync(userId);
        
        if (result.IsFailure)
        {
            return result.ToActionResultWithProblemDetails(HttpContext);
        }

        return Ok(new {
            success = true,
            data = result.Value
        });
    }
}