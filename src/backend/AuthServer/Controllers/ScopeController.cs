using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthServer.Services;
using AuthServer.Models;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ScopeController(ScopeConfigurationService scopeConfigService) : ControllerBase
{
    [HttpGet("clients")]
    public ActionResult<IEnumerable<object>> GetClients()
    {
        var clients = scopeConfigService.GetAllClients().Select(client => new
        {
            client.ClientId,
            client.DisplayName,
            client.Description,
            client.AllowedScopes,
            client.GrantTypes,
            client.RedirectUris
        });

        return Ok(clients);
    }

    [HttpGet("clients/{clientId}")]
    public ActionResult<object> GetClient(string clientId)
    {
        var client = scopeConfigService.GetClientConfig(clientId);
        if (client == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            client.ClientId,
            client.DisplayName,
            client.Description,
            client.AllowedScopes,
            client.GrantTypes,
            client.RedirectUris
        });
    }

    [HttpGet("scopes")]
    public ActionResult<object> GetScopes()
    {
        // Return all available scopes (this could be enhanced to read from configuration)
        var scopes = new
        {
            OpenIdScopes = new[]
            {
                "openid",
                "profile",
                "email",
                "offline_access"
            },
            UserScopes = new[]
            {
                "orders.read",
                "orders.write", 
                "orders.manage",
                "profile.read",
                "profile.write",
                "admin.manage"
            },
            ServiceScopes = new[]
            {
                "internal.orders.read",
                "internal.orders.write",
                "internal.orders.manage"
            }
        };

        return Ok(scopes);
    }

    [HttpGet("validate-client")]
    public ActionResult<object> ValidateClient([FromQuery] string clientId, [FromQuery] string clientSecret)
    {
        var isValid = scopeConfigService.IsValidClient(clientId, clientSecret);
        var client = scopeConfigService.GetClientConfig(clientId);

        return Ok(new
        {
            IsValid = isValid,
            Client = client != null ? new
            {
                client.ClientId,
                client.DisplayName,
                client.Description,
                client.AllowedScopes,
                client.GrantTypes
            } : null
        });
    }

    [HttpGet("validate-scope")]
    public ActionResult<object> ValidateScope([FromQuery] string clientId, [FromQuery] string scope)
    {
        var isValid = scopeConfigService.IsValidScope(clientId, scope);
        var client = scopeConfigService.GetClientConfig(clientId);

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
