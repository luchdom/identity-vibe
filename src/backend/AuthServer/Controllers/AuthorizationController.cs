using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using AuthServer.Data.Entities;
using AuthServer.Services;
using Microsoft.Extensions.Options;
using AuthServer.Configuration;
using Microsoft.AspNetCore.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.Linq;

namespace AuthServer.Controllers;

public class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager,
    IOpenIddictTokenManager tokenManager,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ScopeConfigurationService scopeConfigService,
    IOptions<ClientConfiguration> clientConfig) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to retrieve the user principal
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        // If the user is not authenticated, redirect to the login page
        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        // Retrieve the user from the result
        var user = await userManager.GetUserAsync(result.Principal) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        // Retrieve the application details
        var application = await applicationManager.FindByClientIdAsync(request.ClientId) ??
            throw new InvalidOperationException("The application details cannot be found.");

        // Create the principal
        var principal = await CreateUserPrincipalAsync(user, application, request);

        // Automatically authorize the client (skip consent for now)
        // In production, you might want to show a consent page here

        // Create authorization if it doesn't exist
        var authorizations = await authorizationManager.FindAsync(
            subject: user.Id,
            client: await applicationManager.GetIdAsync(application),
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        var authorization = authorizations.FirstOrDefault();
        if (authorization == null)
        {
            authorization = await authorizationManager.CreateAsync(
                principal: principal,
                subject: user.Id,
                client: await applicationManager.GetIdAsync(application),
                type: AuthorizationTypes.Permanent,
                scopes: principal.GetScopes());
        }

        principal.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));

        // Return authorization code
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            // Retrieve the claims principal stored in the authorization code
            var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is no longer valid."
                    }));
            }

            var userId = authenticateResult.Principal.GetClaim(Claims.Subject);
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user associated with the authorization code no longer exists."
                    }));
            }

            // Ensure the user is still active
            if (!user.IsActive)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account has been disabled."
                    }));
            }

            // Return the existing principal
            return SignIn(authenticateResult.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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

            // Validate client using OpenIddict application manager
            var application = await applicationManager.FindByClientIdAsync(request.ClientId);
            if (application == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient
                    }));
            }

            // Validate client secret for confidential clients
            var clientType = await applicationManager.GetClientTypeAsync(application);
            if (clientType == OpenIddictConstants.ClientTypes.Confidential)
            {
                if (!await applicationManager.ValidateClientSecretAsync(application, request.ClientSecret))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient
                        }));
                }
            }

            // Create a new ClaimsIdentity containing the claims that will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            // Add the claims associated with the client application
            identity.AddClaim(OpenIddictConstants.Claims.Subject, await applicationManager.GetClientIdAsync(application));
            identity.AddClaim(OpenIddictConstants.Claims.Name, await applicationManager.GetDisplayNameAsync(application) ?? string.Empty);

            // Get valid scopes for this client from OpenIddict
            var requestedScopes = request.GetScopes();
            var validScopes = new List<string>();

            foreach (var scope in requestedScopes)
            {
                if (await applicationManager.HasPermissionAsync(application, OpenIddictConstants.Permissions.Prefixes.Scope + scope))
                {
                    validScopes.Add(scope);
                }
            }

            // Set the list of scopes granted to the client application
            identity.SetScopes(validScopes);

            // Create a new authentication ticket holding the client identity
            var principal = new ClaimsPrincipal(identity);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else if (request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the refresh token
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    }));
            }

            // Retrieve the user profile corresponding to the refresh token
            var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token does not contain a valid user identifier."
                    }));
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user associated with the refresh token no longer exists."
                    }));
            }

            // Check if user is still active
            if (!user.IsActive)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account is disabled."
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

            // Get user scopes based on configuration (refresh tokens should maintain the same scopes)
            var originalScopes = result.Principal.GetScopes();
            var userScopes = scopeConfigService.GetUserScopes(user);

            // Only keep scopes that the user is still authorized for
            var validScopes = originalScopes.Where(scope => userScopes.Contains(scope)).ToArray();

            // Set the list of scopes granted to the client application
            identity.SetScopes(validScopes);

            // Create a new authentication ticket holding the user identity
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

    [HttpPost("~/connect/introspect")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Introspect()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Retrieve the principal from the introspection request
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            return Ok(new
            {
                active = false
            });
        }

        // Return the claims as the introspection response
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["active"] = true
        };

        // Add standard claims
        foreach (var claim in result.Principal.Claims)
        {
            // Skip internal OpenIddict claims
            if (claim.Type.StartsWith("oi_"))
                continue;

            // Map claim types to standard introspection response fields
            var claimType = claim.Type switch
            {
                Claims.Subject => "sub",
                Claims.Name => "name",
                Claims.GivenName => "given_name",
                Claims.FamilyName => "family_name",
                Claims.Email => "email",
                Claims.Role => "role",
                Claims.Scope => "scope",
                Claims.Audience => "aud",
                Claims.Issuer => "iss",
                Claims.IssuedAt => "iat",
                Claims.ExpiresAt => "exp",
                Claims.ClientId => "client_id",
                _ => claim.Type
            };

            if (claims.ContainsKey(claimType))
            {
                if (claims[claimType] is List<string> list)
                {
                    list.Add(claim.Value);
                }
                else
                {
                    claims[claimType] = new List<string> { claims[claimType].ToString()!, claim.Value };
                }
            }
            else
            {
                claims[claimType] = claim.Value;
            }
        }

        // Add scopes as space-separated string
        var scopes = result.Principal.GetScopes();
        if (scopes.Any())
        {
            claims["scope"] = string.Join(" ", scopes);
        }

        return Ok(claims);
    }

    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Userinfo()
    {
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
        if (principal == null)
        {
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var userId = principal.GetClaim(Claims.Subject);
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.Id,
            [Claims.Name] = $"{user.FirstName} {user.LastName}",
            [Claims.GivenName] = user.FirstName,
            [Claims.FamilyName] = user.LastName
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims[Claims.Email] = user.Email;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        // Add roles if the roles scope was granted
        if (principal.HasScope(Scopes.Roles))
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                claims["roles"] = roles;
            }
        }

        return Ok(claims);
    }

    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        // Redirect to the post-logout redirect URI if provided
        var request = HttpContext.GetOpenIddictServerRequest();
        if (!string.IsNullOrEmpty(request?.PostLogoutRedirectUri))
        {
            return Redirect(request.PostLogoutRedirectUri);
        }

        return Ok();
    }

    private async Task<ClaimsPrincipal> CreateUserPrincipalAsync(AppUser user, object application, OpenIddictRequest request)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // Add core claims
        identity.AddClaim(Claims.Subject, user.Id);
        identity.AddClaim(Claims.Name, $"{user.FirstName} {user.LastName}");
        identity.AddClaim(Claims.Email, user.Email);
        identity.AddClaim(Claims.GivenName, user.FirstName);
        identity.AddClaim(Claims.FamilyName, user.LastName);

        // Add roles
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        // Get user scopes based on configuration
        var userScopes = await scopeConfigService.GetUserScopesAsync(user);

        // Filter requested scopes against user's allowed scopes
        var requestedScopes = request.GetScopes();
        var validScopes = requestedScopes.Where(scope =>
            userScopes.Contains(scope) ||
            scope == Scopes.OpenId ||
            scope == Scopes.Email ||
            scope == Scopes.Profile ||
            scope == Scopes.OfflineAccess).ToList();

        identity.SetScopes(validScopes);
        identity.SetResources(await scopeManager.ListResourcesAsync(validScopes.ToImmutableArray()).ToListAsync());

        // Set destinations for claims
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, identity));
        }

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
    {
        var destinations = new List<string>();

        // Always include in access token
        destinations.Add(Destinations.AccessToken);

        // Include in ID token for standard claims
        if (identity.HasScope(Scopes.OpenId))
        {
            if (claim.Type == Claims.Subject ||
                claim.Type == Claims.Name ||
                claim.Type == Claims.Email ||
                claim.Type == Claims.GivenName ||
                claim.Type == Claims.FamilyName)
            {
                destinations.Add(Destinations.IdentityToken);
            }
        }

        return destinations;
    }
}
