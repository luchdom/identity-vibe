# Multi-Tenant Column Chunked Execution Plan

Date: 2026-03-13
Status: Ready for implementation

Primary references:
- `docs-ai/2026-03-12-multi-tenant-column-plan.md`
- `docs-ai/2026-03-12-multi-tenant-column-clarification-decisions.md`
- `docs-ai/TASKS-multi-tenant-column-2026-03-12.md`
- `docs-ai/2026-03-12-multi-tenant-column-implementation-checklist.md`
- `docs-ai/2026-03-12-multi-tenant-column-audit.md`

## Audit Summary

The detailed task list is aligned with the plan and is safe to execute, but the work is too cross-cutting to deliver as one large implementation pass.

Recommended execution model:

- ship foundation before policies
- ship policies before tenant-aware frontend routing
- ship tenant-aware backend before broad UI migration
- keep each chunk independently testable

This plan groups the approved tasks into chunks with explicit done criteria so we can stop between slices without leaving the system conceptually broken.

## Execution Model

Each chunk should have:

- a narrow objective
- a bounded write surface
- clear prerequisites
- explicit done criteria
- a recommended validation pass before continuing

Recommended rule:

- do not begin the next chunk until the current chunk has a working, testable result in its own area

## Chunk 1: AuthServer Tenant Foundation

### Objective

Create the core tenant, membership, invite, and audit data model in `AuthServer`, and reset the seed model around tenants and new roles.

### Scope

- `AuthServer` schema
- `AuthServer` seeding
- no token or frontend behavior changes yet

### Tasks Included

- Task 1
- Task 2

### Key work

1. Add:
- `Tenant`
- `TenantMembership`
- `TenantInvite`
- support/impersonation audit entity

2. Add constraints and EF configurations:
- unique tenant slug
- unique user+tenant membership
- one role per membership
- default-membership rules

3. Reset seed roles:
- `PlatformAdmin`
- `TenantAdmin`
- `TenantUser`
- `TenantSupport`

4. Seed sample tenants, memberships, and defaults.

5. Model brand-new invited emails through `TenantInvite` only.

### Done criteria

- Migrations build and apply cleanly.
- Seeded environment starts with tenant-aware users and memberships.
- No schema ambiguity remains around invites vs memberships.
- Existing tests that depend on old role names are either updated or clearly isolated for the next chunk.

### Recommended validation

- run AuthServer startup with migrations
- inspect seeded tenant/membership rows
- verify no nullable `UserId` creep into `TenantMembership`

## Chunk 2: Token, Scope, and Context Contract

### Objective

Make tenant context canonical in tokens and settle the new scope families before broader API and frontend work depends on them.

### Scope

- token issuance
- token switch contract
- scope catalog and policies
- shared tenant/support claim helpers

### Tasks Included

- Task 3
- Task 3A

### Key work

1. Issue default-tenant-scoped tokens for normal login.

2. Implement tenant switch token exchange.

3. Guarantee every context change returns:
- fresh access token
- fresh refresh token
- old token pair discarded client-side

4. Introduce separate scope families:
- global platform scopes
- tenant-local scopes

5. Migrate current `admin.users`, `admin.roles`, and `admin.full` assumptions intentionally.

### Done criteria

- Token contains one active tenant context only.
- Default tenant login works for active memberships.
- Tenant switch contract is stable and returns a fresh token pair.
- Scope catalog, client permissions, backend policies, and frontend guards agree on the new scope families.
- Shared claims helpers understand tenant/support/impersonation context.

### Recommended validation

- AuthServer integration tests for:
  - default tenant issuance
  - tenant switch
  - inactive membership rejection
  - suspended tenant rejection
- inspect scope/policy config for leftover accidental `admin.*` dependencies

## Chunk 3: Platform and Tenant Administration APIs

### Objective

Expose the API surface needed for memberships discovery, tenant-local administration, platform tenant management, and support workflows.

### Scope

- `AuthServer` API layer
- service layer for memberships, support mode, impersonation, and self-serve tenant creation

### Tasks Included

- Task 4
- Task 5

### Key work

1. Add `GET /api/me/tenants`.

2. Add tenant-context membership APIs for:
- list
- invite/add
- role change
- deactivate
- remove

3. Add platform APIs for:
- tenant discovery
- lifecycle management
- self-serve creation

4. Add support-mode and impersonation APIs.

5. Enforce:
- last active `TenantAdmin` guardrail
- no tenant-admin global-profile edits
- required impersonation reason

### Done criteria

- Normal users can fetch all active switchable memberships.
- Tenant admins can manage tenant-local memberships only.
- Platform admins can discover tenants and enter direct or impersonated support mode.
- Audit records exist for support and impersonation actions.
- Existing global users attach immediately to a new tenant; new-email invites create invite records only.

### Recommended validation

- AuthServer integration tests for:
  - self-serve tenant creation
  - tenant member invite/attach
  - role change
  - deactivate/remove
  - last-admin guardrail
  - direct mode
  - impersonation

## Chunk 4: Orders Tenant Isolation

### Objective

Move `Orders` from user-owned visibility to tenant-owned visibility while preserving actor audit fields and per-tenant uniqueness.

