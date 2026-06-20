using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Data;

namespace Orders.IntegrationTests.Infrastructure;

public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        builder.UseContentRoot(ResolveOrdersContentRoot());

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=orders-integration",
                ["EXTERNAL_AUTH_URL"] = "https://auth.integration.test",
                ["Authentication:Authority"] = "https://auth.integration.test",
                ["Cors:AllowedOrigins:0"] = "https://localhost"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.RemoveAll<OrdersDbContext>();

            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            services.AddSingleton(connection);
            services.AddDbContext<OrdersDbContext>(options => options.UseSqlite(connection));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public HttpClient CreateApiClient(string userId = "user-1", params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Scopes", scopes.Length == 0
            ? "orders.read orders.write orders.manage"
            : string.Join(' ', scopes));
        client.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.NewGuid().ToString("N"));

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            connection?.Dispose();
            connection = null;
        }

        base.Dispose(disposing);
    }

    private static string ResolveOrdersContentRoot([CallerFilePath] string callerFilePath = "")
    {
        var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "backend", "Orders");
        if (File.Exists(Path.Combine(cwdCandidate, "Orders.csproj")))
        {
            return cwdCandidate;
        }

        var sourceDir = new DirectoryInfo(Path.GetDirectoryName(callerFilePath)!);
        while (sourceDir is not null)
        {
            var solutionPath = Path.Combine(sourceDir.FullName, "IdentityVibe.sln");
            if (File.Exists(solutionPath))
            {
                var ordersRoot = Path.Combine(sourceDir.FullName, "src", "backend", "Orders");
                if (File.Exists(Path.Combine(ordersRoot, "Orders.csproj")))
                {
                    return ordersRoot;
                }
            }

            sourceDir = sourceDir.Parent;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "IdentityVibe.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, "src", "backend", "Orders");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing IdentityVibe.sln.");
    }
}
