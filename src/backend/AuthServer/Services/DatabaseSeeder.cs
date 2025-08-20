using Microsoft.AspNetCore.Identity;
using AuthServer.Data;

namespace AuthServer.Services;

public class DatabaseSeeder(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync();

            // Seed default admin user
            await SeedDefaultUserAsync();

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

    private async Task SeedDefaultUserAsync()
    {
        const string AdminEmail = "admin@example.com";
        const string AdminPassword = "Admin123!";
        const string AdminRole = "Admin";

        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, AdminPassword);
            if (result.Succeeded)
            {
                // Add admin role
                await userManager.AddToRoleAsync(adminUser, AdminRole);
                logger.LogInformation("Default admin user created: {Email} / {Password}", AdminEmail, AdminPassword);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists: {Email}", AdminEmail);
        }
    }
}