### Scope

- `Orders` schema
- repository/service/controller behavior
- no major frontend UI migration yet

### Tasks Included

- Task 6
- Task 7

### Key work

1. Add `TenantId` to:
- `Orders`
- `Customers`
- `IdempotencyRecords`

2. Update uniqueness:
- customer email per tenant
- order number per tenant
- idempotency scoped by tenant

3. Change query visibility:
- all active tenant members can see all tenant orders

4. Preserve `Order.UserId` and related actor fields as audit metadata.

### Done criteria

- Cross-tenant reads and writes are blocked by tenant context.
- Orders are visible tenant-wide, per the approved model.
- `Order.UserId` is still preserved for creator/actor history.
- Per-tenant uniqueness is enforced by schema and code.

### Recommended validation

- Orders integration tests for:
  - cross-tenant denial
  - tenant-wide visibility
  - per-tenant customer email uniqueness
  - per-tenant order number uniqueness

## Chunk 5: web-app Session and Route Migration

### Objective

Move `web-app` to a tenant-aware route and session model before rebuilding the admin surfaces on top of it.

### Scope

- auth/session state
- route grouping
- redirects from old admin routes
- tenant switcher
- support-mode banner

### Tasks Included

- Task 8
- Task 9
- Task 10

### Key work

1. Extend auth state with:
- active tenant
- memberships list
- switched token pair handling

2. Split routes into:
- global authenticated routes
- `/t/{tenantSlug}/...` tenant routes

3. Redirect legacy routes:
- `/users` -> current tenant members route
- `/users/roles` -> platform-only route

4. Add tenant switcher and auto-switch by route.

5. Add strong support-mode banner and exit action.

### Done criteria

- Login lands on default tenant dashboard.
- Route-based tenant switching works.
- Support mode is visually obvious.
- Legacy global member-management routes are redirected or replaced intentionally.
- No stale token-pair handling remains in normal switch/support transitions.

### Recommended validation

- web-app e2e for:
  - login landing
  - route-based auto-switch
  - switcher behavior
  - support banner
  - legacy route redirects

## Chunk 6: web-app Admin Surface Migration

### Objective

Move tenant-local member management and platform-global administration into their new UI boundaries on top of the tenant-aware routing model.

### Scope

- tenant-local member management screens
- platform admin screens
- navigation and dashboard shortcut cleanup

### Tasks Included

- Task 11
- Task 12

### Key work

1. Build tenant-local member management under tenant routes.

2. Keep global role catalog management platform-only.

3. Add platform admin screens for:
- tenant discovery
- lifecycle actions
- support mode entry

4. Update nav and dashboard links away from obsolete global admin assumptions.

### Done criteria

- Tenant admins manage members only inside their tenant context.
- Platform admins access global management screens outside tenant routes.
- Role catalog is clearly platform-only.
- Dashboard links and nav config no longer point tenant admins into the wrong surfaces.

### Recommended validation

- web-app e2e for:
  - tenant member management
  - platform-only route guards
  - support-mode entry points

## Chunk 7: Hardening, Fixtures, and Docs

### Objective

Finish the migration cleanly with tests, tenant-aware fixtures, and repo doc alignment.

### Scope

- tests
- sample/dev data cleanup
- docs

### Tasks Included

- Task 13
- Task 14
- Task 15

### Key work

1. Add/extend backend tests:
- AuthServer integration
- Shared unit tests
- Orders integration

2. Replace obsolete dev/test fixture assumptions with tenant-aware data.

3. Update docs:
- architecture
- development
- testing
- AGENTS
- README

### Done criteria

- Tenant-aware backend tests pass or remaining failures are explicitly understood.
- Sample/dev data matches the new tenant model.
- Docs reflect `web-app` as the active frontend target and explain tenant-scoped auth/session behavior.
- No major references remain that describe the old global admin model as current behavior.

### Recommended validation

- targeted backend test runs
- targeted `web-app` e2e runs
- doc review for auth, routing, and tenant-admin/platform-admin terminology consistency

## Recommended Stop Points

These are the safest pause points for review or PR boundaries:

1. After Chunk 1
   - schema and seeds are stable

2. After Chunk 2
   - token and scope contract is stable

3. After Chunk 4
   - backend multitenancy is enforceable end-to-end

4. After Chunk 5
   - frontend route/session model is tenant-aware

5. After Chunk 7
   - migration is hardened and documented

## Recommended Delivery Boundaries

If we want clean, reviewable implementation slices, the practical boundaries are:

1. AuthServer tenant schema + seed reset
2. Tenant-scoped tokens + scope-family migration
3. Membership/platform/support APIs
4. Orders tenant isolation
5. web-app tenant route/session migration
6. web-app tenant/platform admin surfaces
7. Tests, fixtures, and docs

## Final Recommendation

The tasks should be executed in chunks, not as one continuous implementation pass.

Most important sequencing rule:

- treat Chunk 2 as the contract-setting chunk
- do not start broad frontend or API-surface migration until token and scope semantics are settled there

That gives us a stable base for everything that follows and keeps rework down.
