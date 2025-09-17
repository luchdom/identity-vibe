using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AuthServer.Models.Requests;
using AuthServer.Models.Mappers;
using AuthServer.Services.Interfaces;
using Shared.Logging.Services;
using Shared.Common;
using Shared.Extensions;

namespace AuthServer.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class AccountController(
    IAuthenticationService authenticationService,
    IUserService userService) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "authserver", timestamp = DateTime.UtcNow });
    }
    [HttpPost("register")]
    [AllowAnonymous]
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

    [HttpGet("profile")]
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

        var response = result.Value.ToPresentation();
        return Ok(response);
    }
}
