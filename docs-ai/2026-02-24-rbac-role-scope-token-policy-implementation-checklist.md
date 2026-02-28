# RBAC Role->Scope->Token->Policy Implementation Checklist

Date: 2026-02-24
Status: Tasked (ready for implementation)
Related plan: `ai-analysis/2026-02-24-rbac-role-scope-token-policy-plan.md`

## Objective
Implement RBAC so that:
- Users have roles
- Roles map to scopes (permissions)
- PKCE login grants role-derived permission scopes (not limited by requested business scopes), while standard protocol scopes remain request-driven
- APIs (Orders) authorize routes by scope-based policies
- Refresh token grant re-evaluates permissions after role changes

## Scope of work
- `AuthServer` (role -> scopes -> token issuance / refresh)
- `Orders` (scope-based route authorization)
- `Shared` authorization/claims helpers (scope parsing + internal permission helpers)
- Tests (AuthServer integration, Orders auth tests, helper unit tests)
- Docs (architecture + testing + extension guidance)

## Assumptions (confirmed / default)
- Keep PKCE flow for user login (`web-admin` public client)
- Keep direct API calls (no gateway)
- Orders route authorization should be scope-first (no role bypass)
- Roles may remain available for UI/userinfo, but resource APIs should rely on scopes

## Implementation Checklist

### 0. AuthorizationController refactor (include in this RBAC work)
Scope decision (confirmed):
- Include `Token()` grant-branch refactor in this RBAC pass
- Defer `Authorize()` endpoint extraction unless it remains trivially low-risk after RBAC changes

#### 0.1 Add auth grant flow service and outcome model
Files (new):
- `src/backend/AuthServer/Services/Interfaces/IAuthorizationGrantService.cs`
- `src/backend/AuthServer/Services/AuthorizationGrantService.cs`
- `src/backend/AuthServer/Models/Authorization/GrantFlowResult.cs` (or similar)

Tasks:
- Introduce a dedicated service for token grant-type handling logic.
- Introduce a dedicated outcome type for OpenIddict token endpoint translation, e.g.:
  - success with `ClaimsPrincipal`
  - protocol forbid with `error` and `error_description`
- Avoid using `Shared.Common.Result<T>` directly for final controller mapping, because token endpoint outputs are OpenIddict protocol actions (`SignIn`/`Forbid`) rather than normal REST `IActionResult` patterns.
- It is acceptable to use `Result<T>` internally inside the service for sub-operations if helpful.

Acceptance checks:
- Grant flow service can express all current token branch outcomes without losing protocol details.

#### 0.2 Refactor `AuthorizationController.Token()` to delegate by grant type
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Tasks:
- Replace inline grant-type branch bodies with small delegating methods, e.g.:
  - `HandleAuthorizationCodeGrantAsync(request)`
  - `HandleClientCredentialsGrantAsync(request)`
  - `HandleRefreshTokenGrantAsync(request)`
- Or delegate directly to `IAuthorizationGrantService` methods and map returned outcomes.
- Keep controller responsibilities limited to:
  - reading OpenIddict request
  - dispatching by grant type
  - mapping `GrantFlowResult` -> `SignIn(...)` / `Forbid(...)`
- Keep unsupported-grant response in controller or centralize in mapper helper (either is fine).

Acceptance checks:
- `Token()` becomes materially smaller/readable and behavior stays the same (before RBAC logic changes).

#### 0.3 (Deferred follow-up / optional) Refactor `Authorize()` endpoint internals incrementally
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Services/AuthorizationGrantService.cs` (or a separate `AuthorizationCodeFlowService`)

Tasks:
- Extract portions of `Authorize()` that are not HTTP concerns:
  - authenticated user retrieval
  - application lookup
  - principal creation
  - authorization record find/create
- Keep `Challenge(...)` redirect handling in controller (HTTP concern).
- Do not over-refactor `userinfo`/`introspect`/`logout` in the same pass unless it remains low-risk.

Acceptance checks:
- `Authorize()` remains behaviorally identical and integration tests pass.

#### 0.4 Add tests to protect refactor behavior (no regression)
Files:
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`

Tasks:
- Re-run and keep existing auth integration tests passing after refactor.
- Add/adjust tests only if error descriptions/codes change unexpectedly.
- Ensure protocol errors still match current behavior (e.g., `invalid_grant`, `invalid_request`, `unsupported_grant_type`).

Acceptance checks:
- Existing auth integration suite passes unchanged or with intentional assertion updates only.

### A. Shared authorization/claims hardening (do first)

