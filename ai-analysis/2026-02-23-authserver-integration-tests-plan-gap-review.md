# AuthServer Integration Test Plan Review (Gaps / Open Questions)

Date: 2026-02-23
Related:
- `ai-analysis/2026-02-23-authserver-integration-tests-plan.md`
- `ai-analysis/2026-02-23-authserver-integration-tests-implementation-checklist.md`

## Summary

The proposed approach is strong and appropriate for this repo:

- `xUnit` + `FluentAssertions`
- `WebApplicationFactory`
- `Testcontainers` PostgreSQL
- real Identity/OpenIddict/EF pipeline (minimal mocking)

This aligns well with the current `AuthServer` architecture and is the right level of fidelity for auth-flow regression protection.

## What Makes Sense (Validated)

- Testing `AuthServer` auth flow independently of frontend/Orders is a good boundary.
- Using PostgreSQL in tests is justified because the app is configured around `Npgsql` and startup seeding.
- Covering auth-code + PKCE, userinfo, refresh, and client-credentials maps directly to implemented endpoints in `AuthorizationController`.
- Including docs updates (`docs/TESTING.md` + references) is the right long-term move.
- `NSubstitute` as optional-only is correct for integration tests.

## Gaps / Risks to Address Before or During Implementation

### 1. Login anti-forgery token may be missing in the current Razor form (high risk)

`src/backend/AuthServer/Views/Account/Login.cshtml` uses:

- `<form method="post">`

and `AccountController.Login` POST has:

- `[ValidateAntiForgeryToken]`

Potential issue:

- The form may not render an anti-forgery hidden field unless `@Html.AntiForgeryToken()` (or tag-helper-driven antiforgery injection) is actually present/active.

Impact:

- The planned auth-code + login integration test may fail immediately on login POST.

Recommendation:

- First verify the rendered login page contains `__RequestVerificationToken`.
- If not present, fix the view (production bug fix) before writing tests around it.

### 2. `WebApplicationFactory` content root / config file loading for AuthServer (high risk)

`Program.cs` explicitly loads:

- `appsettings.Scopes.json`
- `appsettings.Clients.json`

and MVC login tests require:

- `Views/Account/Login.cshtml`

Risk:

- If `WebApplicationFactory` content root resolution points to the test project instead of `src/backend/AuthServer`, startup or view rendering will break.

Recommendation:

- Explicitly set content root in the custom factory to the AuthServer project folder.
- Treat this as a required infrastructure task, not optional.

### 3. OpenIddict dev certificates may be brittle in test environments (medium/high risk)

`AuthServer` uses:

- `AddDevelopmentEncryptionCertificate()`
- `AddDevelopmentSigningCertificate()`

Risk:

- These can be flaky in CI/headless/local environments depending on cert store/profile availability and parallelism.

Recommendation:

- Add a test-only path (e.g. `IntegrationTest` env) to use ephemeral signing/encryption keys for integration tests.
- If you do this, document it clearly as a test-hosting concern, not a production behavior change.

### 4. HTTPS redirection + TestServer behavior needs explicit handling (medium risk)

`Program.cs` includes:

- `app.UseHttpsRedirection();`

Risk:

- Auth/login/token requests may redirect unexpectedly in tests if the client base address is HTTP and redirects are auto-followed.

Recommendation:

- In factory/client setup, use `https://localhost` base address.
- Disable auto-redirect for flow tests that need to inspect `Location` headers.
- Keep a second client configuration for convenience calls (e.g. health endpoint).

### 5. DB isolation details are still under-specified (medium risk)

The plan says "unique DB per test class or reset DB", which is correct, but implementation detail is important with PostgreSQL containers:

- Creating a unique database per class requires an admin connection + DB creation step.
- Seeding runs at startup and can be slow if repeated excessively.

Recommendation:

- Pick one upfront for v1:
- shared container
- unique database per test class
- one factory per test class

This is the best tradeoff for correctness vs speed in this repo.

### 6. Parallelization strategy should be explicit at assembly level (medium risk)

OpenIddict cert generation + shared container + seeded state can produce race conditions.

Recommendation:

- Disable test parallelization for this test assembly (or tightly control collection-level parallelism).
- Add this to the checklist/docs explicitly.

### 7. A few high-value auth cases are missing from the proposed core list (quality gap)

The proposed cases are good, but these are worth adding because the code explicitly contains logic for them:

- PKCE invalid verifier (`/connect/token` auth code exchange should fail)
- Refresh token for disabled user (`AuthorizationController.Token()` has explicit check)
- Scope filtering behavior:
- user requests admin scopes but only receives allowed scopes
- client credentials requests unauthorized scopes and they are filtered out
- `userinfo` roles behavior with/without `roles` scope (current code branches on roles scope)

These do not need to be all in v1, but should be listed as planned tests (v1/v1.1).

### 8. Test helper design should explicitly support cookie + redirect choreography (implementation gap)

Auth-code + login flow requires:

- cookie persistence across requests
- manual redirect handling
- parsing multiple redirect hops

Recommendation:

- Make this an explicit infrastructure requirement (not just helper nicety).
- Prefer a dedicated helper around `HttpClient` with configurable redirect behavior.

### 9. Documentation scope is good, but should include a repo-specific template example (quality improvement)

To satisfy "how to make best integration tests", docs should include at least one concrete example:

- `CustomAuthServerFactory` skeleton
- test fixture pattern
- one sample auth integration test method

This makes the guidance actionable for future contributors.

## Open Questions (Need Confirmation Before Implementation, if you want to lock scope)

1. Should the first version include **real `/Account/Login` form POST** (anti-forgery + HTML parsing), or do you want v1 to start with token endpoint + client credentials/refresh only and add login UI flow immediately after? [RESOLVED]

- Include the full real login form flow in v1.

2. Are you okay with a small test-only production code change in `AuthServer` for **ephemeral OpenIddict keys in `IntegrationTest` environment** (recommended for test stability)? [RESOLVED]

- Yes. Use ephemeral keys in `IntegrationTest` for stability.

## Recommendation

Proceed with the proposed approach, but explicitly add these two tasks before coding begins:

1. Verify/fix anti-forgery token rendering on `Login.cshtml`
2. Implement test-host key strategy (`ephemeral keys` for `IntegrationTest`)

## Implementation Outcome (2026-02-23)

Validated/resolved during implementation:

- Anti-forgery concern was real and was fixed by adding `@Html.AntiForgeryToken()` to login/register views.
- Explicit `WebApplicationFactory` content root was implemented and required for Razor views + `appsettings.*.json`.
- `IntegrationTest` environment with ephemeral OpenIddict signing/encryption keys was added and is working.
- HTTPS test client base address + manual redirect handling were implemented and validated by the full login flow test.
- Parallelization is disabled at assembly level.
- Shared PostgreSQL container + isolated DB per test method is implemented and deterministic.

Remaining planned follow-ups (not blockers):

- Additional negative cases (`unsupported_grant_type`, missing client secret, invalid PKCE verifier)
- Scope filtering and roles-scope assertions
- Expired token simulation path
