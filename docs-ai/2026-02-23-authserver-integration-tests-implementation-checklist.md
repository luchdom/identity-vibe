# AuthServer Integration Tests Implementation Checklist (.NET, Authentication Flow)

Date: 2026-02-23
Repo: `identity-vibe`
Related plan: `ai-analysis/2026-02-23-authserver-integration-tests-plan.md`
Scope: Backend-only integration tests for `AuthServer` authentication flow

## Confirmed Decisions

- [x] Test framework: `xUnit`
- [x] Assertion library: `FluentAssertions`
- [x] Integration host: `WebApplicationFactory`
- [x] Database strategy: `Testcontainers` + PostgreSQL
- [x] Mocking library (if needed): `NSubstitute`
- [x] Scope limited to auth flow (AuthServer only)
- [x] v1 includes full real login flow (`/connect/authorize` -> `/Account/Login` -> `/connect/token`)
- [x] Use test-only ephemeral OpenIddict signing/encryption keys in `IntegrationTest` environment

## Usage

- Use this checklist while implementing the test suite.
- Mark items complete as code/docs are added and verified.
- Add notes for tradeoffs or deviations.

## Status Snapshot (2026-02-23, after implementation)

Implemented and verified:

- AuthServer integration test project created and added to solution
- `WebApplicationFactory` + PostgreSQL Testcontainers infrastructure
- `IntegrationTest` environment with ephemeral OpenIddict keys
- Explicit content-root resolution for AuthServer views/appsettings files
- Real login page + anti-forgery + cookies + redirects + auth-code PKCE flow
- `userinfo`, refresh token, client credentials, and core negative cases
- Docs updates (`docs/TESTING.md`, `README.md`, `docs/DEVELOPMENT.md`, `docs/AGENTS.MD`)
- Local test run passes: `18/18`

Intentionally not implemented yet (left as follow-up items in this checklist):

- Expired token simulation
- CI workflow automation for integration tests

Newly implemented after initial pass:

- Missing-secret client credentials protocol error case
- Unsupported grant type protocol error case
- Invalid PKCE verifier auth-code exchange negative case
- Disabled-user refresh token invalid-grant case
- Unauthorized client scope request protocol error case
- Regular-user admin-scope filtering assertion
- UserInfo roles-claim absence assertion when roles scope not granted

## 0. Test Scope and Success Criteria

- [ ] Define initial auth integration test coverage set:
- [ ] AuthServer health/discovery
- [ ] Authorization Code + PKCE happy path
- [ ] `connect/userinfo` happy path
- [ ] Refresh token happy path + invalid token case
- [ ] Client credentials happy path + invalid client case(s)
- [ ] UserInfo unauthorized/invalid token cases
- [ ] Define "done" criteria for v1 test suite:
- [ ] Deterministic local run
- [ ] No external dependencies beyond Docker
- [ ] Docs added for running/writing integration tests

Notes:

- 

## 1. Create Test Project and Solution Wiring

### 1.1 Create test project

- [ ] Create project folder `tests/backend/AuthServer.IntegrationTests/`
- [ ] Create `AuthServer.IntegrationTests.csproj`
- [ ] Target `net8.0`
- [ ] Enable nullable + implicit usings

### 1.2 Add package references

- [ ] Add `Microsoft.NET.Test.Sdk`
- [ ] Add `xunit`
- [ ] Add `xunit.runner.visualstudio`
- [ ] Add `FluentAssertions`
- [ ] Add `Microsoft.AspNetCore.Mvc.Testing`
- [ ] Add `Microsoft.AspNetCore.TestHost` (if needed explicitly)
- [ ] Add `DotNet.Testcontainers`
- [ ] Add PostgreSQL Testcontainers package
- [ ] Add `NSubstitute` (optional but available)
- [ ] Add `AngleSharp` (or chosen HTML parser) for anti-forgery token parsing

### 1.3 Add project references

- [ ] Reference `src/backend/AuthServer/AuthServer.csproj`
- [ ] Reference `src/backend/Shared/Shared.csproj` only if needed by test helpers/assertions

### 1.4 Add to solution

- [ ] Add test project to `ArchZ.sln`
- [ ] (Optional) Organize under solution folder `tests`

Notes:

- 

## 2. Make AuthServer Test-Hostable

### 2.1 `Program` accessibility for `WebApplicationFactory`

- [ ] Update `src/backend/AuthServer/Program.cs` to add `public partial class Program { }`
- [ ] Confirm app still builds/runs after the change

### 2.2 Test environment assumptions

- [ ] Decide test environment name (e.g. `IntegrationTest`)
- [ ] Ensure startup logic remains compatible with factory-based hosting
- [ ] Keep seeding enabled for integration tests (unless a specific test requires override)
- [ ] Add test-only OpenIddict key strategy for `IntegrationTest` environment (ephemeral keys)
- [ ] Ensure production/development key behavior remains unchanged outside `IntegrationTest`

