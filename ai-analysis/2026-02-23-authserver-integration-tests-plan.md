# AuthServer Integration Test Plan (.NET Backend, Authentication Flow Only)

Date: 2026-02-23
Repo: `identity-vibe`
Scope: Backend-only integration tests for `AuthServer` authentication flow (OAuth2/OIDC + login + userinfo + refresh)

## Goal

Add a reliable integration test suite for `src/backend/AuthServer` that validates the real authentication flow end-to-end against the application pipeline (controllers, middleware, Identity, OpenIddict, EF Core, seeding), with minimal mocking.

## Recommended Stack (Current .NET Best-Practice Direction)

- `xUnit` for test runner (`xunit`, `xunit.runner.visualstudio`)
- `Microsoft.AspNetCore.Mvc.Testing` for `WebApplicationFactory`
- `Microsoft.AspNetCore.TestHost` (transitively useful / explicit if needed)
- `DotNet.Testcontainers` (PostgreSQL module) for production-like DB fidelity
- `NSubstitute` only if a true external seam must be replaced (prefer real components for auth flow)
- `FluentAssertions` for readable assertions
- `FluentValidation`: not required for integration tests unless validating request validator behavior specifically
- `AngleSharp` (or equivalent HTML parser) to extract anti-forgery token from `/Account/Login` form

## Why This Approach

- `AuthServer` uses `Identity + OpenIddict + EF Core + PostgreSQL` and seeds clients/users/scopes at startup.
- Integration tests should exercise the real auth pipeline, not mocked services.
- `Testcontainers + PostgreSQL` is safer than in-memory/SQLite for auth/storage behavior because the app is configured around `Npgsql` in `Program.cs`.

## Target Test Scope (Authentication Flow)

Focus on these flows only (backend integration):

1. Health and basic availability
2. Authorization Code + PKCE (through real login UI + anti-forgery + token exchange)
3. `connect/userinfo` with access token
4. Refresh token flow
5. Client credentials flow (service client)
6. Negative auth cases (invalid client, invalid refresh token, invalid/expired token)

Out of scope for this test suite (initially):

- `Orders` API integration
- Frontend integration/e2e
- Performance/load tests
- Full browser automation

## Current Repo Constraints / Observations (Relevant to Plan)

- `AuthServer` uses top-level `Program.cs` and `app.Run();`
- Startup seeds DB via `DatabaseSeeder` automatically
- `AccountController` login POST uses `[ValidateAntiForgeryToken]`
- OAuth2/OIDC endpoints are implemented in `AuthorizationController`
- Password grant is **not enabled** (good to avoid in tests)
- Userinfo endpoint is `/connect/userinfo` (already standardized in repo changes)

## Implementation Plan (Detailed Tasks)

## 1. Create a dedicated backend integration test project

### 1.1 Project structure

Recommended path:

- `tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`

Alternative (if you prefer under `src`):

- `src/tests/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`

Recommendation: use `tests/backend/...` to keep test assets separated from production code.

### 1.2 Add package references

Required:

- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `Microsoft.AspNetCore.Mvc.Testing`
- `DotNet.Testcontainers`
- `DotNet.Testcontainers.PostgreSql` (or current PostgreSQL package variant)
- `NSubstitute` (optional but okay to include up front)

Recommended:

- `FluentAssertions` (if approved)
- `AngleSharp` (for anti-forgery token parsing)

### 1.3 Solution wiring

- Add the new test project to `ArchZ.sln`
- Optionally create a solution folder `tests`

## 2. Make AuthServer test-hostable with `WebApplicationFactory`

### 2.1 Expose `Program` for `WebApplicationFactory`

In `src/backend/AuthServer/Program.cs`, add at end:

- `public partial class Program { }`

This is the standard pattern for top-level statements in ASP.NET Core integration tests.

### 2.2 Keep app startup behavior stable in tests

Do not split startup logic unnecessarily unless needed.

Prefer:

- override config/services in `WebApplicationFactory`
- preserve real middleware/auth pipeline
- keep seeding enabled (use test DB isolation instead of mocking seeding away)

## 3. Build reusable integration test infrastructure

## 3.1 `CustomAuthServerFactory : WebApplicationFactory<Program>`

Responsibilities:

- set test environment (e.g. `IntegrationTest`)
- override configuration values (`ConnectionStrings__DefaultConnection`, URLs, logging)
- point AuthServer to test PostgreSQL container connection string
- reduce noisy telemetry/logging exporters in tests if needed
- configure `ClientOptions.BaseAddress` to `https://localhost` or use a client created with HTTPS base (important due `UseHttpsRedirection`)

