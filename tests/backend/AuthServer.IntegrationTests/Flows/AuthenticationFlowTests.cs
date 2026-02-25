using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Data.Entities;
using AuthServer.IntegrationTests.Contracts;
using AuthServer.IntegrationTests.Helpers;
using AuthServer.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.IntegrationTests.Flows;

[Collection(AuthServerIntegrationTestCollection.Name)]
public sealed class AuthenticationFlowTests(PostgresContainerFixture postgresFixture)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        await using var host = await CreateHostAsync();

        var response = await host.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DiscoveryEndpoint_ReturnsOpenIdConfiguration()
    {
        await using var host = await CreateHostAsync();

        var response = await host.Client.GetAsync("/.well-known/openid-configuration");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("authorization_endpoint");
        body.Should().Contain("token_endpoint");
        body.Should().Contain("userinfo_endpoint");
    }

    [Fact]
    public async Task LoginPage_RendersAntiForgeryToken()
    {
        await using var host = await CreateHostAsync();

        var response = await host.Client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parsed = await LoginPageParser.ParseAsync(html);
        parsed.AntiForgeryToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AuthorizationCodePkce_WithRealLoginFlow_ReturnsAccessAndRefreshTokens()
    {
        await using var host = await CreateHostAsync();

        var authResult = await RunAuthorizationCodeFlowAsync(host.Client, "user@example.com", "User123!");

        authResult.StateFromCallback.Should().Be(authResult.State);
        authResult.Token.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResult.Token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResult.Token.TokenType.Should().Be("Bearer", because: "OpenIddict returns bearer tokens for the configured flows");
    }

    [Fact]
    public async Task AuthorizationCodePkce_UserRole_GetsRoleDerivedPermissionScopes_EvenWhenNotRequested()
    {
        await using var host = await CreateHostAsync();

        var authResult = await RunAuthorizationCodeFlowAsync(
            host.Client,
            "user@example.com",
            "User123!",
            requestedScope: "openid profile email");

        var grantedScopes = ParseScopes(authResult.Token);

        grantedScopes.Should().Contain(["openid", "profile", "email"]);
        grantedScopes.Should().Contain(["orders.read", "orders.write", "profile.read", "profile.write"]);
        grantedScopes.Should().NotContain("orders.manage");
        grantedScopes.Should().NotContain("offline_access");
    }

    [Fact]
    public async Task AuthorizationCodePkce_AdminRole_GetsOrdersManageFromRole_EvenWhenNotRequested()
    {
        await using var host = await CreateHostAsync();

        var authResult = await RunAuthorizationCodeFlowAsync(
            host.Client,
            "admin@example.com",
            "Admin123!",
            requestedScope: "openid profile email");

        var grantedScopes = ParseScopes(authResult.Token);

        grantedScopes.Should().Contain("orders.manage");
        grantedScopes.Should().Contain("orders.read");
        grantedScopes.Should().Contain("orders.write");
        grantedScopes.Should().NotContain("offline_access");
    }

    [Fact]
    public async Task UserInfo_WithValidAccessToken_ReturnsUserClaims()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(host.Client, "user@example.com", "User123!");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token.AccessToken);

        var response = await host.Client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("\"sub\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"email\"");
    }

    [Fact]
    public async Task RefreshTokenGrant_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(host.Client, "user@example.com", "User123!");

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = "web-admin",
            ["refresh_token"] = authResult.Token.RefreshToken!
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshTokenGrant_WithInvalidRefreshToken_ReturnsInvalidGrant()
    {
        await using var host = await CreateHostAsync();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = "web-admin",
            ["refresh_token"] = "not-a-valid-refresh-token"
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("invalid_grant");
    }

    [Fact]
    public async Task RefreshTokenGrant_ForDisabledUser_ReturnsInvalidGrant()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(host.Client, "user@example.com", "User123!");

        await SetUserActiveAsync(host.ConnectionString, "user@example.com", isActive: false);

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = "web-admin",
            ["refresh_token"] = authResult.Token.RefreshToken!
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("invalid_grant");
        body.Should().Contain("disabled");
    }

    [Fact]
    public async Task RefreshTokenGrant_WhenRoleDowngraded_RemovesOrdersManage()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(
            host.Client,
            "admin@example.com",
            "Admin123!",
            requestedScope: "openid profile email offline_access");

        ParseScopes(authResult.Token).Should().Contain("orders.manage");

        await SetUserRolesAsync(host.ConnectionString, "admin@example.com", "User");

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = "web-admin",
            ["refresh_token"] = authResult.Token.RefreshToken!
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        token.Should().NotBeNull();
        ParseScopes(token!).Should().NotContain("orders.manage");
    }

    [Fact]
    public async Task RefreshTokenGrant_WhenRoleUpgraded_AddsOrdersManage()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(
            host.Client,
            "user@example.com",
            "User123!",
            requestedScope: "openid profile email offline_access");

        ParseScopes(authResult.Token).Should().NotContain("orders.manage");

        await SetUserRolesAsync(host.ConnectionString, "user@example.com", "Admin");

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = "web-admin",
            ["refresh_token"] = authResult.Token.RefreshToken!
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        token.Should().NotBeNull();
        ParseScopes(token!).Should().Contain("orders.manage");
    }

    [Fact]
    public async Task ClientCredentialsGrant_WithValidOrdersClient_ReturnsAccessToken()
    {
        await using var host = await CreateHostAsync();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "orders",
            ["client_secret"] = "orders-secret",
            ["scope"] = "orders.read orders.write orders.manage"
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task ClientCredentialsGrant_WithInvalidSecret_ReturnsInvalidClient()
    {
        await using var host = await CreateHostAsync();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "orders",
            ["client_secret"] = "wrong-secret",
            ["scope"] = "orders.read"
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("invalid_client");
    }

    [Fact]
    public async Task ClientCredentialsGrant_WithMissingSecret_ReturnsInvalidRequest()
    {
        await using var host = await CreateHostAsync();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "orders",
            ["scope"] = "orders.read"
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("invalid_request");
        body.Should().Contain("client_secret");
    }

    [Fact]
    public async Task ClientCredentialsGrant_WithUnauthorizedRequestedScope_ReturnsProtocolError()
    {
        await using var host = await CreateHostAsync();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "orders",
            ["client_secret"] = "orders-secret",
            ["scope"] = "orders.read admin.full"
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("invalid_request");
        body.Should().Contain("not allowed to use the specified scope");
    }

    [Fact]
    public async Task TokenEndpoint_WithUnsupportedGrantType_ReturnsUnsupportedGrantType()
    {
        await using var host = await CreateHostAsync();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "web-admin",
            ["username"] = "user@example.com",
            ["password"] = "User123!"
        };

        var response = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(formData));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("unsupported_grant_type");
    }

    [Fact]
    public async Task AuthorizationCodePkce_WithInvalidCodeVerifier_ReturnsInvalidGrant()
    {
        await using var host = await CreateHostAsync();
        var authorizeResult = await CompleteAuthorizationAndGetCodeAsync(host.Client, "user@example.com", "User123!");

        var tokenRequestData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "web-admin",
            ["code"] = authorizeResult.Code,
            ["redirect_uri"] = authorizeResult.RedirectUri,
            ["code_verifier"] = "invalid-code-verifier"
        };

        var tokenResponse = await host.Client.PostAsync("/connect/token", new FormUrlEncodedContent(tokenRequestData));
        var body = await tokenResponse.Content.ReadAsStringAsync();

        tokenResponse.IsSuccessStatusCode.Should().BeFalse();
        body.Should().Contain("invalid_grant");
    }

    [Fact]
    public async Task AuthorizationCodePkce_WithRegularUserRequestingAdminScope_FiltersAdminScopeFromToken()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(
            host.Client,
            "user@example.com",
            "User123!",
            requestedScope: "openid profile email offline_access orders.read admin.full");

        authResult.Token.Scope.Should().NotBeNullOrWhiteSpace();
        var grantedScopes = authResult.Token.Scope!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        grantedScopes.Should().Contain("orders.read");
        grantedScopes.Should().NotContain("admin.full");
    }

    [Fact]
    public async Task UserInfo_WithoutAccessToken_IsUnauthorizedOrForbidden()
    {
        await using var host = await CreateHostAsync();

        var response = await host.Client.GetAsync("/connect/userinfo");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserInfo_WithMalformedAccessToken_IsUnauthorizedOrForbidden()
    {
        await using var host = await CreateHostAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserInfo_WithoutRolesScope_DoesNotReturnRolesClaim()
    {
        await using var host = await CreateHostAsync();
        var authResult = await RunAuthorizationCodeFlowAsync(host.Client, "user@example.com", "User123!");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token.AccessToken);

        var response = await host.Client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().NotContain("\"roles\"");
    }

    private async Task<TestHostContext> CreateHostAsync()
    {
        var databaseName = $"authserver_it_{Guid.NewGuid():N}";
        await postgresFixture.EnsureDatabaseExistsAsync(databaseName);

        var connectionString = postgresFixture.BuildConnectionString(databaseName);
        var factory = new CustomAuthServerFactory(connectionString);
        var client = factory.CreateHttpsClient(allowAutoRedirect: false);

        return new TestHostContext(factory, client, connectionString);
    }

    private async Task<AuthCodeFlowResult> RunAuthorizationCodeFlowAsync(
        HttpClient client,
        string email,
        string password,
        string requestedScope = "openid profile email offline_access orders.read")
    {
        var authorizeResult = await CompleteAuthorizationAndGetCodeAsync(client, email, password, requestedScope);

        var tokenRequestData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "web-admin",
            ["code"] = authorizeResult.Code,
            ["redirect_uri"] = authorizeResult.RedirectUri,
            ["code_verifier"] = authorizeResult.CodeVerifier
        };

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(tokenRequestData));
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: tokenBody);

        var token = JsonSerializer.Deserialize<TokenResponse>(tokenBody, _jsonOptions);
        token.Should().NotBeNull();

        return new AuthCodeFlowResult(authorizeResult.State, authorizeResult.StateFromCallback, token!);
    }

    private async Task<AuthorizationCodeChallengeResult> CompleteAuthorizationAndGetCodeAsync(
        HttpClient client,
        string email,
        string password,
        string requestedScope = "openid profile email offline_access orders.read")
    {
        var (codeVerifier, codeChallenge) = PkceHelper.Create();
        var state = Guid.NewGuid().ToString("N");
        var redirectUri = $"{CustomAuthServerFactory.ExternalClientUrl}/callback";

        var authorizeUrl = QueryHelpers.AddQueryString("/connect/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = "web-admin",
            ["redirect_uri"] = redirectUri,
            ["scope"] = requestedScope,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        });

        var authorizeResponse = await client.GetAsync(authorizeUrl);
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        authorizeResponse.Headers.Location.Should().NotBeNull();
        authorizeResponse.Headers.Location!.ToString().Should().Contain("/Account/Login");

        var loginPageResponse = await client.GetAsync(authorizeResponse.Headers.Location);
        loginPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var loginForm = await LoginPageParser.ParseAsync(loginHtml);

        var loginPostData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = loginForm.ReturnUrl ?? string.Empty,
            ["__RequestVerificationToken"] = loginForm.AntiForgeryToken
        };

        var loginPostResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginPostData));
        loginPostResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        loginPostResponse.Headers.Location.Should().NotBeNull();

        var authorizeAfterLoginResponse = await client.GetAsync(loginPostResponse.Headers.Location);
        authorizeAfterLoginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        authorizeAfterLoginResponse.Headers.Location.Should().NotBeNull();

        var callbackUri = authorizeAfterLoginResponse.Headers.Location!;
        callbackUri.IsAbsoluteUri.Should().BeTrue();
        callbackUri.GetLeftPart(UriPartial.Path).Should().Be(redirectUri);

        var code = QueryStringHelper.GetQueryValue(callbackUri, "code");
        var callbackState = QueryStringHelper.GetQueryValue(callbackUri, "state");

        code.Should().NotBeNullOrWhiteSpace();
        callbackState.Should().NotBeNullOrWhiteSpace();

        return new AuthorizationCodeChallengeResult(
            state,
            callbackState!,
            code!,
            codeVerifier,
            redirectUri);
    }

    private static async Task SetUserActiveAsync(string connectionString, string email, bool isActive)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var user = await db.Users.SingleAsync(u => u.Email == email);
        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    private static async Task SetUserRolesAsync(string connectionString, string email, params string[] roleNames)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var roleIds = await db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        roleIds.Should().HaveCount(roleNames.Length, because: "all requested test roles must exist");

        var existingUserRoles = db.UserRoles.Where(ur => ur.UserId == user.Id);
        db.UserRoles.RemoveRange(existingUserRoles);

        var newRoles = roleIds.Select(roleId => new AppUserRole
        {
            UserId = user.Id,
            RoleId = roleId
        });

        await db.UserRoles.AddRangeAsync(newRoles);
        await db.SaveChangesAsync();
    }

    private static string[] ParseScopes(TokenResponse token)
    {
        token.Scope.Should().NotBeNullOrWhiteSpace();
        return token.Scope!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed record AuthCodeFlowResult(string State, string StateFromCallback, TokenResponse Token);

    private sealed record AuthorizationCodeChallengeResult(
        string State,
        string StateFromCallback,
        string Code,
        string CodeVerifier,
        string RedirectUri);

    private sealed class TestHostContext(CustomAuthServerFactory factory, HttpClient client, string connectionString) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public string ConnectionString { get; } = connectionString;

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
