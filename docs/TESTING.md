# Testing Guide

## Backend Integration Tests (AuthServer)

This repo includes backend integration tests for `AuthServer` authentication flows using:

- `xUnit`
- `FluentAssertions`
- `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)
- `Testcontainers` with PostgreSQL

Location:

- `tests/backend/AuthServer.IntegrationTests`

## What These Tests Validate

- Real ASP.NET Core middleware/pipeline
- Identity + OpenIddict endpoint wiring
- Real PostgreSQL persistence/seeding
- OAuth2/OIDC auth flows (`authorize`, login, token, userinfo, refresh, client credentials)

These tests are intended to catch wiring/config/auth regressions that unit tests typically miss.

## Prerequisites

- Docker Desktop (or compatible Docker engine) running locally
- .NET 8 SDK

## Run Tests

From repo root:

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj
```

Run a single class:

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj --filter "FullyQualifiedName~AuthenticationFlowTests"
```

From the test project directory:

```bash
cd tests/backend/AuthServer.IntegrationTests
dotnet test
```

## Best Practices in This Repo

### Prefer real auth integration over mocks for auth changes

Do not mock these for auth integration tests:

- Identity (`UserManager`, `SignInManager`)
- OpenIddict managers/server pipeline
- EF Core DbContext
- ASP.NET Core auth middleware

Reason:

- The highest-value regressions are usually in configuration, middleware order, cookies, redirects, anti-forgery, and token endpoint wiring.

### When `NSubstitute` is appropriate

Use `NSubstitute` only for true external dependencies (for example, an email sender or external webhook client) if those are introduced later and are outside the auth behavior being validated.

### Isolate test data

- Tests use PostgreSQL Testcontainers.
- Prefer isolated databases per test class (or per test) to avoid flaky state coupling.

### Handle redirects and cookies intentionally

The auth flow requires:

- cookie persistence across requests
- anti-forgery token submission for `/Account/Login`
- manual redirect inspection (`Location` headers)

Use a `WebApplicationFactory` client configured for HTTPS base address and `AllowAutoRedirect = false` for flow assertions.

### Account for HTTPS redirection

`AuthServer` uses `UseHttpsRedirection()`. In tests, create clients with an HTTPS base address (for example `https://localhost`) to avoid unnecessary redirect noise.

## Repo-Specific Integration Test Pattern (Example)

### Factory skeleton

```csharp
public sealed class CustomAuthServerFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        builder.UseContentRoot("<repo>/src/backend/AuthServer");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["EXTERNAL_CLIENT_URL"] = "https://client.test"
            });
        });
    }
}
```

### Example test shape

```csharp
[Fact]
public async Task HealthEndpoint_ReturnsOk()
{
    await using var host = await CreateHostAsync();
    var response = await host.Client.GetAsync("/health");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Common Pitfalls

1. Missing anti-forgery token in login form
- The login POST is protected by `[ValidateAntiForgeryToken]`.
- Integration tests should assert the login page renders `__RequestVerificationToken`.

2. Wrong content root in `WebApplicationFactory`
- `AuthServer` loads `appsettings.Clients.json`, `appsettings.Scopes.json`, and Razor views by path/content root.
- Set content root explicitly to `src/backend/AuthServer`.

3. OpenIddict key/cert instability in tests
- Use a test-only `IntegrationTest` environment path with ephemeral keys for reliable local/CI runs.

4. Redirect URI mismatch
- Ensure the test `redirect_uri` matches the seeded `web-admin` client redirect URI.

5. Container startup timing / Docker availability
- If Docker is not running or startup is slow, Testcontainers initialization will fail before tests execute.
- Start Docker first and rerun the suite; avoid adding sleeps in tests.
