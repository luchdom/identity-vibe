using Microsoft.Extensions.Options;

namespace AuthServer.Configuration;

public sealed class ClientConfigurationValidator : IValidateOptions<ClientConfiguration>
{
    public ValidateOptionsResult Validate(string? name, ClientConfiguration options)
    {
        if (options.Clients is null || options.Clients.Count == 0)
        {
            return ValidateOptionsResult.Fail("Client configuration must contain at least one client.");
        }

        var clientIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < options.Clients.Count; i++)
        {
            var client = options.Clients[i];
            var prefix = $"Clients[{i}]";

            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                return ValidateOptionsResult.Fail($"{prefix}.ClientId is required.");
            }

            if (!clientIds.Add(client.ClientId))
            {
                return ValidateOptionsResult.Fail($"Duplicate client id '{client.ClientId}' in client configuration.");
            }

            if (string.IsNullOrWhiteSpace(client.DisplayName))
            {
                return ValidateOptionsResult.Fail($"{prefix}.DisplayName is required.");
            }

            if (string.IsNullOrWhiteSpace(client.Type))
            {
                return ValidateOptionsResult.Fail($"{prefix}.Type is required.");
            }

            if (client.AllowedGrantTypes is null || client.AllowedGrantTypes.Count == 0)
            {
                return ValidateOptionsResult.Fail($"{prefix}.AllowedGrantTypes must contain at least one grant type.");
            }

            if (client.AllowedGrantTypes.Any(string.IsNullOrWhiteSpace))
            {
                return ValidateOptionsResult.Fail($"{prefix}.AllowedGrantTypes cannot contain empty values.");
            }

            if (client.AllowedScopes is null || client.AllowedScopes.Count == 0)
            {
                return ValidateOptionsResult.Fail($"{prefix}.AllowedScopes must contain at least one scope.");
            }

            if (client.AllowedScopes.Any(string.IsNullOrWhiteSpace))
            {
                return ValidateOptionsResult.Fail($"{prefix}.AllowedScopes cannot contain empty values.");
            }

            var isConfidential = string.Equals(client.Type, "Confidential", StringComparison.OrdinalIgnoreCase);
            if (isConfidential && string.IsNullOrWhiteSpace(client.ClientSecret))
            {
                return ValidateOptionsResult.Fail($"{prefix}.ClientSecret is required for confidential clients.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