Notes:

- 

## 2.5 Pre-flight validation for login form anti-forgery (v1 blocker check)

- [ ] Verify rendered `GET /Account/Login` contains anti-forgery hidden field (`__RequestVerificationToken`)
- [ ] If missing, fix `src/backend/AuthServer/Views/Account/Login.cshtml` before implementing login-flow tests
- [ ] Add a regression assertion/test helper to fail clearly when anti-forgery token is absent

Notes:

- This is a v1 blocker because the real login flow is included in v1.

## 3. Build Test Infrastructure (Reusable)

### 3.1 PostgreSQL Testcontainers fixture

- [ ] Create `PostgresContainerFixture` (xUnit fixture)
- [ ] Implement `IAsyncLifetime`
- [ ] Start PostgreSQL container on initialization
- [ ] Expose connection string
- [ ] Stop/dispose container cleanly
- [ ] Add container wait strategy (readiness check)
- [ ] Configure sensible startup timeout

### 3.2 AuthServer test factory

- [ ] Create `CustomAuthServerFactory : WebApplicationFactory<Program>`
- [ ] Override configuration to inject test DB connection string
- [ ] Set environment to `IntegrationTest`
- [ ] Override URLs/base settings needed for auth flow tests
- [ ] Reduce noisy logging/telemetry exporters if they slow tests (without changing auth behavior)
- [ ] Ensure HTTPS base address strategy is handled (`UseHttpsRedirection` compatibility)

### 3.3 xUnit collection setup

- [ ] Create collection definition to share container fixture across auth integration tests
- [ ] Ensure tests in collection do not run in unsafe parallel mode against shared mutable DB state

### 3.4 Test isolation strategy

- [ ] Choose and implement DB isolation approach:
- [ ] Unique DB name per test class (recommended starting point)
- [ ] or reset DB between tests
- [ ] Ensure seeding runs against isolated DB
- [ ] Verify no cross-test state leakage

Notes:

- 

## 4. Add Test Helpers (Protocol / Form / Parsing)

### 4.1 PKCE helper

- [ ] Implement helper to generate `code_verifier`
- [ ] Implement helper to compute `S256` `code_challenge`
- [ ] Add deterministic test variant if needed

### 4.2 Anti-forgery/login form helper

- [ ] Implement helper to GET `/Account/Login`
- [ ] Parse anti-forgery token from HTML form
- [ ] Preserve cookies (cookie container / handler)
- [ ] Build login form POST payload with token + credentials + `ReturnUrl`

### 4.3 Redirect/authorize parsing helper

- [ ] Implement helper to parse callback redirect URI
- [ ] Extract `code` and `state`
- [ ] Assert expected redirect host/path

### 4.4 Response DTOs / helpers

- [ ] Add token response DTO (`access_token`, `refresh_token`, etc.)
- [ ] Add userinfo response DTO (or use dynamic/JSON)
- [ ] Add form URL-encoded request builder helpers

Notes:

- 

## 5. Implement Core Integration Tests (Happy Paths First)

### 5.1 Health + discovery smoke tests

- [ ] `GET /health` returns `200`
- [ ] `GET /.well-known/openid-configuration` returns `200`
- [ ] Assert discovery includes expected endpoints (`authorize`, `token`, `userinfo`, `logout`)

### 5.2 Authorization Code + PKCE happy path (user)

- [ ] Build authorize request for `web-admin` client with PKCE
- [ ] Assert challenge/redirect to login page
- [ ] Submit real login form with anti-forgery token + seeded user credentials (`user@example.com` / `User123!`)
- [ ] Follow redirect back through authorize endpoint
- [ ] Capture callback redirect with `code` + `state`
- [ ] Exchange code at `/connect/token`
- [ ] Assert token response contains:
- [ ] `access_token`
- [ ] `refresh_token` (when `offline_access` requested)
- [ ] `token_type=bearer`
- [ ] Assert returned `state` matches original request

### 5.3 UserInfo happy path

- [ ] Call `/connect/userinfo` with access token from PKCE flow
- [ ] Assert `sub`, `name`, `email`
- [ ] Assert role/roles behavior according to granted scopes (document actual behavior)

### 5.4 Refresh token happy path

- [ ] Use refresh token from auth code flow
- [ ] POST `/connect/token` with `grant_type=refresh_token` and `client_id=web-admin`
- [ ] Assert new access token returned
- [ ] Assert response shape is valid

### 5.5 Client credentials happy path (`orders` service client)

- [ ] POST `/connect/token` with `grant_type=client_credentials`
- [ ] Use `client_id=orders`, `client_secret=orders-secret`
- [ ] Request valid scopes (`orders.read orders.write orders.manage`)
- [ ] Assert token is issued
- [ ] Assert unauthorized scopes are not granted (if extra scopes requested in test)

Notes:

- 

## 6. Implement Negative/Failure Auth Tests

