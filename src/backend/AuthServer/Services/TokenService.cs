using AuthServer.Models.ViewModels;
using AuthServer.Services.Interfaces;
using Shared.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services;

public class TokenService(
    IConfiguration configuration,
    ILogger<TokenService> logger) : ITokenService
{
    public async Task<Result<string>> GenerateAccessTokenAsync(AuthenticatedUserViewModel user, string[] scopes)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["Jwt:Key"] ?? "your-secret-key-that-is-at-least-32-characters-long"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new("sub", user.Id),
                new("email", user.Email),
                new("given_name", user.FirstName),
                new("family_name", user.LastName)
            };

            // Add roles
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            // Add scopes
            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"] ?? "https://localhost:5000",
                audience: configuration["Jwt:Audience"] ?? "https://localhost:5000",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            
            logger.LogDebug("Token generated for user {UserId}", user.Id);
            return await Task.FromResult(Result<string>.Success(tokenString));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating token for user {UserId}", user.Id);
            return Result<string>.Failure("TOKEN_GENERATION_ERROR", "Failed to generate access token");
        }
    }

    public async Task<Result<bool>> ValidateTokenAsync(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["Jwt:Key"] ?? "your-secret-key-that-is-at-least-32-characters-long"));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "https://localhost:5000",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "https://localhost:5000",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            await tokenHandler.ValidateTokenAsync(token, validationParameters);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Token validation failed");
            return Result<bool>.Success(false);
        }
    }
}