using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuthServer.IntegrationTests.Contracts;
using AuthServer.IntegrationTests.Helpers;
using AuthServer.IntegrationTests.Infrastructure;
using FluentAssertions;
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

    private async Task<TestHostContext> CreateHostAsync()
    {
        var databaseName = $"authserver_it_{Guid.NewGuid():N}";
        await postgresFixture.EnsureDatabaseExistsAsync(databaseName);

        var factory = new CustomAuthServerFactory(postgresFixture.BuildConnectionString(databaseName));
        var client = factory.CreateHttpsClient(allowAutoRedirect: false);

        return new TestHostContext(factory, client);
    }

    private async Task<AuthCodeFlowResult> RunAuthorizationCodeFlowAsync(HttpClient client, string email, string password)
    {
        var (codeVerifier, codeChallenge) = PkceHelper.Create();
        var state = Guid.NewGuid().ToString("N");
        var redirectUri = $"{CustomAuthServerFactory.ExternalClientUrl}/callback";

        var authorizeUrl = QueryHelpers.AddQueryString("/connect/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = "web-admin",
            ["redirect_uri"] = redirectUri,
            ["scope"] = "openid profile email offline_access orders.read",
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

        var tokenRequestData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "web-admin",
            ["code"] = code!,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(tokenRequestData));
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: tokenBody);

        var token = JsonSerializer.Deserialize<TokenResponse>(tokenBody, _jsonOptions);
        token.Should().NotBeNull();

        return new AuthCodeFlowResult(state, callbackState!, token!);
    }

    private sealed record AuthCodeFlowResult(string State, string StateFromCallback, TokenResponse Token);

    private sealed class TestHostContext(CustomAuthServerFactory factory, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

