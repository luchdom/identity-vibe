using Microsoft.AspNetCore.Identity;
using AuthServer.Data;
using AuthServer.Data.Entities;
using OpenIddict.Abstractions;
using Microsoft.Extensions.Options;
using AuthServer.Configuration;

namespace AuthServer.Services;

public class DatabaseSeeder(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ILogger<DatabaseSeeder> logger,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IOptions<ScopeConfiguration> scopeConfig,
    IOptions<ClientConfiguration> clientConfig,
    IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync();

            // Seed default users (admin and regular)
            await SeedDefaultUsersAsync();

            // Seed OpenIddict clients and scopes
            await SeedOpenIddictAsync();

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new { Name = "Admin", Description = "Administrator role with full access" },
            new { Name = "User", Description = "Standard user role" },
            new { Name = "Service", Description = "Service-to-service role" }
        };

        foreach (var roleInfo in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleInfo.Name))
            {
                var role = new AppRole
                {
                    Name = roleInfo.Name,
                    Description = roleInfo.Description,
                    IsActive = true
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {RoleName}", roleInfo.Name);
                }
                else
                {
                    logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleInfo.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedDefaultUsersAsync()
    {
        // Seed Admin User
        await SeedUserAsync(
            email: "admin@example.com",
            password: "Admin123!",
            firstName: "John",
            lastName: "Administrator",
            role: "Admin",
            phoneNumber: "+1234567890");

        // Seed Regular User
        await SeedUserAsync(
            email: "user@example.com",
            password: "User123!",
            firstName: "Jane",
            lastName: "User",
            role: "User",
            phoneNumber: "+1234567891");
    }

    private async Task SeedUserAsync(string email, string password, string firstName, string lastName, string role, string phoneNumber)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Add role
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Default {Role} user created: {Email} / {Password}", role.ToLower(), email, password);
            }
            else
            {
                logger.LogError("Failed to create {Role} user: {Errors}",
                    role.ToLower(), string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Update existing user with additional fields if they're missing
            bool updated = false;
            if (string.IsNullOrEmpty(user.FirstName))
            {
                user.FirstName = firstName;
                updated = true;
            }
            if (string.IsNullOrEmpty(user.LastName))
            {
                user.LastName = lastName;
                updated = true;
            }
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                user.PhoneNumber = phoneNumber;
                user.PhoneNumberConfirmed = true;
                updated = true;
            }

            if (updated)
            {
                await userManager.UpdateAsync(user);
                logger.LogInformation("Updated {Role} user with additional fields: {Email}", role.ToLower(), email);
            }
            else
            {
                logger.LogInformation("{Role} user already exists: {Email}", role, email);
            }

            // Ensure user has the correct role
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Added {Role} role to user: {Email}", role, email);
            }
        }
    }

    private async Task SeedOpenIddictAsync()
    {
        logger.LogInformation("Starting OpenIddict seeding...");

        // Create all required scopes from configuration
        await SeedScopesAsync();

        // Sync all clients from configuration to database
        await SyncClientsFromConfigurationAsync();

        logger.LogInformation("OpenIddict seeding completed");
    }

    private async Task SeedScopesAsync()
    {
        // Read scopes from configuration
        var configuredScopes = scopeConfig.Value.Scopes ?? new List<ScopeSettings>();

        if (!configuredScopes.Any())
        {
            logger.LogWarning("No scopes configured in appsettings.Scopes.json");
            return;
        }

        logger.LogInformation("Seeding {Count} scopes from configuration", configuredScopes.Count);

        foreach (var scope in configuredScopes)
        {
            if (await scopeManager.FindByNameAsync(scope.Name) == null)
            {
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scope.Name,
                    DisplayName = scope.DisplayName,
                    Description = scope.Description
                };

                // Add resources if any are configured
                if (scope.Resources?.Any() == true)
                {
                    foreach (var resource in scope.Resources)
                    {
                        descriptor.Resources.Add(resource);
                    }
                }

                await scopeManager.CreateAsync(descriptor);
                logger.LogInformation("Created scope: {ScopeName} - {DisplayName}", scope.Name, scope.DisplayName);
            }
            else
            {
                // Update existing scope if configuration has changed
                var existingScope = await scopeManager.FindByNameAsync(scope.Name);
                if (existingScope != null)
                {
                    var descriptor = new OpenIddictScopeDescriptor();
                    await scopeManager.PopulateAsync(descriptor, existingScope);

                    // Update display name and description
                    descriptor.DisplayName = scope.DisplayName;
                    descriptor.Description = scope.Description;

                    // Update resources
                    descriptor.Resources.Clear();
                    if (scope.Resources?.Any() == true)
                    {
                        foreach (var resource in scope.Resources)
                        {
                            descriptor.Resources.Add(resource);
                        }
                    }

                    await scopeManager.UpdateAsync(existingScope, descriptor);
                    logger.LogDebug("Updated scope: {ScopeName}", scope.Name);
                }
            }
        }
    }

    private async Task SyncClientsFromConfigurationAsync()
    {
        var configuredClients = clientConfig.Value.Clients ?? new List<ClientSettings>();

        logger.LogInformation("Syncing {Count} clients from configuration", configuredClients.Count);

        foreach (var client in configuredClients)
        {
            await CreateOrUpdateClientAsync(client);
        }
    }

    private async Task CreateOrUpdateClientAsync(ClientSettings clientSettings)
    {
        var existingClient = await applicationManager.FindByClientIdAsync(clientSettings.ClientId);

        if (existingClient == null)
        {
            // Create new client
            await CreateClientFromConfigAsync(clientSettings);
        }
        else
        {
            // Update existing client with latest configuration
            await UpdateClientFromConfigAsync(existingClient, clientSettings);
        }
    }

    private async Task CreateClientFromConfigAsync(ClientSettings clientSettings)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientSettings.ClientId,
            DisplayName = clientSettings.DisplayName,
            ConsentType = clientSettings.RequireConsent
                ? OpenIddictConstants.ConsentTypes.Explicit
                : OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = clientSettings.Type.ToLowerInvariant() == "confidential"
                ? OpenIddictConstants.ClientTypes.Confidential
                : OpenIddictConstants.ClientTypes.Public
        };

        // Set client secret for confidential clients
        if (clientSettings.Type.ToLowerInvariant() == "confidential" &&
            !string.IsNullOrEmpty(clientSettings.ClientSecret))
        {
            descriptor.ClientSecret = clientSettings.ClientSecret;
        }

        // Add grant type permissions
        foreach (var grantType in clientSettings.AllowedGrantTypes ?? new())
        {
            var permission = grantType.ToLowerInvariant() switch
            {
                "authorization_code" => OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                "client_credentials" => OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                "refresh_token" => OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                "password" => OpenIddictConstants.Permissions.GrantTypes.Password,
                _ => $"gt:{grantType}"
            };
            descriptor.Permissions.Add(permission);
        }

        // Add endpoint permissions based on grant types
        if (clientSettings.AllowedGrantTypes?.Contains("authorization_code") == true)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
        }
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);

        // Add scope permissions
        foreach (var scope in clientSettings.AllowedScopes ?? new())
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        // Add redirect URIs with environment variable replacement
        foreach (var uri in clientSettings.RedirectUris ?? new())
        {
            var resolvedUri = ReplaceUrlTokens(uri);
            descriptor.RedirectUris.Add(new Uri(resolvedUri));
        }

        // Add post-logout redirect URIs with environment variable replacement
        foreach (var uri in clientSettings.PostLogoutRedirectUris ?? new())
        {
            var resolvedUri = ReplaceUrlTokens(uri);
            descriptor.PostLogoutRedirectUris.Add(new Uri(resolvedUri));
        }

        // Handle PKCE requirement
        if (clientSettings.RequirePkce && clientSettings.Type.ToLowerInvariant() == "public")
        {
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        }

        await applicationManager.CreateAsync(descriptor);
        logger.LogInformation("Created client: {ClientId} ({DisplayName}, Type: {Type})",
            clientSettings.ClientId, clientSettings.DisplayName, clientSettings.Type);
    }

    private async Task UpdateClientFromConfigAsync(object existingClient, ClientSettings clientSettings)
    {
        // Delete and recreate (simpler than complex update logic)
        await applicationManager.DeleteAsync(existingClient);
        await CreateClientFromConfigAsync(clientSettings);
        logger.LogInformation("Updated client: {ClientId} ({DisplayName})", clientSettings.ClientId, clientSettings.DisplayName);
    }

    private string ReplaceUrlTokens(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("${EXTERNAL_AUTH_URL}", configuration["EXTERNAL_AUTH_URL"] ?? "https://localhost:5000")
            .Replace("${EXTERNAL_GATEWAY_URL}", configuration["EXTERNAL_GATEWAY_URL"] ?? "http://localhost:5002")
            .Replace("${EXTERNAL_CLIENT_URL}", configuration["EXTERNAL_CLIENT_URL"] ?? "http://localhost:5173")
            .Replace("${INTERNAL_AUTH_URL}", configuration["INTERNAL_AUTH_URL"] ?? "http://authserver:8080")
            .Replace("${INTERNAL_GATEWAY_URL}", configuration["INTERNAL_GATEWAY_URL"] ?? "http://gateway:8080")
            .Replace("${INTERNAL_ORDERS_URL}", configuration["INTERNAL_ORDERS_URL"] ?? "http://orders:8080");
    }
}
