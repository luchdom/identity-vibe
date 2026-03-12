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
- Existing seed/sample data must be updated to use the new role naming and tenant-aware model.

## Token and session model

- Canonical tenant context for authorization: the token.
- Route/URL responsibility: selects or requests tenant context.
- Active tenant selection:
  - first session uses explicit default tenant
  - later navigation can auto-switch by route
- Token payload should include active tenant only, not all memberships.
- Tenant switch mechanism:
  - dedicated `AuthServer` switch endpoint
- Membership discovery:
  - `AuthServer` endpoint
  - returns all active switchable memberships for the signed-in identity

## Routing and frontend shape

- Active frontend target: `src/web-app`
- Deprecated frontend: `src/web-admin`
- Tenant route style: path-based, `/t/{tenantSlug}/...`
- Authenticated route split:
  - global authenticated routes
  - tenant-context routes
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
  - represented by dedicated invite record plus pending membership
  - full invite acceptance UX deferred for now

## Orders and business-data rules

- Orders visibility inside a tenant:
  - all tenant members can see all tenant orders
- Business-data uniqueness:
  - customer email unique per tenant
  - order number unique per tenant
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
- Sample/dev data migration:
  - hard reset sample/dev data
- Invite/onboarding implementation:
  - model now, acceptance flow later
