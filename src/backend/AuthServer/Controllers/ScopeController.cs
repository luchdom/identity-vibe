using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthServer.Services;
using AuthServer.Models;
using Microsoft.Extensions.Options;
using AuthServer.Configuration;
using OpenIddict.Abstractions;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ScopeController(
    ScopeConfigurationService scopeConfigService,
    IOptions<ClientConfiguration> clientConfig,
    IOpenIddictApplicationManager applicationManager) : ControllerBase
{
    [HttpGet("clients")]
    public ActionResult<IEnumerable<object>> GetClients()
    {
        var clients = clientConfig.Value.Clients.Select(client => new
        {
            client.ClientId,
            client.DisplayName,
            client.Type,
            client.AllowedScopes,
            client.AllowedGrantTypes,
            client.RedirectUris,
            client.RequireConsent,
            client.RequirePkce
        });

        return Ok(clients);
    }

    [HttpGet("clients/{clientId}")]
    public ActionResult<object> GetClient(string clientId)
    {
        var client = clientConfig.Value.Clients.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            client.ClientId,
            client.DisplayName,
            client.Type,
            client.AllowedScopes,
            client.AllowedGrantTypes,
            client.RedirectUris,
            client.PostLogoutRedirectUris,
            client.RequireConsent,
            client.RequirePkce
        });
    }

    [HttpGet("scopes")]
    public ActionResult<object> GetScopes()
    {
        var allScopes = scopeConfigService.GetAllScopes().Select(scope => new
        {
            scope.Name,
            scope.DisplayName,
            scope.Description,
            scope.Resources
        });

        return Ok(allScopes);
    }

    [HttpGet("validate-client")]
    public async Task<ActionResult<object>> ValidateClient([FromQuery] string clientId, [FromQuery] string clientSecret)
    {
        var client = clientConfig.Value.Clients.FirstOrDefault(c => c.ClientId == clientId);
        var isValid = client != null &&
                     (client.Type.ToLowerInvariant() == "public" ||
                      client.ClientSecret == clientSecret);

        return Ok(new
        {
            IsValid = isValid,
            Client = client != null ? new
            {
                client.ClientId,
                client.DisplayName,
                client.Type,
                client.AllowedScopes,
                client.AllowedGrantTypes
            } : null
        });
    }

    [HttpGet("validate-scope")]
    public ActionResult<object> ValidateScope([FromQuery] string clientId, [FromQuery] string scope)
    {
        var client = clientConfig.Value.Clients.FirstOrDefault(c => c.ClientId == clientId);
        var isValid = client?.AllowedScopes.Contains(scope) == true;

        return Ok(new
        {
            IsValid = isValid,
            ClientId = clientId,
            Scope = scope,
            Client = client != null ? new
            {
                client.ClientId,
                client.DisplayName,
                client.AllowedScopes
            } : null
        });
    }
}
