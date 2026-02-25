using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using AuthServer.Data.Entities;
using AuthServer.Models.Authorization;
using AuthServer.Services;
using AuthServer.Services.Interfaces;
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
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ScopeConfigurationService scopeConfigService,
    IAuthorizationGrantService authorizationGrantService) : Controller
{
    private static readonly HashSet<string> ProtocolScopes = new(StringComparer.Ordinal)
    {
        Scopes.OpenId,
        Scopes.Email,
        Scopes.Profile,
        Scopes.OfflineAccess,
        Scopes.Roles
    };

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
            var result = await authorizationGrantService.HandleAuthorizationCodeGrantAsync(HttpContext);
            return ToGrantFlowActionResult(result);
        }
        else if (request.IsClientCredentialsGrantType())
        {
            var result = await authorizationGrantService.HandleClientCredentialsGrantAsync(request);
            return ToGrantFlowActionResult(result);
        }
        else if (request.IsRefreshTokenGrantType())
        {
            var result = await authorizationGrantService.HandleRefreshTokenGrantAsync(HttpContext);
            return ToGrantFlowActionResult(result);
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
            [Claims.Name] = user.Email ?? user.UserName ?? user.Id
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
        identity.AddClaim(Claims.Name, user.Email ?? user.UserName ?? user.Id);
        if (!string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(Claims.Email, user.Email);
        }

        // Add roles
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        // Product decision: grant all role-derived permission scopes for user PKCE flows
        // (not limited by requested business scopes). Protocol scopes remain request-driven.
        var configuredUserScopes = await scopeConfigService.GetUserScopesAsync(user);
        var roleDerivedPermissionScopes = configuredUserScopes.Where(scope => !IsProtocolScope(scope));
        var requestedProtocolScopes = request.GetScopes().Where(IsProtocolScope);
        var validScopes = roleDerivedPermissionScopes
            .Concat(requestedProtocolScopes)
            .Distinct(StringComparer.Ordinal)
            .ToList();

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

    private IActionResult ToGrantFlowActionResult(GrantFlowResult result)
    {
        if (result.IsSuccess)
        {
            return SignIn(result.Principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: CreateProtocolErrorProperties(result.Error!, result.ErrorDescription));
    }

    private static AuthenticationProperties CreateProtocolErrorProperties(string error, string? errorDescription)
    {
        var properties = new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error
        };

        if (!string.IsNullOrWhiteSpace(errorDescription))
        {
            properties[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription;
        }

        return new AuthenticationProperties(properties);
    }

    private static bool IsProtocolScope(string scope) => ProtocolScopes.Contains(scope);
}