#### A1. Fix scope claim parsing robustness
Files:
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`

Tasks:
- Update `GetScopes()` to support both claim formats:
  - repeated claims (`scope=orders.read`, `scope=orders.write`)
  - space-separated claim (`scope="orders.read orders.write"`)
- Deduplicate returned scopes after splitting.
- Update `HasScope(string scope)` to use normalized parsed scopes (exact token match only).
- Keep `HasAnyScope(...)` behavior unchanged but ensure it uses the corrected `HasScope()`.

Acceptance checks:
- `HasScope("orders.read")` works for both repeated and space-separated formats.
- `HasScope("orders.read")` does not match `orders.read.all`.

#### A2. Align internal admin helper with permission scopes
Files:
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- `src/backend/Shared/Extensions/HttpContextExtensions.cs` (no API change expected, validate usage)

Tasks:
- Update `IsAdmin()` to recognize `orders.manage` as admin-equivalent for Orders behavior checks.
- Keep `admin.full` as a super-scope if desired.
- Keep role checks only as compatibility fallback (or mark as deprecated behavior in comment).
- Add a short comment clarifying `IsAdmin()` is a coarse helper, not route authorization.

Acceptance checks:
- User with `orders.manage` scope is treated as admin in `OrdersController` internal branching.
- Helper still supports existing admin.full behavior.

#### A3. Add tests for shared claims helpers (new unit tests)
Project (new, recommended):
- `tests/backend/Shared.UnitTests`

Files to add:
- `tests/backend/Shared.UnitTests/Shared.UnitTests.csproj`
- `tests/backend/Shared.UnitTests/Extensions/ClaimsExtensionsTests.cs`

Packages:
- `xunit`
- `xunit.runner.visualstudio`
- `FluentAssertions`
- `Microsoft.NET.Test.Sdk`

Tasks:
- Add tests for `GetScopes()` splitting/dedup.
- Add tests for `HasScope()` exact matching.
- Add tests for `IsAdmin()` with `orders.manage` and `admin.full`.

Acceptance checks:
- Unit test project passes locally.

### B. Orders service authorization cleanup (scope-only route policies)

#### B1. Remove role bypass from Orders policy config
Files:
- `src/backend/Orders/appsettings.json`
- `src/backend/Orders/appsettings.Development.json` (if policy sections are introduced/overridden)
- `src/backend/Orders/appsettings.Docker.json` (check if policy section exists)

Tasks:
- Convert `Authorization:Policies` entries to scope-only route authorization.
- For each policy, clear/remove `Roles` list:
  - `orders.read`: scopes `orders.read`, `orders.manage`
  - `orders.write`: scopes `orders.write`, `orders.manage`
  - `orders.manage`: scopes `orders.manage`
- Keep hierarchy via scope inclusion (e.g., `orders.manage` satisfies `orders.read` and `orders.write`).

Acceptance checks:
- No `Roles` entries remain in Orders policy config (unless intentionally kept empty arrays).
- Orders routes still reference the same policy names.

#### B2. Confirm Orders route usage is consistent with scope policies
Files:
- `src/backend/Orders/Controllers/OrdersController.cs`

Tasks:
- Review endpoint policy annotations and confirm they align with intended permission model.
- No code changes required unless policy names change (not recommended).
- Validate internal `isAdmin` branching now behaves correctly with scope-only tokens.

Acceptance checks:
- Admin-only endpoints continue to use `orders.manage` policy.
- Non-admin endpoints continue to use `orders.read`/`orders.write` policies.

#### B3. Clarify shared policy extension semantics (docs/comments only)
Files:
- `src/backend/Shared/Authorization/Extensions/AuthorizationExtensions.cs`

Tasks:
- Keep existing OR semantics (generic/shared behavior), but update XML comments to clarify:
  - Resource APIs using OAuth should prefer scope-only policies.
  - Role lists are optional and should not be used as a bypass for permission scopes unless explicitly intended.

Acceptance checks:
- Comment/docstring reflects actual policy semantics and recommended usage.

### C. AuthServer RBAC issuance and refresh behavior

#### C1. Validate and document role->scope source of truth
Files:
- `src/backend/AuthServer/appsettings.Scopes.json`
- `src/backend/AuthServer/Services/ScopeConfigurationService.cs`

Tasks:
- Keep `UserScopes.RoleScopes` mapping as canonical RBAC permission mapping.
- Verify casing consistency for role names (`Admin`, `User`, `Support`) across seeding and role assignment flows.
- Add a brief code/doc comment (optional) clarifying roles map to scopes here.

Acceptance checks:
- Seeded roles names match `RoleScopes` keys exactly.

#### C2. Change PKCE user flow to grant all role-derived permission scopes (and document expectations)
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Tasks:
- Update `CreateUserPrincipalAsync(...)` to grant all role-derived permission scopes, even if not requested.
- Keep standard protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`) request-driven.
- Add a code comment documenting this deliberate behavior.
- Document grant-type difference explicitly:
  - user PKCE flow intentionally grants all role-derived permission scopes (no extra client-allowed intersection step, per product decision)
  - `client_credentials` remains request-driven/validated by intersection behavior

