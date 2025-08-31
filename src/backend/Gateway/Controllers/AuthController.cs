using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gateway.Models.Requests;
using Gateway.Services.Interfaces;
using Shared.Logging.Services;
using Shared.Common;
using Shared.Extensions;

namespace Gateway.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(
    IAuthProxyService authProxyService,
    ICorrelationIdService correlationIdService,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("Gateway: Proxying login request for {Email}", request.Email);
        var result = await authProxyService.LoginAsync(request);
        
        if (result.Success)
        {
            logger.LogInformation("Gateway: Login successful for {Email}", request.Email);
            return Ok(result);
        }
        
        logger.LogWarning("Gateway: Login failed for {Email}", request.Email);
        return ConvertToProblemsDetails(result, "AUTH_INVALID_CREDENTIALS");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        logger.LogInformation("Gateway: Proxying logout request");
        var result = await authProxyService.LogoutAsync();
        
        return result.Success ? Ok(result) : ConvertToProblemsDetails(result, "LOGOUT_FAILED");
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        logger.LogInformation("Gateway: Proxying profile request");
        var result = await authProxyService.GetProfileAsync();
        
        return result.Success ? Ok(result) : ConvertToProblemsDetails(result, "PROFILE_FETCH_FAILED");
    }

    /// <summary>
    /// Converts AuthResponse failure to Problem Details format
    /// </summary>
    /// <param name="response">The failed AuthResponse</param>
    /// <param name="errorCode">The error code to use</param>
    /// <returns>IActionResult with Problem Details</returns>
    private IActionResult ConvertToProblemsDetails(Gateway.Models.Responses.AuthResponse response, string errorCode)
    {
        var result = Result<object>.Failure(errorCode, response.Message);
        return result.ToActionResultWithProblemDetails(HttpContext);
    }
}