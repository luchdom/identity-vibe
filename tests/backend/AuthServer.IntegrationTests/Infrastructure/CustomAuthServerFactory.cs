using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace AuthServer.IntegrationTests.Infrastructure;

public sealed class CustomAuthServerFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string ExternalClientUrl = "https://client.test";
    public const string ExternalAuthUrl = "https://localhost";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        builder.UseContentRoot(ResolveAuthServerContentRoot());

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["EXTERNAL_CLIENT_URL"] = ExternalClientUrl,
                ["EXTERNAL_AUTH_URL"] = ExternalAuthUrl,
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["Cors:AllowedOrigins:1"] = "http://localhost:3000"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });
    }

    public HttpClient CreateHttpsClient(bool allowAutoRedirect = false)
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
    }

    private static string ResolveAuthServerContentRoot([CallerFilePath] string callerFilePath = "")
    {
        var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "backend", "AuthServer");
        if (File.Exists(Path.Combine(cwdCandidate, "AuthServer.csproj")))
        {
            return cwdCandidate;
        }

        var sourceDir = new DirectoryInfo(Path.GetDirectoryName(callerFilePath)!);
        while (sourceDir is not null)
        {
            var solutionPath = Path.Combine(sourceDir.FullName, "IdentityVibe.sln");
            if (File.Exists(solutionPath))
            {
                var authRoot = Path.Combine(sourceDir.FullName, "src", "backend", "AuthServer");
                if (File.Exists(Path.Combine(authRoot, "AuthServer.csproj")))
                {
                    return authRoot;
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
                return Path.Combine(directory.FullName, "src", "backend", "AuthServer");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing IdentityVibe.sln.");
    }
}