Acceptance checks:
- PKCE user token includes role-derived permission scopes even when not requested (intentional behavior).
- PKCE user token does not include permission scopes outside the user's role mapping.

#### C3. Normalize refresh token re-issuance behavior (roles + scopes)
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Tasks:
- Keep existing refresh behavior that re-evaluates user activity and role-derived scopes (already present).
- Re-attach role claims in refresh branch for consistency/backward compatibility (confirmed, since role claims remain in access tokens).
- Ensure refreshed principal claims have destinations set appropriately if roles are added.
- Update refresh issuance to align with the new model:
  - re-issue all current role-derived permission scopes (not limited to originally requested business scopes)
  - keep protocol scopes constrained to the originally granted/requested set
  - this is confirmed behavior (not optional)

Acceptance checks:
- Role downgrade after login removes scopes from refreshed token.
- Role upgrade after login can add newly allowed role-derived permission scopes in refreshed token.
- Refreshed token continues to work with Orders scope policies.
- Existing access tokens are expected to retain prior permissions until refresh/relogin/expiry (confirmed v1 behavior).

#### C4. Optional RBAC hardening (defer if time-boxed)
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Tasks:
- Optionally intersect user-requested scopes with OpenIddict application permissions explicitly in `CreateUserPrincipalAsync(...)` for defense-in-depth.
- Keep as a separate commit/task if implemented.

Acceptance checks:
- No regression in existing auth integration tests.

### D. AuthServer configuration alignment for RBAC

#### D1. Review client allowed scopes for PKCE client(s)
Files:
- `src/backend/AuthServer/appsettings.Clients.json`

Tasks:
- Confirm `web-admin` allowed scopes are intentional and match UI features.
- Confirm `orders` service client keeps only service-level scopes.
- Remove any stale scopes if no longer used (separate change if uncertain).

Acceptance checks:
- `web-admin` can request expected user scopes.
- `orders` client credentials remain limited to orders scopes.

#### D2. Optional scope resource metadata normalization
Files:
- `src/backend/AuthServer/appsettings.Scopes.json`

Tasks:
- Optionally populate `Resources` on scopes (e.g., `orders-api`) for future audience/resource modeling.
- Defer if not needed for current local JWT validation.

Acceptance checks:
- If implemented, no token issuance regression.

### E. Backend tests to validate RBAC behavior

#### E1. Extend AuthServer integration tests (existing project)
Project:
- `tests/backend/AuthServer.IntegrationTests`

