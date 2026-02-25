using AuthServer.Models.Authorization;
using OpenIddict.Abstractions;

namespace AuthServer.Services.Interfaces;

public interface IAuthorizationGrantService
{
    Task<GrantFlowResult> HandleAuthorizationCodeGrantAsync(HttpContext httpContext);
    Task<GrantFlowResult> HandleClientCredentialsGrantAsync(OpenIddictRequest request);
    Task<GrantFlowResult> HandleRefreshTokenGrantAsync(HttpContext httpContext);
}
