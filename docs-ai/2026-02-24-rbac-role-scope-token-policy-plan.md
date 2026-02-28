# RBAC Role->Scope->Token->Policy Plan (AuthServer + Orders)

Date: 2026-02-24
Status: Planning (pre-implementation)

## Goal
Implement RBAC where:
- Users are assigned one or more roles
- Roles map to allowed scopes
- During PKCE login (authorization code flow), granted permission scopes are derived from the user's roles (not limited by requested business scopes)
- Resource APIs (e.g. Orders) authorize routes by scope-based policies

## High-level assessment (current codebase)
The foundation already exists and mostly matches the target design:
- `AuthServer` already computes user scopes from roles via `ScopeConfigurationService`
- `AuthorizationController.CreateUserPrincipalAsync(...)` already filters requested scopes against role-derived user scopes and sets granted scopes in the token principal (this will be changed per design choice)
- `Orders` routes already use named policies like `orders.read`, `orders.write`, `orders.manage`
- `Orders` policies are config-driven

However, there are important gaps/mismatches that will block a clean RBAC implementation:
1. `Orders` policies currently allow `Role OR Scope`, which can bypass the intended scope-based route auth model
2. `ClaimsExtensions.HasScope(...)` is too strict and may fail if the `scope` claim is space-separated
3. Refresh token flow rebuilds user principal without role claims, which can break role-based helper logic after refresh
4. Internal admin checks in `Orders` use role/admin-full heuristics instead of the explicit permission scope used by route policy (`orders.manage`)
5. There is no Orders integration test coverage proving end-to-end scope enforcement against real AuthServer-issued tokens

## Current implementation map (what exists)

### AuthServer (already present)
- `src/backend/AuthServer/Services/ScopeConfigurationService.cs`
  - Computes scopes = default scopes + role-specific scopes from `ScopeConfiguration:UserScopes`
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
  - `CreateUserPrincipalAsync(...)`:
    - Loads user roles
    - Adds role claims to principal
    - Currently filters requested scopes against role-derived scopes (+ standard OIDC scopes)
    - Sets scopes/resources on principal
  - `Token()` refresh branch:
    - Recomputes user scopes and filters original scopes against current user scopes (good)
    - Rebuilds identity but does not re-add roles (gap if downstream code depends on roles)
  - `Userinfo()`:
    - Returns roles only when `roles` scope is granted (good)
- `src/backend/AuthServer/appsettings.Scopes.json`
  - Has `UserScopes:RoleScopes` mapping for `Admin`, `User`, `Support`
  - Has custom scopes for `orders.*`, `profile.*`, `admin.*`
- `src/backend/AuthServer/appsettings.Clients.json`
  - `web-admin` is PKCE public client and is allowed the custom scopes

### Orders (already present)
- `src/backend/Orders/Controllers/OrdersController.cs`
  - Uses `[Authorize(Policy = "orders.read/write/manage")]`
- `src/backend/Orders/Program.cs`
  - Configures OpenIddict validation against AuthServer authority
  - Calls `builder.Services.AddConfigurablePolicies(builder.Configuration)`
- `src/backend/Orders/appsettings.json`
  - Defines `Authorization:Policies` with both `Scopes` and `Roles`
- `src/backend/Shared/Authorization/Extensions/AuthorizationExtensions.cs`
  - Policy logic is `Scope OR Role`
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
  - `HasScope()` checks exact claim match only (`user.HasClaim("scope", scope)`) 
  - `IsAdmin()` checks role/admin.full scope (not `orders.manage`)

## Target behavior (clarified)
For user access to Orders routes:
- Authorization should succeed because the access token contains the required scope(s)
- Role membership should be used in `AuthServer` to derive scopes, not as a direct bypass in `Orders` route policies
- If user role assignment changes, refresh token grant should re-evaluate and reduce scopes accordingly

## Detailed implementation checklist (code changes)

### 0) AuthorizationController refactor (recommended as part of RBAC work)

