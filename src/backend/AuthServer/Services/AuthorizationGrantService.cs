using System.Collections.Immutable;
using System.Security.Claims;
using AuthServer.Data.Entities;
using AuthServer.Models.Authorization;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Services;

public sealed class AuthorizationGrantService(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    UserManager<AppUser> userManager,
    ScopeConfigurationService scopeConfigService) : IAuthorizationGrantService
{
    private static readonly HashSet<string> ProtocolScopes = new(StringComparer.Ordinal)
    {
        Scopes.OpenId,
        Scopes.Email,
        Scopes.Profile,
        Scopes.OfflineAccess,
        Scopes.Roles
    };

    public async Task<GrantFlowResult> HandleAuthorizationCodeGrantAsync(HttpContext httpContext)
    {
        var authenticateResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The authorization code is no longer valid.");
        }

        var userId = authenticateResult.Principal.GetClaim(Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The authorization code does not contain a valid user identifier.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The user associated with the authorization code no longer exists.");
        }

        if (!user.IsActive)
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The user account has been disabled.");
        }

        return GrantFlowResult.SignIn(authenticateResult.Principal);
    }

    public async Task<GrantFlowResult> HandleClientCredentialsGrantAsync(OpenIddictRequest request)
    {
        if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidClient,
                "Client credentials are required.");
        }

        var application = await applicationManager.FindByClientIdAsync(request.ClientId);
        if (application == null)
        {
            return GrantFlowResult.Forbid(Errors.InvalidClient);
        }

        var clientType = await applicationManager.GetClientTypeAsync(application);
        if (clientType == ClientTypes.Confidential &&
            !await applicationManager.ValidateClientSecretAsync(application, request.ClientSecret))
        {
            return GrantFlowResult.Forbid(Errors.InvalidClient);
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        var clientId = await applicationManager.GetClientIdAsync(application) ?? request.ClientId;
        if (string.IsNullOrEmpty(clientId))
        {
            return GrantFlowResult.Forbid(Errors.InvalidClient);
        }

        identity.AddClaim(Claims.Subject, clientId);
        identity.AddClaim(Claims.Name, await applicationManager.GetDisplayNameAsync(application) ?? clientId);

        var validScopes = new List<string>();
        foreach (var scope in request.GetScopes())
        {
            if (await applicationManager.HasPermissionAsync(application, Permissions.Prefixes.Scope + scope))
            {
                validScopes.Add(scope);
            }
        }

        identity.SetScopes(validScopes);
        if (validScopes.Count > 0)
        {
            identity.SetResources(await scopeManager.ListResourcesAsync(validScopes.ToImmutableArray()).ToListAsync());
        }

        return GrantFlowResult.SignIn(new ClaimsPrincipal(identity));
    }

    public async Task<GrantFlowResult> HandleRefreshTokenGrantAsync(HttpContext httpContext)
    {
        var authenticateResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The refresh token is no longer valid.");
        }

        var originalPrincipal = authenticateResult.Principal;
        var userId = originalPrincipal.GetClaim(Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The refresh token does not contain a valid user identifier.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The user associated with the refresh token no longer exists.");
        }

        if (!user.IsActive)
        {
            return GrantFlowResult.Forbid(
                Errors.InvalidGrant,
                "The user account is disabled.");
        }

        var principal = await CreateRefreshPrincipalAsync(user, originalPrincipal);
        return GrantFlowResult.SignIn(principal);
    }

    private async Task<ClaimsPrincipal> CreateRefreshPrincipalAsync(AppUser user, ClaimsPrincipal originalPrincipal)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id);
        identity.AddClaim(Claims.Name, user.Email ?? user.UserName ?? user.Id);
        if (!string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(Claims.Email, user.Email);
        }

        var roles = await scopeConfigService.GetActiveRoleNamesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        var currentUserScopes = await scopeConfigService.GetUserScopesAsync(user);
        var currentPermissionScopes = currentUserScopes.Where(scope => !IsProtocolScope(scope));

        var originalProtocolScopes = originalPrincipal.GetScopes().Where(IsProtocolScope);

        var grantedScopes = currentPermissionScopes
            .Concat(originalProtocolScopes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        identity.SetScopes(grantedScopes);
        if (grantedScopes.Length > 0)
        {
            identity.SetResources(await scopeManager.ListResourcesAsync(grantedScopes.ToImmutableArray()).ToListAsync());
        }

        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, identity));
        }

        return new ClaimsPrincipal(identity);
    }

    private static bool IsProtocolScope(string scope) => ProtocolScopes.Contains(scope);

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
    {
        var destinations = new List<string> { Destinations.AccessToken };

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
