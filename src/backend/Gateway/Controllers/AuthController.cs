using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gateway.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var authServerUrl = configuration["Authentication:Authority"];
        
        var tokenRequest = new
        {
            grant_type = "password",
            client_id = "gateway-bff",
            client_secret = "gateway-secret",
            username = request.Email,
            password = request.Password,
            scope = "openid profile email offline_access data.read data.write profile.read profile.write"
        };

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", tokenRequest.grant_type),
            new KeyValuePair<string, string>("client_id", tokenRequest.client_id),
            new KeyValuePair<string, string>("client_secret", tokenRequest.client_secret),
            new KeyValuePair<string, string>("username", tokenRequest.username),
            new KeyValuePair<string, string>("password", tokenRequest.password),
            new KeyValuePair<string, string>("scope", tokenRequest.scope)
        });

        try
        {
            var response = await _httpClient.PostAsync($"{authServerUrl}/connect/token", formContent);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Ok(content);
            }
            
            return BadRequest(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Authentication service unavailable", details = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var authServerUrl = configuration["Authentication:Authority"];
        
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "gateway-bff"),
            new KeyValuePair<string, string>("client_secret", "gateway-secret"),
            new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
        });

        try
        {
            var response = await _httpClient.PostAsync($"{authServerUrl}/connect/token", formContent);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Ok(content);
            }
            
            return BadRequest(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Authentication service unavailable", details = ex.Message });
        }
    }

    [HttpGet("user")]
    [Authorize]
    public IActionResult GetUser()
    {
        var user = new
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Name = User.FindFirst(ClaimTypes.Name)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        };

        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("profile")]
    [HttpGet("/account/profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User ID not found in token" });
        }

        var authServerUrl = configuration["Authentication:Authority"];
        
        try
        {
            var response = await _httpClient.GetAsync($"{authServerUrl}/account/profile?userId={userId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Ok(content);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Profile service unavailable", details = ex.Message });
        }
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);