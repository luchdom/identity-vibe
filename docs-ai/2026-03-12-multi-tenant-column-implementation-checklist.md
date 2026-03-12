# Multi-Tenant Column Model Implementation Checklist

Date: 2026-03-12

Use this as the condensed execution checklist. The detailed task breakdown lives in `docs-ai/TASKS-multi-tenant-column-2026-03-12.md`.

## Phase 1. AuthServer foundation

- [ ] Add `Tenant` entity with `Guid` id, immutable slug, display fields, and lifecycle state.
- [ ] Add `TenantMembership` entity with `UserId`, `TenantId`, `RoleId`, `IsActive`, and `IsDefault`.
- [ ] Add `TenantInvite` entity for pending new-email invites.
- [ ] Add support/impersonation audit entity.
- [ ] Add EF configurations, indexes, and migrations.
- [ ] Reset seed roles to `PlatformAdmin`, `TenantAdmin`, `TenantUser`, `TenantSupport`.
- [ ] Seed sample tenants, memberships, and default memberships.

## Phase 2. Tenant-scoped auth/session flows

- [ ] Issue tenant-scoped tokens for the default active tenant.
- [ ] Add tenant claims and tenant-role-derived scopes to the token.
- [ ] Add `GET /api/me/tenants`.
- [ ] Add tenant switch endpoint in `AuthServer`.
- [ ] Block switch/issuance for inactive memberships and suspended tenants.
- [ ] Add platform tenant discovery APIs.
- [ ] Add self-serve tenant creation API.

## Phase 3. Support mode and impersonation

- [ ] Add direct support-mode entry for `PlatformAdmin`.
- [ ] Add impersonation entry with required reason.
- [ ] Encode actor/effective-user metadata into support/impersonation tokens.
- [ ] Audit-log all support-mode and impersonation actions.
- [ ] Add exit/stop APIs for support and impersonation sessions.

## Phase 4. Tenant-local administration

- [ ] Add tenant-context membership list APIs.
- [ ] Add tenant-context invite/add-member APIs.
- [ ] Add tenant-context role-change APIs.
- [ ] Add tenant-context deactivate/remove membership APIs.
- [ ] Enforce last-active-tenant-admin guardrail.
- [ ] Prevent tenant-admin mutation of global identity fields.

## Phase 5. Orders tenant isolation

- [ ] Add `TenantId` to `Orders`.
- [ ] Add `TenantId` to `Customers`.
- [ ] Add `TenantId` to `IdempotencyRecords`.
- [ ] Change customer-email uniqueness to per-tenant.
- [ ] Change order-number uniqueness to per-tenant.
- [ ] Scope idempotency uniqueness by tenant.
- [ ] Update repository/service/controller logic to enforce tenant filters everywhere.
- [ ] Keep tenant-wide order visibility for all active tenant members.

## Phase 6. web-app routing and session UX

- [ ] Split authenticated routes into global and `/t/{tenantSlug}/...` areas.
- [ ] Load memberships from `AuthServer` after login.
- [ ] Land on default tenant dashboard after login.
- [ ] Auto-switch tenant context when navigating to another accessible tenant route.
- [ ] Add tenant switcher UI.
- [ ] Add strong support-mode banner with visible exit action.

## Phase 7. web-app admin surfaces

- [ ] Move tenant member management into tenant-context routes.
- [ ] Keep global role catalog management platform-only.
- [ ] Add platform admin global screens for tenant discovery and lifecycle actions.
- [ ] Add self-serve create-tenant flow in `web-app`.
- [ ] Exclude deprecated `web-admin` from implementation scope.

## Phase 8. Test and docs cleanup

- [ ] Update AuthServer integration tests for tenant issuance, switch, support mode, impersonation, and suspension.
- [ ] Add Orders integration coverage for cross-tenant isolation.
- [ ] Add shared unit tests for tenant/support/impersonation claim helpers.
- [ ] Add `web-app` e2e coverage for routing, switching, banners, and tenant-local admin flows.
- [ ] Update `README.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/TESTING.md`, and `docs/AGENTS.MD`.
- [ ] Verify local startup still works with tenant-aware sample data.

## Completion criteria

- [ ] Global identity is preserved in `AuthServer`.
- [ ] Tenant membership is the source of tenant-local access.
- [ ] Tokens are single-tenant and route-driven switching works.
- [ ] `Orders` is tenant-isolated by column.
- [ ] `web-app` is the only active frontend target for the feature.
- [ ] Platform support mode is explicit, audited, and reversible.
