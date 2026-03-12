# Multi-Tenant Column Model Plan

Date: 2026-03-12

Related:
- `docs-ai/2026-03-12-multi-tenant-column-clarification-decisions.md`
- `docs-ai/TASKS-multi-tenant-column-2026-03-12.md`
- `docs-ai/2026-03-12-multi-tenant-column-implementation-checklist.md`
- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md`
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Data/AppDbContext.cs`
- `src/backend/AuthServer/Data/Entities/AppUser.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/Orders/Data/Entities/Order.cs`
- `src/backend/Orders/Data/Configurations/OrderConfiguration.cs`
- `src/backend/Orders/Data/Configurations/CustomerConfiguration.cs`
- `src/backend/Orders/Data/Configurations/IdempotencyRecordConfiguration.cs`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/web-app/contexts/auth-context.tsx`
- `src/web-app/lib/auth/oauth2-auth.ts`

## 1. Overview

### Problem statement

The repo currently supports global identity plus scope-based RBAC, but it does not model SaaS subscribers as tenants. There is no tenant entity, no tenant membership model, no tenant-scoped token context, and no tenant filtering in `Orders`. Current business-data isolation is effectively by `UserId`, not by tenant.

The requested feature is column-based multi-tenancy, not schema-per-tenant:

- a tenant represents a SaaS subscriber
- a user can belong to multiple tenants
- identity stays global
- access management is tenant-local
- the token is the canonical tenant authorization context
- route/URL selects or requests that tenant context

### Goals

- Introduce a column-based tenant model across `AuthServer`, `Orders`, and `web-app`.
- Keep global identity in `AuthServer`.
- Make tenant membership the source of tenant-local access.
- Issue tenant-scoped JWTs whose claims represent exactly one active tenant context.
- Add tenant switching, self-serve tenant creation, platform-admin tenant support flows, and tenant-local membership management.
- Make `Orders` tenant-isolated by `TenantId`.
- Replace the current seed/sample assumptions with tenant-aware sample data.

### Non-goals

- No schema-per-tenant or database-per-tenant partitioning.
- No full invite-acceptance UX in this pass; only the data model and API foundation for pending invites.
- No attempt to preserve deprecated `src/web-admin` as an implementation target.
- No immediate token revocation infrastructure beyond current refresh/re-login/expiry behavior.

## 2. Assumptions & Constraints

### Known constraints

- Frontend target is `src/web-app`; `src/web-admin` is deprecated.
- The browser calls `AuthServer` and `Orders` directly. There is no proxy/BFF layer.
- `AuthServer` already issues self-contained JWT access tokens.
- Current RBAC is scope-based for APIs, with runtime role-to-scope assignments stored in `AuthServer` DB.
- Current auth flow is OAuth2 Authorization Code + PKCE with refresh tokens.
- Existing auth/session state in `web-app` is stored in browser storage and has no tenant context yet.

### Explicit assumptions

- Global role catalog remains in `AuthServer`, but tenant membership chooses exactly one role from that catalog.
- Tenant token context is trusted by downstream APIs until expiry.
- Tenant membership removal/deactivation and tenant suspension take effect on refresh/token exchange, not immediate revocation.
- Platform-admin tenant access supports both direct tenant mode and impersonation mode.
- Platform-admin direct mode is full access, but visually obvious and fully audited.
- `Orders` visibility is tenant-wide for all active tenant members in v1.

## 3. Architecture / Approach

### Proposed solution

Add a tenant domain to `AuthServer` and move from global user-role assignment to tenant membership assignment for tenant-scoped access:

- `Tenant`
  - `Guid` PK
  - immutable unique slug
  - lifecycle state: `Pending`, `Active`, `Suspended`, `Archived`
- `TenantMembership`
  - links global `AppUser` to one `Tenant`
  - exactly one role per membership via `RoleId`
  - `IsActive`
  - `IsDefault`
- `TenantInvite`
  - supports pending invite records for brand-new emails
  - designed for later acceptance/onboarding flow

Authorization flow:

1. User authenticates globally.
2. `AuthServer` selects the default tenant for first app session when the user has tenant memberships.
3. `AuthServer` issues a tenant-scoped token containing:
   - active tenant id
   - active tenant slug
   - effective tenant role
   - tenant-derived scopes
   - support/impersonation claims when applicable
4. `web-app` uses a memberships endpoint to discover accessible tenants.
5. Visiting `/t/{tenantSlug}/...` auto-switches tenant context when needed by calling a dedicated `AuthServer` switch endpoint.

### Control flow

Normal user login:

1. complete global authentication
2. resolve active default membership
3. issue tenant-scoped app token
4. load memberships list from `AuthServer`
5. land at `/t/{tenantSlug}/dashboard`

Normal tenant switch:

1. route or switcher requests another tenant
2. `web-app` calls tenant switch endpoint
3. `AuthServer` validates active membership and tenant lifecycle
4. `AuthServer` issues new tenant-scoped token set
5. `web-app` replaces local tokens and continues in the new tenant route

Platform-admin support mode:

1. platform admin discovers tenant via platform API
2. platform admin can enter direct tenant mode or impersonation mode
3. direct mode issues a tenant-scoped support token with actor metadata
4. impersonation issues a tenant-scoped token for the effective user, plus actor metadata and required reason
5. all support actions are audit logged

### Key components and responsibilities

- `AuthServer`
  - tenant domain source of truth
  - tenant membership source of truth
  - tenant token issuance/switch logic
  - invite records
  - platform-admin and tenant-admin management APIs
  - audit records for support mode and impersonation
- `Orders`
  - enforces tenant isolation by `TenantId`
  - trusts tenant-scoped JWT until expiry
  - scopes uniqueness/indexes by tenant where required
- `web-app`
  - global authenticated area vs tenant-context area
  - tenant switcher and auto-switch behavior
  - support-mode banner and exit controls
  - tenant-local membership management UI
  - platform admin global UI

## 4. API / Contracts

### AuthServer API additions

Identity-scoped:

- `GET /api/me/tenants`
  - returns all active switchable memberships for signed-in identity
  - includes default membership information
- `POST /api/auth/tenant-switch`
  - exchanges current authenticated session into a new tenant-scoped token set
  - input: tenant id or slug
  - output: access token, refresh token, granted scopes, effective tenant summary

Platform/global:

- tenant discovery/list/search APIs for `PlatformAdmin`
- self-serve tenant creation API
- tenant lifecycle management APIs
- support-mode start/stop APIs
- impersonation start/stop APIs

Tenant-context:

- tenant membership list/create/update/remove APIs
- tenant-local member activation/deactivation APIs
- tenant settings APIs

### Error handling

- `403` for insufficient tenant or platform privileges
- `404` for unknown tenant or membership within allowed visibility rules
- `409` for slug uniqueness, duplicate membership, or last-tenant-admin guardrails
- `400` for invalid lifecycle transition, invalid switch target, or invalid impersonation target
- `401` remains standard auth failure

### Authn/authz expectations

- Tenant APIs authorize primarily by tenant-scoped token claims and scopes.
- Platform APIs authorize by global `PlatformAdmin` role/scope set.
- Direct support mode and impersonation mode must be explicit in both token claims and audit records.
- Impersonation requires a reason before token issuance.

## 5. Data Model & Storage

### AuthServer schema changes

Add:

- `Tenant`
- `TenantMembership`
- `TenantInvite`
- support-mode / impersonation audit table

Adjust:

- user-management logic to stop assuming global role assignment is the main access model
- seed data to use global platform role plus tenant-local roles:
  - `PlatformAdmin`
  - `TenantAdmin`
  - `TenantUser`
  - `TenantSupport`

Recommended constraints:

- unique tenant slug
- unique `(TenantId, UserId)` membership
- one default active membership per user
- one active role reference per membership

### Orders schema changes

Add `TenantId` to:

- `Orders`
- `Customers`
- `IdempotencyRecords`

Update uniqueness:

- `Customer.Email` unique per tenant
- `Order.OrderNumber` unique per tenant
- `IdempotencyKey` unique per tenant or per tenant+user depending on final implementation choice

### Migration approach

- hard reset sample/dev data for the new tenant-aware seed model
- migration-based schema changes for actual code paths and test databases
- no compatibility effort for deprecated `web-admin`

### Backward compatibility

- this is a functional model shift, not a transparent additive change
- old global-role assumptions in admin flows must be removed or fenced off in the same pass
- self-contained JWT behavior remains, but tenant context becomes first-class

## 6. Implementation Steps

### Step 1. Add tenant domain to AuthServer

Files/modules likely to change:

- `src/backend/AuthServer/Data/AppDbContext.cs`
- `src/backend/AuthServer/Data/Entities/*`
- `src/backend/AuthServer/Data/Configurations/*`
- `src/backend/AuthServer/Data/Migrations/*`
- `src/backend/AuthServer/Services/DatabaseSeeder.cs`

Work:

- add tenant, membership, invite, and audit entities
- add EF configs and migrations
- replace seed roles/users/data with tenant-aware samples

### Step 2. Update role model and authorization derivation

Files/modules likely to change:

- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Services/*`
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- `src/backend/Shared/Extensions/HttpContextExtensions.cs`

Work:

- derive scopes from the active tenant role instead of a global user-role assumption
- add tenant claims and support/impersonation claims to tokens
- keep token-scoped claims minimal to one active tenant

### Step 3. Add memberships, switch, platform, and support APIs

Files/modules likely to change:

- `src/backend/AuthServer/Controllers/*`
- `src/backend/AuthServer/Models/*`
- `src/backend/AuthServer/Services/*`
- `src/backend/AuthServer/Program.cs`

Work:

- add `/api/me/tenants`
- add tenant switch endpoint
- add platform-admin tenant discovery and lifecycle APIs
- add direct support mode and impersonation APIs
- add tenant-local membership APIs
- add self-serve tenant creation API

### Step 4. Make Orders tenant-aware

Files/modules likely to change:

- `src/backend/Orders/Data/Entities/*`
- `src/backend/Orders/Data/Configurations/*`
- `src/backend/Orders/Data/OrdersDbContext.cs`
- `src/backend/Orders/Data/Migrations/*`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/Orders/Controllers/OrdersController.cs`

Work:

- add `TenantId`
- filter reads/writes by tenant
- update query semantics from user-owned to tenant-owned where required
- update uniqueness and indexes

### Step 5. Reshape web-app routing and session model

Files/modules likely to change:

- `src/web-app/app/*`
- `src/web-app/contexts/auth-context.tsx`
- `src/web-app/lib/auth/oauth2-auth.ts`
- `src/web-app/lib/api.ts`
- `src/web-app/components/*`

Work:

- split authenticated routes into global and `/t/{tenantSlug}/...`
- add memberships load and tenant switch flow
- land on default tenant dashboard after login
- auto-switch when navigating to another accessible tenant route
- add support-mode banner and exit control

### Step 6. Move admin UI to the right boundary

Files/modules likely to change:

- `src/web-app/app/(dashboard)/*`
- tenant-local admin pages under `/t/{tenantSlug}/...`
- new platform pages under global authenticated routes

Work:

- make tenant member management tenant-local
- keep global role catalog platform-managed
- keep global identity edits out of tenant-admin flows
- add tenant settings pages

### Step 7. Add tests and docs

Files/modules likely to change:

- `tests/backend/AuthServer.IntegrationTests/*`
- `tests/backend/Shared.UnitTests/*`
- new or extended Orders integration coverage
- `src/web-app/tests/e2e/*`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `README.md`

Work:

- add end-to-end tenant token tests
- add tenant isolation tests in Orders
- add route/switch/support-mode tests in web-app
- update architecture and extension docs

## 7. Testing Strategy

### Unit tests

- claims helpers for tenant/support/impersonation claims
- tenant guardrail helpers
- invite/membership validation helpers

### Integration tests

- `AuthServer`
  - default tenant token issuance
  - tenant switch exchange
  - direct support mode
  - impersonation with required reason
  - suspended tenant behavior
  - last-tenant-admin guardrail
- `Orders`
  - tenant read/write isolation
  - per-tenant uniqueness for customers/order numbers
  - tenant-wide visibility rules

### Frontend/e2e

- login lands on default tenant dashboard
- memberships endpoint drives tenant switcher
- route visit auto-switches tenant context
- support-mode banner renders in direct and impersonated sessions
- tenant-local user management behavior

### Edge cases

- user with no active memberships
- user with inactive default membership
- suspended tenant selected as default
- removing a membership used by a stale refresh token
- platform admin switching to unknown tenant or inactive user

## 8. Observability / Debuggability

- add full audit trail for:
  - tenant switch
  - direct support mode start/stop
  - impersonation start/stop
  - impersonation reason
- enrich logs/traces with:
  - tenant id
  - tenant slug
  - effective user id
  - actor user id when in support/impersonation mode
- ensure UI always displays support-mode state clearly

## 9. Rollout Plan

- implement in phases:
  1. AuthServer tenant foundation and seed reset
  2. tenant-scoped token/session APIs
  3. Orders isolation
  4. web-app routing and UI
  5. test/doc cleanup
- reset local/dev sample data as part of rollout
- keep deprecated `web-admin` out of implementation scope
- monitor token issuance/switch flows and tenant audit events after rollout

## 10. Risks & Mitigations

### Risk 1. Old global-role assumptions remain in live code

Mitigation:

- audit controllers/services for role assignment paths and remove dead assumptions in the same pass

### Risk 2. Tenant context leaks across APIs or UI state

Mitigation:

- keep tokens single-tenant
- use route-driven switch flow
- add explicit tenant claims helpers and tests

### Risk 3. Orders queries miss tenant filters

Mitigation:

- centralize tenant filter handling
- add integration coverage for cross-tenant access attempts

### Risk 4. Support mode becomes hard to audit

Mitigation:

- require impersonation reason
- persist dedicated audit rows
- show strong UI banner and exit controls

### Risk 5. Seed/sample data becomes inconsistent with the new role and tenant model

Mitigation:

- reset and reseed dev/test data in the same delivery pass
- update integration tests to use tenant-aware fixtures only

## 11. Open Questions

No blocking open questions remain for the current plan.

Deferred:

- full invite-acceptance UX and onboarding flow implementation
- future billing-specific lifecycle expansion beyond the chosen v1 states

## Sources Consulted

- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md`
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Data/AppDbContext.cs`
- `src/backend/AuthServer/Data/Entities/AppUser.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/Orders/Data/Entities/Order.cs`
- `src/backend/Orders/Data/Entities/Customer.cs`
- `src/backend/Orders/Data/Entities/IdempotencyRecord.cs`
- `src/backend/Orders/Data/Configurations/OrderConfiguration.cs`
- `src/backend/Orders/Data/Configurations/CustomerConfiguration.cs`
- `src/backend/Orders/Data/Configurations/IdempotencyRecordConfiguration.cs`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- `src/backend/Shared/Extensions/HttpContextExtensions.cs`
- `src/web-app/contexts/auth-context.tsx`
- `src/web-app/lib/auth/oauth2-auth.ts`
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`