Important detail:

- `AuthServer` calls `app.UseHttpsRedirection()`
- if test client starts on HTTP base address, requests may redirect and break token/login flow expectations
- best practice: create client with HTTPS base address for auth tests

## 3.2 `PostgresContainerFixture` (xUnit collection fixture)

Use `IAsyncLifetime` to:

- start one PostgreSQL Testcontainer per test collection (or per test class)
- expose connection string to factory
- stop/cleanup container after tests

Recommended strategy:

- one container per collection
- one **database/schema reset strategy** per test (see below)

## 3.3 Test DB isolation strategy (important)

Options:

1. Recreate DB per test class (simpler, slower)
2. Recreate DB per test (cleanest, slower)
3. Shared DB + reset tables between tests (fastest, more work)

Recommended initial implementation:

- Shared PostgreSQL container
- Unique database name per test class or test method
- New `WebApplicationFactory` per test class using that DB

This avoids complex reset logic while keeping tests deterministic.

## 3.4 Test helpers

Create helper classes for:

- PKCE generation (`code_verifier`, `code_challenge`)
- HTML parsing of anti-forgery token from `/Account/Login`
- Form post helpers with cookies
- Redirect URI parsing (extract auth `code`, `state`)
- Token response parsing (DTOs)

## 4. Implement authentication integration tests (priority order)

## 4.1 Smoke / availability

- `GET /health` returns `200`
- `GET /.well-known/openid-configuration` returns `200` and expected endpoints

Purpose:

- quick failure signal before deep flow tests

## 4.2 Authorization Code + PKCE end-to-end (core test)

### Happy path (user login + token exchange)

Test steps:

1. Build PKCE values (`code_verifier`, `code_challenge`)
2. Request `GET /connect/authorize` with:
- `client_id=web-admin`
- `response_type=code`
- `redirect_uri` matching seeded client
- requested scopes (e.g. `openid profile email offline_access orders.read`)
- `code_challenge`, `code_challenge_method=S256`
- `state`
3. Assert redirect/challenge to `/Account/Login`
4. `GET /Account/Login` page and parse anti-forgery token + cookies
5. `POST /Account/Login` with seeded user credentials (`user@example.com` / `User123!`) + anti-forgery
6. Follow redirect back into `/connect/authorize`
7. Capture final redirect to client callback URL and extract `code` + `state`
8. `POST /connect/token` with authorization code + `code_verifier`
9. Assert token response:
- `access_token` present
- `refresh_token` present (if `offline_access` requested)
- `token_type=bearer`

Assertions to include:

- returned `state` matches request
- no unexpected redirect loops
- token response contains expected scopes

## 4.3 UserInfo endpoint

### Happy path

- `GET /connect/userinfo` with bearer access token from auth code flow
- Assert `sub`, `name`, `email` exist
- If roles scope is granted/supported, assert roles claim behavior

Note:

- Current implementation may not grant `roles` scope unless explicitly included in allowed/filtered scopes. Plan to assert current intended behavior (and document if roles scope is excluded).

## 4.4 Refresh token flow

### Happy path

- Use `refresh_token` returned by auth code flow
- `POST /connect/token` with `grant_type=refresh_token`, `client_id=web-admin`
- Assert new `access_token` returned
- Optionally assert refresh token rotation behavior (if enabled / current behavior)

### Negative path

- Invalid refresh token -> `invalid_grant`

## 4.5 Client credentials flow (service auth)

### Happy path

- `POST /connect/token` with `grant_type=client_credentials`
- `client_id=orders`, `client_secret=orders-secret`
- Request valid scopes (`orders.read`, etc.)
- Assert token issued and scope filtered correctly

### Negative paths

- missing client secret for confidential client -> `invalid_client`
- invalid secret -> `invalid_client`
- invalid scope request -> token issued without unauthorized scopes (assert filtering)

## 4.6 Invalid / unauthorized userinfo

- `GET /connect/userinfo` without token -> unauthorized/challenge
- `GET /connect/userinfo` with malformed token -> unauthorized

## 5. Test organization and naming conventions (best practices)

Recommended folder layout:

- `tests/backend/AuthServer.IntegrationTests/Infrastructure/`
- `tests/backend/AuthServer.IntegrationTests/Helpers/`
- `tests/backend/AuthServer.IntegrationTests/Flows/`
- `tests/backend/AuthServer.IntegrationTests/Contracts/`

Naming examples:

- `AuthorizationCodePkceFlowTests`
- `RefreshTokenFlowTests`
- `ClientCredentialsFlowTests`
- `UserInfoEndpointTests`

