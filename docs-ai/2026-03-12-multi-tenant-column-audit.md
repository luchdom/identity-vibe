# Multi-Tenant Column Model Audit

Date: 2026-03-12

Related:
- `docs-ai/2026-03-12-multi-tenant-column-plan.md`
- `docs-ai/2026-03-12-multi-tenant-column-clarification-decisions.md`
- `docs-ai/TASKS-multi-tenant-column-2026-03-12.md`
- `docs-ai/2026-03-12-multi-tenant-column-implementation-checklist.md`

## Audit Summary

The plan is now materially stronger and internally consistent with the clarified product choices:

- global identity
- tenant-local access management
- one active tenant context per token
- path-based tenant routes in `web-app`
- dual support mode for `PlatformAdmin`
- column-based tenant isolation in `Orders`
- fresh token pair per context switch

The earlier blocking ambiguities have been resolved in the plan, decisions doc, and task breakdown. At this point the work is implementation-ready from a planning perspective. What remains are normal implementation watchpoints, not plan blockers.

## Findings

### 1. The previously blocking gaps are resolved

Status: Resolved

Resolved clarifications now captured in the docs:

- Brand-new invited emails now use `TenantInvite` only until acceptance; `TenantMembership` remains non-nullable on `UserId`.
- Global platform scopes and tenant-local scopes are now explicitly separated, and old `admin.*` assumptions are flagged for migration/deprecation.
- Current `/users` and `/users/roles` route behavior is now explicitly migrated:
  - `/users` redirects to tenant-local members
  - `/users/roles` becomes a platform-only route
- `Order.UserId` is now explicitly retained as creator/actor audit metadata, not as the visibility boundary.
- Every tenant switch, direct support entry, and impersonation entry now issues a fresh access token and refresh token, with the client discarding the old pair immediately.

Evidence:

- `docs-ai/2026-03-12-multi-tenant-column-clarification-decisions.md`
- `docs-ai/2026-03-12-multi-tenant-column-plan.md`
- `docs-ai/TASKS-multi-tenant-column-2026-03-12.md`

### 2. Scope-family names are still an implementation detail, but no longer a planning blocker

Severity: Low

Why it matters:

- The plan now correctly requires separate global and tenant-local scope families.
- It still leaves the exact scope names to implementation, which is acceptable as long as Task `3A` resolves them before coding touches policy guards broadly.

Recommendation:

- Decide the concrete scope catalog at the start of implementation Task `3A`, and update:
  - `appsettings.Scopes.json`
  - `appsettings.Clients.json`
  - `Program.cs` policies
  - frontend permission helpers

### 3. Invite-acceptance UX remains intentionally deferred and should stay fenced

Severity: Low

Why it matters:

- The data model is now clear, but the plan still intentionally defers the acceptance flow itself.
- That is fine, but implementation should not accidentally drift into half-built invite UX.

Recommendation:

- Keep the v1 implementation limited to:
  - invite record creation
  - pending/invited state visibility where needed
  - no partial email/token acceptance UI unless explicitly re-scoped

## Audit Result

Status: implementation-ready.

The prior blocking issues have been addressed in the planning artifacts. The remaining notes are execution watchpoints, not reasons to stop before implementation.

## Sources Consulted

- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md`
- `docs-ai/2026-03-12-multi-tenant-column-plan.md`
- `docs-ai/2026-03-12-multi-tenant-column-clarification-decisions.md`
- `docs-ai/TASKS-multi-tenant-column-2026-03-12.md`
- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/Orders/Data/Entities/Order.cs`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/Shared/Authorization/Extensions/AuthorizationExtensions.cs`
- `src/web-app/components/auth/auth-guard.tsx`
- `src/web-app/components/app-shell/nav-config.ts`
- `src/web-app/app/(dashboard)/dashboard/page.tsx`
- `src/web-app/app/(dashboard)/users/page.tsx`
- `src/web-app/app/(dashboard)/users/roles/page.tsx`
- `src/web-app/lib/auth/oauth2-auth.ts`
- `src/web-app/lib/permissions/authz.ts`
- `src/web-app/tests/e2e/loading-ux-admin.spec.ts`
