using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Services;

namespace AuthServer.Controllers;

public class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager,
    IOpenIddictTokenManager tokenManager,
    UserManager<AppUser> userManager,
    ScopeConfigurationService scopeConfigService) : Controller
{
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            // Validate the username/password parameters using Identity
            if (string.IsNullOrEmpty(request.Username))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Username is required."
                    }));
            }

            var user = await userManager.FindByEmailAsync(request.Username);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
                    }));
            }

            // Verify the password
            if (string.IsNullOrEmpty(request.Password))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Password is required."
                    }));
            }

            var isValidPassword = await userManager.CheckPasswordAsync(user, request.Password);
            if (!isValidPassword)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
                    }));
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Account is disabled."
                    }));
            }

            // Create a new ClaimsIdentity containing the claims that will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            // Add the claims associated with the user to the identity
            identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id);
            
            // Add profile claims and set destinations
            var nameClaim = new Claim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}");
            nameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
            identity.AddClaim(nameClaim);

            if (!string.IsNullOrEmpty(user.Email))
            {
                var emailClaim = new Claim(OpenIddictConstants.Claims.Email, user.Email);
                emailClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                identity.AddClaim(emailClaim);
            }

            var givenNameClaim = new Claim(OpenIddictConstants.Claims.GivenName, user.FirstName);
            givenNameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
            identity.AddClaim(givenNameClaim);
            
            var familyNameClaim = new Claim(OpenIddictConstants.Claims.FamilyName, user.LastName);
            familyNameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
            identity.AddClaim(familyNameClaim);

            // Get user scopes based on configuration
            var userScopes = scopeConfigService.GetUserScopes(user);

            // Filter requested scopes against user's allowed scopes
            var requestedScopes = request.GetScopes();
            var validScopes = requestedScopes.Where(scope => userScopes.Contains(scope)).ToArray();

            // Set the list of scopes granted to the client application
            identity.SetScopes(validScopes);

            // Create a new authentication ticket holding the user identity
            var principal = new ClaimsPrincipal(identity);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else if (request.IsClientCredentialsGrantType())
        {
            // Validate the client credentials using scope configuration
            if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Client credentials are required."
                    }));
            }

            if (!scopeConfigService.IsValidClient(request.ClientId, request.ClientSecret))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient
                    }));
            }

            // Get client configuration
            var clientConfig = scopeConfigService.GetClientConfig(request.ClientId);
            if (clientConfig == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient
                    }));
            }

            // Create a new ClaimsIdentity containing the claims that will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            // Add the claims associated with the client application
            identity.AddClaim(OpenIddictConstants.Claims.Subject, clientConfig.ClientId);
            identity.AddClaim(OpenIddictConstants.Claims.Name, clientConfig.DisplayName);

            // Get valid scopes for this client
            var requestedScopes = request.GetScopes();
            var validScopes = scopeConfigService.GetValidScopesForClient(request.ClientId, requestedScopes.ToArray());

            // Set the list of scopes granted to the client application
            identity.SetScopes(validScopes);

            // Create a new authentication ticket holding the client identity
            var principal = new ClaimsPrincipal(identity);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.UnsupportedGrantType
            }));
    }

    [HttpPost("~/connect/logout")]
    public IActionResult Logout()
    {
        // For now, implement a simple logout that doesn't require token revocation
        // In a production system, you might want to maintain a token blacklist
        // or implement proper token revocation based on your security requirements
        
        return Ok(new { success = true, message = "Logout successful" });
    }
}