Primary file (existing):
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`

Tasks:
- Add/confirm test: User role gets all role-derived permission scopes even when only minimal standard scopes are requested, and still does not receive `orders.manage`.
- Add test: Admin role gets `orders.manage` from role mapping even when not explicitly requested.
- Add test: Refresh token after role downgrade removes `orders.manage`.
- Add test: Refresh token after role upgrade adds newly allowed role-derived permission scopes (`orders.manage`).
- Add/confirm `userinfo` roles behavior remains gated by `roles` scope.
- If refresh branch adds role claims, add a regression assertion for refreshed token behavior consistency (scope-focused; claims assertion optional).

Acceptance checks:
- Tests pass and explicitly prove role->scope issuance + refresh re-evaluation.

#### E2. Add Orders authorization tests (recommended new integration project)
Project (new):
- `tests/backend/Orders.IntegrationTests`

Recommended stack:
- `xUnit`
- `FluentAssertions`
- `Microsoft.AspNetCore.Mvc.Testing`
- (Optional) Testcontainers if using real AuthServer + real JWTs end-to-end

Recommended v1 approach (pragmatic):
- Start Orders with a test host and inject authenticated principals/tokens to validate policy behavior and controller responses.
- OR if time allows, use real AuthServer-issued tokens from `AuthServer.IntegrationTests` helpers for full end-to-end fidelity.

Initial test cases:
1. Token with `orders.read` can access read endpoint and is denied admin endpoint.
2. Token with `orders.write` can access write endpoint and is denied manage endpoint.
3. Token with `orders.manage` can access admin endpoints.
4. Token with no orders scopes gets `403`.
5. Space-separated `scope` claim format is accepted by policies (proves shared parser fix).

Acceptance checks:
- Orders route authorization behavior is proven by automated tests (200/403 matrix).

#### E3. Keep test history and best practices docs in sync
Files:
- `docs/TESTING.md`
- `README.md`
- `docs/DEVELOPMENT.md`
- `docs/AGENTS.MD`

Tasks:
- Add a backend RBAC testing section:
  - AuthServer integration tests validate role->scope issuance + refresh re-evaluation
  - Orders tests validate scope-based route enforcement
- Include examples of how to add a new permission and corresponding tests.
- Include guidance on `scope` claim parsing pitfall.

Acceptance checks:
- Docs reflect actual test projects and commands.

### F. Documentation updates for RBAC architecture (required)

#### F1. Document the RBAC model and extension workflow
Files:
- `docs/ARCHITECTURE.md`
- `README.md`
- `docs/DEVELOPMENT.md`

Tasks:
- Explain clearly:
  - Roles = identity grouping
  - Scopes = API permissions
  - APIs authorize by scopes (policies)
  - AuthServer maps roles to scopes at token issuance time
- Add “how to add a permission” steps:
  1. Add scope to `appsettings.Scopes.json`
  2. Add role mappings in `UserScopes.RoleScopes`
  3. Add client allowed scope in `appsettings.Clients.json`
  4. Add/update API policy config (`Orders`)
  5. Add/update tests

Acceptance checks:
- Docs match implemented behavior (no mention of role-based route bypass for Orders).

#### F2. Document pitfalls / guardrails
Files:
- `docs/ARCHITECTURE.md` and/or `docs/TESTING.md`

Tasks:
- Add a short guardrails section:
  - Don’t authorize resource routes directly by roles when using OAuth scopes for permissions.
  - Don’t assume `scope` claim format.
  - Refresh grants must re-evaluate current permissions.

Acceptance checks:
- Guardrails section exists and is repo-specific.

## Execution Order (recommended)
1. A1/A2 (shared claims + admin helper)
2. A3 (shared helper tests)
3. B1/B2/B3 (Orders policy config + comments)
4. C3 (AuthServer refresh normalization), then C1/C2 review comments
5. E1 (AuthServer integration tests)
6. E2 (Orders authorization tests)
7. E3 + F1/F2 (docs)
8. Final validation sweep

## Final Validation Checklist (done criteria)
- [x] `User` role token cannot get `orders.manage` even if requested
- [x] `Admin` role token gets `orders.manage` from role mapping (not request-limited, by design)
- [x] Token issuance includes role-derived permission scopes even when not requested (intentional)
- [x] Refresh token re-evaluates scopes, removes revoked role-based permissions, and can add newly granted role-based permissions
- [x] Orders policies are scope-only (no role bypass)
- [x] `HasScope()` works with repeated and space-separated scope claims
- [ ] Orders endpoints return expected `200/403` per scope matrix
- [x] Docs explain RBAC model and how to extend it
- [x] Tests added/updated and passing locally

## Implementation Log (to update while coding)
- Implemented core RBAC behavior and `AuthorizationController.Token()` refactor (grant-service extraction + OpenIddict-specific `GrantFlowResult`).
- Updated PKCE user scope issuance to add all role-derived permission scopes (not request-limited) while keeping protocol scopes request-driven.
- Updated refresh-token issuance to recompute current role-derived permission scopes (supports role upgrades/downgrades on refresh) and reattach role claims.
- Kept `client_credentials` grant behavior request/intersection-based.
- Hardened shared scope parsing (`GetScopes`/`HasScope`) for repeated and space-separated `scope` claims.
- Updated `IsAdmin()` helper to recognize `orders.manage` (and `admin.full`) for internal behavior branching.
- Removed role lists from `Orders` policy config (`src/backend/Orders/appsettings.json`) to prevent role-bypass route authorization.
- Added `Shared.UnitTests` for claims/scope parsing helper behavior.
- Extended `AuthServer.IntegrationTests` with RBAC coverage (user/admin issuance, refresh role downgrade/upgrade).
- Validation run:
  - `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj` -> Passed (22/22)
  - `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj` -> Passed (5/5)
- Deferred in this pass:
  - `tests/backend/Orders.IntegrationTests` route 200/403 matrix
- Docs updated in this pass:
  - `README.md`
  - `docs/ARCHITECTURE.md`
  - `docs/DEVELOPMENT.md`
  - `docs/TESTING.md`
  - `docs/AGENTS.MD`
