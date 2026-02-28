# User Management Plan Audit + Clarifications

Date: 2026-02-27
Status: Audited (pre-implementation)
Related:
- `ai-analysis/2026-02-26-user-management-page-authserver-plan.md`
- `ai-analysis/2026-02-26-user-management-page-authserver-implementation-checklist.md`

## Current implementation status (verified)

- `/users` route now exists only as a placeholder:
  - `src/web-admin/app/(authenticated)/users/page.tsx`
- Full user-management backend API is not implemented:
  - no `AdminUsersController`
  - no `IUserAdminService` / `UserAdminService`
- Frontend users API client and users components are not implemented:
  - no `src/web-admin/lib/admin-users-api.ts`
  - no `src/web-admin/components/admin/users/*`
- `/users/activity` page is still missing.

Conclusion:
- The plan/checklist are good, but implementation has not started beyond route placeholder.

## Plan quality assessment

What is good:
- Uses existing architecture patterns (`Result<T>`, thin controllers, services).
- Aligns permissions with current RBAC (`admin.users` OR `admin.full`).
- Includes tests and docs work (good coverage discipline).
- Includes self-disable guard (important safety check).

Potential gaps/clarifications needed before coding:

1. Pagination contract shape
- Backend list endpoint needs a concrete response schema (`items`, `page`, `pageSize`, `totalCount`, `totalPages`) to avoid frontend mismatch.

2. User profile fields scope
- Current `AppUser` has `Email`, `UserName`, `PhoneNumber`, `IsActive`, timestamps.
- Clarify if v1 includes only these fields (no first/last name) to avoid schema expansion now.

3. Create-user role assignment rule
- Resolved: require at least one explicit role on create; reject empty role list.

4. Role assignment constraints
- Resolved: reject assignment of inactive roles.

5. Self-protection scope
- Resolved:
  - self-disable is blocked
  - self-removal of admin-capable roles is blocked (lockout guard)

6. Activity page v1 scope
- Resolved: keep “coming soon” page and guard with `admin.users` or `admin.full`.

7. Audit trail requirements
- Clarify if we need to persist admin action audit records now (user created/disabled/password reset), or defer.

## Confirmed from follow-up

- Password reset semantics selected: token/link-based reset flow in v1.
- Create-user role rule selected: role list is required (no auto-default assignment).
- Role assignment constraint selected: inactive roles cannot be assigned.
- Self-protection selected: block self-disable and self-removal of admin-capable roles.
- Activity placeholder selected: keep route and guard with `admin.users`/`admin.full`.

## Recommendation before implementation

Lock the following defaults (unless changed):
- List response contract: `PagedResult<T>` with explicit fields.
- Editable fields in v1: `Email` (create only), `PhoneNumber`, `IsActive`, roles.
- Password reset: token/link-based reset flow in v1.
- Create role behavior: default to `User` when role list omitted.
- Reject assignment of inactive roles.
- Keep `/users/activity` behind same admin scopes and show “coming soon”.
- Defer persistent audit trail to v1.1 (but log actions server-side now).