### 6.1 Token endpoint negative cases

- [ ] Invalid client secret (client credentials grant) -> `invalid_client`
- [ ] Missing client secret for confidential client -> `invalid_client`
- [ ] Unsupported grant type -> `unsupported_grant_type`
- [ ] Invalid refresh token -> `invalid_grant`

### 6.2 UserInfo negative cases

- [ ] No access token -> unauthorized/challenge
- [ ] Malformed token -> unauthorized/challenge
- [ ] Expired/invalid token path (if feasible to simulate reliably)

### 6.3 Authorization flow negative cases (optional v1.1)

- [ ] Invalid PKCE verifier on token exchange -> invalid grant
- [ ] Invalid redirect URI -> authorization/token failure as expected
- [ ] Invalid client id in authorize request -> proper error path

Notes:

- 

## 7. Test Quality / Reliability Hardening

- [ ] Ensure tests are deterministic (no sleeps / timing hacks)
- [ ] Ensure redirects are handled intentionally (not auto-followed when assertions need redirect URLs)
- [ ] Ensure cookie handling persists across login/authorize requests
- [ ] Ensure tests can run repeatedly without manual cleanup
- [ ] Tune container startup timeout and logging for debugging failures
- [ ] Add helper diagnostics (dump response body on failure for token/login flow)

Notes:

- 

## 8. Execution Commands and Dev Workflow

- [ ] Add `dotnet test` command examples to docs
- [ ] Add filtered test run examples (`--filter`) to docs
- [ ] Verify local run from repo root
- [ ] Verify local run from test project directory

Notes:

- 

## 9. Documentation Updates (Best Practices for Integration Tests)

### 9.1 New testing doc

- [ ] Create `docs/TESTING.md`
- [ ] Document purpose of integration tests in this repo
- [ ] Document stack used (`xUnit`, `FluentAssertions`, `WebApplicationFactory`, `Testcontainers`)
- [ ] Document Docker prerequisite
- [ ] Document how to run tests locally
- [ ] Document common pitfalls:
- [ ] HTTPS redirection in tests
- [ ] anti-forgery token handling for login form
- [ ] redirect URI mismatch with seeded client
- [ ] container startup timing issues

### 9.2 Update existing docs

- [ ] Update `docs/DEVELOPMENT.md` with a “Backend Integration Tests” section + command examples
- [ ] Update `README.md` with a short section linking to `docs/TESTING.md`
- [ ] Update `docs/AGENTS.MD` with guidance to prefer real auth-flow integration tests for auth changes

### 9.3 Best-practice guidance content (ensure included)

- [ ] What should not be mocked (Identity/OpenIddict/DbContext/auth middleware)
- [ ] When `NSubstitute` is appropriate (external dependencies only)
- [ ] Test naming conventions
- [ ] Test isolation strategy
- [ ] How to avoid flaky tests

Notes:

- 

## 10. Validation / Done Checklist

### 10.1 Test suite validation

- [ ] All v1 auth integration tests pass locally
- [ ] Full real login flow test passes consistently on repeat runs
- [ ] Tests pass on repeat run (no hidden DB state leakage)
- [ ] Test runtime is acceptable (document approximate duration)
- [ ] Failures provide actionable diagnostics

### 10.2 Code/structure validation

- [ ] `AuthServer` still runs normally after `Program` test-hostability change
- [ ] Test project builds cleanly
- [ ] No production code paths are altered beyond test-hostability/config hooks

### 10.3 Docs validation

- [ ] `docs/TESTING.md` is accurate and runnable
- [ ] `README.md` and `docs/DEVELOPMENT.md` references are correct

Notes:

- 

## 11. Optional Follow-Ups (Later)

- [ ] Add integration tests for logout flow behavior
- [ ] Add admin-user PKCE flow variant
- [ ] Add token introspection endpoint tests (`/connect/introspect`)
- [ ] Add CI workflow to run integration tests on PRs (Docker-enabled runner)
- [ ] Add shared test utilities package if future backend services adopt same pattern
- [ ] Add expired-token simulation test (only if implemented deterministically)

## Implementation Log

- [x] Implementation started
- [x] Test project created
- [x] Test infrastructure complete
- [x] Core happy-path tests complete
- [x] Negative tests complete
- [x] Docs updates complete
- [x] Final validation complete

Change notes:

- Added `tests/backend/AuthServer.IntegrationTests` with 11 passing auth-flow integration tests.
- Expanded auth-flow suite to 18 passing tests with additional negative/protocol cases.
- Added test-host support in `src/backend/AuthServer/Program.cs` (`IntegrationTest` env + ephemeral OpenIddict keys + `public partial class Program`).
- Fixed anti-forgery token rendering in login/register Razor views to support real form POST integration tests.
- Added repo testing guidance in `docs/TESTING.md` and linked from `README.md` / `docs/DEVELOPMENT.md` / `docs/AGENTS.MD`.