Rationale:
- `AuthorizationController.Token()` currently contains large grant-type branches with mixed responsibilities:
  - protocol request parsing
  - principal validation
  - user/client lookups
  - scope computation
  - OpenIddict error/Forbid shaping
- RBAC changes will make these branches even more complex (PKCE user-scope auto-grant + refresh recomputation).

Recommendation:
- Extract grant-type logic into a dedicated auth flow service, but keep controller responsible for HTTP/OpenIddict endpoint translation.
- Use a dedicated endpoint outcome/result type for OpenIddict flows instead of reusing generic `Shared.Common.Result<T>` directly.
  - Reason: OpenIddict token endpoint needs protocol-specific outcomes (`SignIn`, `Forbid` with `error`, `error_description`) that don't map cleanly to normal API `Ok/BadRequest/ProblemDetails`.
- Scope for current RBAC pass (confirmed): refactor `Token()` grant branches first; defer `Authorize()` extraction unless it stays low-risk after RBAC changes.

Suggested shape (v1):
- New service: `AuthorizationGrantService` (or `OpenIddictGrantFlowService`)
- Methods:
  - `HandleAuthorizationCodeGrantAsync(HttpContext, OpenIddictRequest)`
  - `HandleClientCredentialsGrantAsync(OpenIddictRequest)`
  - `HandleRefreshTokenGrantAsync(HttpContext, OpenIddictRequest)`
- Each method returns a dedicated outcome type (example):
  - `GrantFlowResult.Success(ClaimsPrincipal principal)`
  - `GrantFlowResult.Forbid(string error, string? description = null)`
- Controller maps `GrantFlowResult` to `SignIn(...)` / `Forbid(...)`

Optional v2 refactor:
- Extract principal construction into a dedicated `TokenPrincipalFactory` service for user/client/refresh issuance.
- Extract OpenIddict error factory helpers to reduce duplicated `Forbid(AuthenticationProperties(...))` blocks.

### 1) AuthServer: harden and normalize role->scope issuance

#### 1.1 Keep role-to-scope mapping as the source of truth
Files:
- `src/backend/AuthServer/appsettings.Scopes.json`
- `src/backend/AuthServer/Services/ScopeConfigurationService.cs`

Tasks:
- Keep `UserScopes:RoleScopes` as the canonical RBAC mapping.
- Review role names for exact casing consistency (`Admin`, `User`, `Support`) across seeding, UI/admin APIs, and docs.
- Optionally add comments/docs clarifying that resource permissions must be expressed as scopes, not roles.

#### 1.2 Change user token issuance to grant all role-derived permission scopes
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Current status:
- Requested + role-allowed filtering is already implemented in `CreateUserPrincipalAsync(...)`.
- OpenIddict likely validates client permissions earlier, but the code does not explicitly intersect with client permissions in the user flow.

Requested design choice:
- Grant all role-derived permission scopes in the token, even if they were not requested by the client.
- Keep standard protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`) request-driven to avoid over-granting identity data/refresh capability.

Tasks:
- Update `CreateUserPrincipalAsync(...)` so final granted scopes are:
  - all role-derived permission scopes allowed for the user
  - plus requested standard protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`)
- Add a code comment documenting this deliberate behavior (different from strict request-based least-privilege scope issuance).
- Preserve and document grant-type differences:
  - user PKCE flow: intentionally add all role-derived permission scopes (no extra client-allowed intersection step, per product decision)
  - client_credentials flow: keep requested-scope validation/intersection behavior
- Add a prominent code comment documenting this divergence and its first-party trust assumption

#### 1.3 Refresh token branch: re-attach roles or remove downstream dependence on roles
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- `src/backend/Orders/Controllers/OrdersController.cs`

Gap:
- Refresh token flow recalculates scopes but does not add role claims back to the new principal.

Required decision (recommended path for this RBAC design):
- Do **not** depend on roles in Orders route/internal authorization decisions.
- Keep route auth scope-based.
- Update Orders internal admin checks to use permission scopes (`orders.manage`) instead of role claims.

