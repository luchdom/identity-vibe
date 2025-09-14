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
    IOptions<ScopeConfiguration> scopeConfig)
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
        // Create required scopes manually
        var requiredScopes = new[]
        {
            "openid", "profile", "email", "offline_access", 
            // Orders-specific scopes
            "orders.read", "orders.write", "orders.manage",
            // Profile scopes
            "profile.read", "profile.write", 
            // Admin scopes
            "admin.manage", "admin.users", "admin.roles"
        };

        foreach (var scopeName in requiredScopes)
        {
            if (await scopeManager.FindByNameAsync(scopeName) == null)
            {
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = scopeName switch
                    {
                        "openid" => "OpenID Connect",
                        "profile" => "Profile", 
                        "email" => "Email",
                        "offline_access" => "Offline Access",
                        // Orders scopes
                        "orders.read" => "Read Orders",
                        "orders.write" => "Create/Update Orders",
                        "orders.manage" => "Manage Orders (Admin)",
                        // Profile scopes
                        "profile.read" => "Read Profile", 
                        "profile.write" => "Write Profile",
                        // Admin scopes
                        "admin.manage" => "Admin Management",
                        "admin.users" => "Admin User Management",
                        "admin.roles" => "Admin Role Management",
                        _ => scopeName
                    }
                };

                await scopeManager.CreateAsync(descriptor);
                logger.LogInformation("Created scope: {ScopeName}", scopeName);
            }
        }
    }

    private async Task SyncClientsFromConfigurationAsync()
    {
        var configuredClients = scopeConfig.Value.ServiceClients?.Clients ?? new Dictionary<string, ServiceClientConfig>();
        
        logger.LogInformation("Syncing {Count} clients from configuration", configuredClients.Count);

        foreach (var (key, clientConfig) in configuredClients)
        {
            await CreateOrUpdateClientAsync(clientConfig);
        }
    }

    private async Task CreateOrUpdateClientAsync(ServiceClientConfig clientConfig)
    {
        var existingClient = await applicationManager.FindByClientIdAsync(clientConfig.ClientId);
        
        if (existingClient == null)
        {
            // Create new client
            await CreateClientFromConfigAsync(clientConfig);
        }
        else
        {
            // Update existing client with latest configuration
            await UpdateClientFromConfigAsync(existingClient, clientConfig);
        }
    }

    private async Task CreateClientFromConfigAsync(ServiceClientConfig clientConfig)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientConfig.ClientId,
            DisplayName = clientConfig.DisplayName,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit
        };

        // Set client secret only if provided (supports public clients)
        if (!string.IsNullOrEmpty(clientConfig.ClientSecret))
        {
            descriptor.ClientSecret = clientConfig.ClientSecret;
        }

        // Add grant type permissions
        if (clientConfig.GrantTypes?.Contains("password") == true)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
        if (clientConfig.GrantTypes?.Contains("refresh_token") == true)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        if (clientConfig.GrantTypes?.Contains("client_credentials") == true)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);

        // Add endpoint permissions
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        
        // Add scope permissions
        foreach (var scope in clientConfig.AllowedScopes ?? [])
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        // Add redirect URIs
        foreach (var uri in clientConfig.RedirectUris ?? [])
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        await applicationManager.CreateAsync(descriptor);
        logger.LogInformation("Created client: {ClientId} ({DisplayName})", clientConfig.ClientId, clientConfig.DisplayName);
    }

    private async Task UpdateClientFromConfigAsync(object existingClient, ServiceClientConfig clientConfig)
    {
        // Delete and recreate (simpler than complex update logic)
        await applicationManager.DeleteAsync(existingClient);
        await CreateClientFromConfigAsync(clientConfig);
        logger.LogInformation("Updated client: {ClientId} ({DisplayName})", clientConfig.ClientId, clientConfig.DisplayName);
    }
}
