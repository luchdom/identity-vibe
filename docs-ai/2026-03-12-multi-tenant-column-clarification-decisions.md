# Multi-Tenant Column Model Clarification Decisions

Date: 2026-03-12

This document captures the decisions resolved during clarification for the multi-tenant feature plan.

## Core tenancy model

- Tenant model: column-based multi-tenancy, not schema-per-tenant.
- Identity model: global identity in `AuthServer`.
- Tenant access model: tenant-local access management.
- User-to-tenant relationship: a user can belong to multiple tenants.
- Tenant membership status: active/inactive membership.
- Membership role cardinality: exactly one role per membership.
- Role catalog: global catalog, tenant-local assignment.
- Membership role storage: `TenantMembership.RoleId`.

## Roles and permissions

- Tenant-local roles:
  - `TenantAdmin`
  - `TenantUser`
  - `TenantSupport`
- Global platform role:
  - `PlatformAdmin`
- Permission derivation:
  - global role-to-scope mapping
  - evaluated for the active tenant role
- Scope model:
  - separate global platform scope family
  - separate tenant-local scope family
  - existing `admin.users`, `admin.roles`, and `admin.full` must be explicitly migrated or deprecated
- Existing seed/sample data must be updated to use the new role naming and tenant-aware model.

## Token and session model

- Canonical tenant context for authorization: the token.
- Route/URL responsibility: selects or requests tenant context.
- Active tenant selection:
  - first session uses explicit default tenant
  - later navigation can auto-switch by route
  - if the default tenant is no longer usable, `AuthServer` auto-falls back to another active membership
  - if no active memberships remain, token issuance/refresh fails
- Token payload should include active tenant only, not all memberships.
- Canonical tenant/support claim contract uses flat explicit custom claims:
  - `tenant_id`
  - `tenant_slug`
  - `tenant_role`
  - `effective_user_id`
  - `actor_user_id`
  - `support_mode`
  - `impersonation_mode`
- Tenant switch mechanism:
  - dedicated `AuthServer` switch endpoint
  - request selector is `tenantId` only
- Context-switch token behavior:
  - every tenant switch, direct support entry, or impersonation entry issues a fresh access token and fresh refresh token
  - each token pair is bound to one exact active context
  - client discards the previous token pair immediately
- Membership discovery:
  - `AuthServer` endpoint
  - returns all active switchable memberships for the signed-in identity
  - response shape includes:
    - `memberships[]`
    - `activeTenantId`
    - `defaultTenantId`

## Routing and frontend shape

- Active frontend target: `src/web-app`
- Deprecated frontend: `src/web-admin`
- Tenant route style: path-based, `/t/{tenantSlug}/...`
- Authenticated route split:
  - global authenticated routes
  - tenant-context routes
- Existing route migration:
  - `/users` redirects to the current tenant members page
  - `/users/roles` becomes a platform-only global route
- Post-login landing:
  - default tenant dashboard
- Cross-tenant route behavior:
  - auto-switch tenant context if the user is an active member

## Tenant lifecycle and identifiers

- Tenant key model:
  - `Guid` primary key
  - immutable unique slug/code
- Tenant lifecycle model:
  - `Pending`
  - `Active`
  - `Suspended`
  - `Archived`
- Self-serve tenant creation:
  - enabled in v1
  - new tenant starts `Active`
  - creator becomes `TenantAdmin`

## Platform admin support model

- Platform-admin access model: dual mode
  - direct tenant mode
  - impersonation mode
- Direct tenant mode access: full tenant access
- Impersonation target scope: any active user in the tenant
- Impersonation token identity:
  - effective user
  - actor metadata
- Impersonation reason: required
- UI indication:
  - strong persistent banner
  - one-click visible exit control
- Audit requirement:
  - full audit trail for switch, direct mode, and impersonation

## Tenant administration

- Tenant creation/management: hybrid
  - platform-admin managed
  - self-serve tenant creation also supported
- Tenant-admin authority:
  - can fully manage users in their tenant
  - can grant/remove `TenantAdmin`
  - can edit tenant metadata for their own tenant
  - cannot edit global identity fields like email/phone
- Last tenant admin guardrail:
  - block removal/deactivation of the last active `TenantAdmin`

## Membership invite and onboarding model

- Tenant admin can invite by email.
- Existing global user invited to another tenant:
  - attach existing identity
  - membership becomes active immediately
- Brand-new invited email:
  - represented by dedicated invite record only until acceptance
  - no `TenantMembership` row exists before acceptance
  - `TenantInvite` stores the intended tenant and role assignment
  - full invite acceptance UX deferred for now

## Orders and business-data rules

- Orders visibility inside a tenant:
  - all tenant members can see all tenant orders
- `Order.UserId` meaning after tenant rollout:
  - creator/actor audit field only
  - not the tenant visibility boundary
- Business-data uniqueness:
  - customer email unique per tenant
  - order number unique per tenant
  - idempotency key unique per tenant via `TenantId + IdempotencyKey`
- Orders isolation model:
  - tenant-scoped by column via `TenantId`

## Revocation, suspension, and trust boundaries

- Membership removal/deactivation effect:
  - invalidate on next refresh/switch
- Downstream API trust model:
  - trust JWT until expiry
- Suspended tenant behavior:
  - block new token issuance and tenant switching
  - existing tokens continue until expiry

## Migration and rollout decisions

- v1 scope: end-to-end for current active surfaces
  - `AuthServer`
  - `Orders`
  - `web-app`
- Before implementation begins, the docs must define exact API DTOs and the final frontend route map.
- Sample/dev data migration:
  - hard reset sample/dev data
- Invite/onboarding implementation:
  - model now, acceptance flow later