Tasks:
- Update `ClaimsExtensions.IsAdmin(...)` to treat `orders.manage` (and optionally `admin.full`) as admin-equivalent permission.
- If any service still depends on role claims, then also re-add roles in refresh branch. Otherwise roles in access token can remain non-authoritative.

Confirmed implementation choice:
- Short term: add roles in refresh branch for consistency and backward compatibility, while still moving Orders auth decisions to scopes.
- Align refresh to the new issuance model:
  - re-issue all current role-derived permission scopes (not just originally requested business scopes)
  - keep protocol scopes constrained to what was originally granted/requested (e.g. don't auto-add `offline_access`/`roles`)

#### 1.4 Claim destination review (optional but recommended)
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Current status:
- All claims are sent to access token by default (`GetDestinations` always adds `AccessToken`).
- This includes role claims.

Tasks:
- Decide whether roles should remain in access tokens.
- For strict RBAC-by-scope, roles are not required by Orders route policies.
- Keep roles in `userinfo` gated by `roles` scope for client UI display.
- Optional tightening (v2): limit role claims in access token unless specifically needed.

### 2) Orders: make route authorization scope-first (and avoid role bypass)

#### 2.1 Remove role-based bypass from Orders policy config
Files:
- `src/backend/Orders/appsettings.json`
- `src/backend/Orders/appsettings.Development.json` (if policy overrides exist later)
- `src/backend/Orders/appsettings.Docker.json` / `appsettings.*` variants if they define auth policies

Current gap:
- Policies contain both `Scopes` and `Roles`, and shared logic uses OR semantics.
- This allows access even if the required scope is not present but the role claim is present.

Tasks (recommended v1):
- Make Orders policies scope-only:
  - `orders.read` -> scopes: `[orders.read, orders.manage]`, roles: `[]`/remove
  - `orders.write` -> scopes: `[orders.write, orders.manage]`, roles: `[]`/remove
  - `orders.manage` -> scopes: `[orders.manage]`, roles: `[]`/remove
- Update docs to state Orders route auth is scope-based.

Note:
- This preserves the practical permission hierarchy by including `orders.manage` in lower-level policies.

#### 2.2 Keep shared authorization extension generic, but clarify its semantics
Files:
- `src/backend/Shared/Authorization/Extensions/AuthorizationExtensions.cs`

Tasks:
- Keep OR semantics if other services may use mixed scope/role policies.
- Add/adjust XML comments/docs to clarify that OR is intentional and that API policies should prefer scopes for OAuth/OIDC resource authorization.

Optional improvement (not required for v1):
- Add configurable policy evaluation mode (`AnyOf` / `ScopesOnly` / `RolesOnly` / `AllOf`) to avoid ambiguity across services.

#### 2.3 Fix scope claim parsing robustness in shared claims helpers
Files:
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`

Gap:
- `HasScope()` only checks exact `scope` claim values and does not split space-delimited claims.
- Depending on token validation behavior, this can cause false negatives.

Tasks:
- Update `GetScopes()` and `HasScope()` to support both formats:
  - multiple `scope` claims (`scope=orders.read`, `scope=orders.write`)
  - single space-separated `scope` claim (`scope="orders.read orders.write"`)
- Preserve exact matching semantics after splitting (no substring matching).
- Add unit tests for claim parsing helper.

#### 2.4 Align internal admin logic with scope-based authorization
Files:
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- `src/backend/Orders/Controllers/OrdersController.cs` (indirectly uses `HttpContext.IsAdmin()`)

Gap:
- `IsAdmin()` currently relies on `Role=Admin`, `role=admin`, or `admin.full` scope.
- Orders endpoints and behaviors are actually governed by `orders.manage`.

Tasks:
- Update `IsAdmin()` to recognize `orders.manage` as admin-level access for Orders behavior checks.
- Keep `admin.full` as a super-scope if desired.
- Treat role checks as compatibility fallback only (or remove if you want strict permission-only logic).

Reason:
- A user may legitimately have `orders.manage` without `admin.full`.
- A refreshed token may preserve `orders.manage` but not roles if role claims are omitted.

### 3) AuthServer + Orders configuration normalization for RBAC

#### 3.1 Ensure client allowed scopes are consistent with role-scope model
Files:
- `src/backend/AuthServer/appsettings.Clients.json`

Tasks:
- Confirm `web-admin` allowed scopes include only scopes the UI may request.
- Keep `orders.manage` only if admin flows truly require it.
- Avoid exposing unnecessary scopes to public clients.
- Confirm `orders` service client (client credentials) has only service scopes.

#### 3.2 Normalize scope catalog and resource metadata (optional but useful)
Files:
- `src/backend/AuthServer/appsettings.Scopes.json`

Tasks:
- Optionally populate `Resources` per scope (e.g., `orders-api`) for better OpenIddict resource modeling.
- This is not required for current local validation setup, but helps future audience/resource enforcement.

### 4) Tests to validate the RBAC behavior (recommended)

## 4A) AuthServer integration tests (extend existing suite)
Project exists:
- `tests/backend/AuthServer.IntegrationTests`

Add tests to prove role->scope issuance and re-evaluation:

1. `AuthorizationCodePkce_UserRole_GetsAllRolePermissionScopes_EvenWhenNotRequested`
- Login as seeded `User`
- Request minimal standard scopes (e.g. `openid profile email`)
- Assert access token still includes role-derived permission scopes for `User` (e.g. `orders.read`, `orders.write`, `profile.read`, `profile.write`)
- Assert access token still excludes admin-only scopes (e.g. `orders.manage`)

2. `AuthorizationCodePkce_AdminRole_GetsOrdersManageFromRole`
- Login as seeded `Admin`
- Request minimal standard scopes
- Assert granted scopes include `orders.manage` because it is role-derived

3. `RefreshToken_WhenRolePermissionsReduced_RemovesNoLongerAllowedScopes`
- Login as Admin requesting `orders.manage`
- Change user role assignment in DB (e.g., remove Admin, add User)
- Use refresh token
- Assert refreshed token no longer grants `orders.manage`

4. `RefreshToken_WhenRolePermissionsIncrease_AddsNewRolePermissionScopes`
- Login as User with minimal standard scopes
- Upgrade role assignment in DB (e.g., add Admin)
- Use refresh token
- Assert refreshed token now includes newly allowed role-derived scopes (e.g. `orders.manage`)

5. `UserInfo_WithRolesScope_ReturnsRoles` (likely already present/partially covered)
- Keep as regression proof that UI can read roles when requesting `roles`

6. `UserInfo_WithoutRolesScope_DoesNotReturnRoles` (already exists)
- Keep and reference as privacy/minimal-claims behavior

Optional:
- Assert role claims presence/absence in access token after refresh if you choose to standardize it.

## 4B) Orders integration tests (new project recommended)
New project recommendation:
- `tests/backend/Orders.IntegrationTests` (xUnit + FluentAssertions)

Goal:
- Validate actual route authorization behavior using real AuthServer-issued JWTs (or realistic JWTs if startup complexity is too high).

Recommended approach (best fidelity):
- Start AuthServer and Orders with `WebApplicationFactory` in-process (or separate factories if feasible)
- Use AuthServer integration helpers to obtain user access tokens via PKCE or a test helper flow
- Call Orders endpoints with bearer token
- Assert `200/403` based on scopes

Initial test matrix (high value):
1. User token with `orders.read` can access `GET /Orders` but not `GET /Orders/all`
2. User token with `orders.write` can create/update own order but not admin endpoints
3. Admin token with `orders.manage` can access `GET /Orders/all` and status admin endpoints
4. Token missing orders scopes returns `403` on protected Orders routes
5. Client-credentials token with `orders.read` can access read endpoints only if intended for service use (confirm desired behavior)

Pragmatic v1 alternative (lower setup cost):
- Add integration tests for `Shared.Authorization` + minimal test host with fake authenticated principals to validate policy semantics and `HasScope()` parsing.
- Then add full Orders+Auth end-to-end tests in v2.

## 4C) Shared helper unit tests (fast, important)
New/extended test project if one exists for `Shared`; otherwise add a small unit test project.

Add tests for `Shared.Extensions.ClaimsExtensions`:
- `HasScope` returns true for exact `scope` claim
- `HasScope` returns true for space-separated scope claim
- `HasScope` does not match substrings
- `GetScopes` de-duplicates and splits correctly
- `IsAdmin` returns true for `orders.manage` and `admin.full`

### 5) Documentation updates (required)
Add/Update docs so future changes preserve the RBAC design.

#### 5.1 New/updated auth architecture documentation
Files:
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/TESTING.md`
- `docs/AGENTS.MD`
- `README.md` (brief summary + link)

Content to add/update:
- RBAC model definition:
  - Roles are identity/grouping metadata
  - Scopes are API permissions
  - APIs authorize by scopes (policies)
- Token issuance rules:
  - User PKCE permission scopes = role-derived scopes (not request-limited; no extra client-allowed intersection step, by design)
  - Client-credentials scopes remain request-driven/validated
  - Standard protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`) remain request-driven
- Orders policy examples using scope-only authorization
- How to add a new permission:
  1. Add scope in `appsettings.Scopes.json`
  2. Map scope to roles in `UserScopes.RoleScopes`
  3. Add client allowed scope if needed in `appsettings.Clients.json`
  4. Add/adjust API policy in `Orders` (or other service)
  5. Add integration tests
- Testing guidance:
  - AuthServer integration tests for issuance/filtering/refresh behavior
  - Orders authorization tests for route enforcement

#### 5.2 Document pitfalls explicitly (important)
Add a short “pitfalls” section with:
- Do not authorize resource routes by role claims when using OAuth scopes as permissions
- Do not assume `scope` claim format (space-separated vs repeated claims)
- Refresh token grants must re-evaluate current permissions and follow the same issuance rules

## Implementation order (single pass)
1. Harden shared scope parsing (`ClaimsExtensions`) + unit tests
2. Convert Orders policy config to scope-only (remove role entries)
3. Align `IsAdmin()` with `orders.manage` / `admin.full`
4. Normalize AuthServer refresh branch behavior (roles consistency decision + implementation)
5. Add/extend AuthServer integration tests for role->scope issuance and refresh re-evaluation
6. Add Orders authorization integration tests (or minimal policy-host tests first if time-boxed)
7. Update docs (architecture + testing + developer guidance)

## Open questions / decisions (non-blocking for planning)
1. Should role claims remain in access tokens at all, or only in `userinfo` when `roles` scope is granted?
   - Recommendation: keep for now (compatibility), but do not use for API route authorization.
2. Do you want Orders service-to-service calls (client credentials) to use the same route set as user calls?
   - If yes, ensure policies are scope-only and endpoints that require `sub` user ID are separated or guarded by identity type.
3. Do you want to introduce a dedicated permission helper (`HasPermission`) and deprecate `IsAdmin()` naming to avoid cross-domain ambiguity?
   - Recommendation: yes, eventually (`IsAdmin` is too broad and leaks domain assumptions).

## Acceptance criteria
- A `User` role token cannot get `orders.manage` even if requested
- An `Admin` role token gets `orders.manage` from role mapping (subject to client allowance)
- Token issuance may include role-derived permission scopes even when not requested (intentional behavior)
- Refresh token grant removes scopes no longer allowed after role changes and can add newly allowed role-derived permission scopes
- Orders routes authorize based on scopes, not role fallback
- Scope parsing works for both repeated and space-separated `scope` claim formats
- Docs explain the RBAC design and how to extend it safely