Test method naming:

- `Method_Scenario_ExpectedResult`
- Example: `Token_WithRefreshTokenGrantAndValidToken_ReturnsNewAccessToken`

## 6. Mocking policy (NSubstitute)

Default rule:

- **Do not mock** Identity, OpenIddict managers, DbContext, or auth middleware for integration tests.

Use `NSubstitute` only if:

- a true outbound dependency is introduced later (email sender, external audit sink, webhook publisher)
- the dependency is non-deterministic / slow / flaky and outside auth-core verification

If used:

- substitute at DI registration override in `CustomAuthServerFactory`
- keep tests focused on auth behavior, not substitute call counts

## 7. Assertions / validation libraries guidance

## 7.1 xUnit

- Use xUnit for all integration tests and fixtures
- Use collection fixtures to avoid container startup per test

## 7.2 FluentValidation (user-requested)

FluentValidation is typically for request/domain validation rules, not assertions.

Plan recommendation:

- Do **not** add FluentValidation just for integration tests unless you are also introducing validators and want validator-specific tests.

If your intent was assertion fluency, use:

- `FluentAssertions` (recommended)

## 8. Test execution and developer ergonomics

## 8.1 Commands

- `dotnet test` for full test run
- `dotnet test --filter "FullyQualifiedName~AuthorizationCodePkceFlowTests"` for focused debugging

## 8.2 Stability practices

- Avoid sleeps/time-based waits; rely on direct responses/redirects
- Use deterministic test inputs (`state`, `redirect_uri`, scopes)
- Keep tests independent (isolated DB)
- Limit assertions to stable protocol fields (avoid brittle snapshot assertions on full JWT contents)

## 8.3 CI readiness (future)

- Document Docker requirement if using Testcontainers
- Add a test profile later only after tests are stable locally

## 9. Documentation updates (required by request)

Update docs to explain both how to run tests and how to write good integration tests in this repo.

## 9.1 Add/expand testing guidance

Recommended file options:

1. Add new `docs/TESTING.md` (preferred)
2. Also add a short entry in `docs/DEVELOPMENT.md` linking to it

## 9.2 Content to include in docs (best practices)

### What integration tests are for (in this repo)

- Validate real ASP.NET Core middleware + auth endpoints + database + seeding
- Catch wiring/config regressions (OpenIddict, Identity, CORS/config where applicable)

### What not to mock

- Identity/OpenIddict/DbContext/auth middleware

### When to use `NSubstitute`

- only for true external dependencies

### Test structure standards

- Fixture pattern (`WebApplicationFactory` + container fixture)
- Naming conventions
- Data isolation strategy
- Avoiding flaky tests

### Running tests locally

- Docker/Testcontainers prerequisite
- `dotnet test` commands
- how to run a single test class

### Common pitfalls

- `UseHttpsRedirection` causing redirects in test clients
- anti-forgery token handling for `/Account/Login`
- redirect URI mismatch with seeded `web-admin` client
- container startup latency/timeouts

## 9.3 Update project docs references

- `docs/DEVELOPMENT.md`: add test section and command examples
- `README.md`: add short "Backend integration tests" section
- `docs/AGENTS.MD`: add rule/note about using real auth flow integration tests for auth changes

## 10. Suggested Implementation Sequence (for later coding)

1. Create test project + packages + solution entry
2. Add `public partial class Program` to `AuthServer`
3. Implement Testcontainers PostgreSQL fixture
4. Implement `CustomAuthServerFactory`
5. Add helpers (PKCE + anti-forgery + redirect parsing)
6. Implement auth code + PKCE happy path test
7. Implement userinfo test
8. Implement refresh token tests
9. Implement client credentials tests
10. Add negative-path tests
11. Add docs (`docs/TESTING.md`, updates to README/docs)
12. Stabilize test runtime and naming conventions

## Open Questions / Confirmations (one-by-one if needed)

1. Assertion library choice [RESOLVED]
- Use `FluentAssertions` for assertions.

2. Infrastructure dependency [RESOLVED]
- Docker locally is allowed/required.
- Use Testcontainers with PostgreSQL for AuthServer integration tests.

3. v1 scope includes full real login flow [RESOLVED]
- Include the full `/connect/authorize` -> `/Account/Login` (anti-forgery + cookies + redirects) -> `/connect/token` flow in v1.

4. Test-host key strategy [RESOLVED]
- Use a small test-only production code change to support ephemeral OpenIddict signing/encryption keys in `IntegrationTest` environment for stability.
