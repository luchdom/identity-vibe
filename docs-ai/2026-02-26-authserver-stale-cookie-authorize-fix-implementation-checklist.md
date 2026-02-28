# AuthServer Stale Cookie Authorize Fix Implementation Checklist

Date: 2026-02-26
Status: Tasked
Related:
- `ai-analysis/2026-02-26-authserver-stale-cookie-authorize-fix-plan.md`

## Goal

Prevent `/connect/authorize` from throwing `InvalidOperationException` when the Identity app cookie is valid but the referenced user is missing/inactive (common after local DB reset).

## Tasks

### A. Controller fix

- [x] Update `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- [x] Replace "user not found" throw in `Authorize()` with stale-session recovery:
  - sign out Identity cookie
  - challenge login again (preserve original authorize request redirect URI)
- [x] Apply same behavior when `user.IsActive == false`
- [x] Add warning logs for stale/missing/inactive cookie user cases
- [x] Refactor duplicate login challenge creation into helper (or equivalent)

### B. Integration tests

- [x] Extend `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`
- [x] Add regression test: stale cookie + deleted user => redirects to login (no 500)
- [x] Add regression test: stale cookie + inactive user => redirects to login (no 500)

### C. Validation

- [x] `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj` (or targeted filter)
- [ ] Manual smoke (optional): reset Auth DB, hit login/authorize with old browser cookie and confirm re-login instead of 500

## Implementation log

- Implemented stale-session recovery in `AuthorizationController.Authorize()` for:
  - authenticated cookie principal with missing user (deleted/reset DB user)
  - authenticated cookie principal with inactive user (`IsActive == false`)
- Behavior now signs out the Identity cookie and re-challenges login instead of throwing `InvalidOperationException`.
- Added warning logging for both stale/missing-user and inactive-user cases.
- Added `BuildLoginChallenge()` helper to avoid duplicating challenge/redirect URI construction.
- Added AuthServer integration regression tests:
  - stale cookie + deleted user => redirect to `/Account/Login`
  - stale cookie + inactive user => redirect to `/Account/Login`
- Patched `tests/backend/AuthServer.IntegrationTests/Infrastructure/CustomAuthServerFactory.cs` content-root resolution to work when tests are run with alternate `OutDir`.
- Validation executed:
  - `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj --filter "FullyQualifiedName~Authorize_WithStaleCookie" -p:OutDir=C:\\temp\\auth-stale-cookie-tests\\` ✅ (`2/2`)
- Note:
  - Standalone `dotnet build` using custom `BaseIntermediateOutputPath` hit the known duplicate assembly-attribute issue (pre-existing quirk with alternate intermediate paths), but the integration test run compiled and passed the affected code paths.
