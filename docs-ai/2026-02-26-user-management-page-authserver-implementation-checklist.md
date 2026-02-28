# User Management Page + AuthServer Admin Users Implementation Checklist

Date: 2026-02-26
Status: Implemented (v1 core)
Related:
- `ai-analysis/2026-02-26-user-management-page-authserver-plan.md`

## Confirmed decisions

- Include role assignment directly in `/users` v1.
- Include admin password reset endpoint/UI in v1 (token/link-based reset flow).
- Block self-disable in API/UI.
- Block self-removal of admin-capable roles (`admin.users`/`admin.full`) in API/UI.
- Keep `/users/activity` nav item and add a simple “Coming soon” page in v1.
- Guard `/users/activity` with `admin.users` or `admin.full`.
- Require at least one explicit role on user creation.
- Reject inactive role assignment.

## Goal

Deliver user management in `web-admin` + `AuthServer` with create/edit/disable/role-assignment/password-reset flows, aligned to current architecture patterns (`Result<T>`, thin controllers, scope-based policies).

---

## A. Backend (`AuthServer`) - Admin Users API

### A1. Models (`Models/AdminUsers`)

- [x] Add `src/backend/AuthServer/Models/AdminUsers/AdminUserSummaryResponse.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/AdminUserDetailResponse.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/AdminRoleOptionResponse.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/CreateUserRequest.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/UpdateUserRequest.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/UpdateUserRolesRequest.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/ResetUserPasswordRequest.cs`
- [x] Add `src/backend/AuthServer/Models/AdminUsers/UpdateUserStatusRequest.cs`
- [x] Add pagination/query request/response model(s) for list endpoint (`AdminUserListResponse`)

Conventions:
- Use `sealed record` + `init` (+ `required` where applicable).

### A2. Service Interface + Implementation

- [x] Add `src/backend/AuthServer/Services/Interfaces/IUserAdminService.cs`
- [x] Add `src/backend/AuthServer/Services/UserAdminService.cs`

Service operations:
- [x] List users (search/filter/paging)
- [x] Get user detail
- [x] Create user
- [x] Update user profile/status fields
- [x] Update user roles (replace assignment)
- [x] Reset user password
- [x] List available roles for assignment

Rules:
- [x] Validate role names exist and reject inactive roles
- [x] Reject create-user requests with empty role list
- [x] Block self-disable
- [x] Block self-admin-role removal that would remove `admin.users`/`admin.full` from current user
- [x] Set `UpdatedAt` on updates
- [x] Keep errors mapped to existing `CommonErrors` patterns

### A3. Controller

- [x] Add `src/backend/AuthServer/Controllers/AdminUsersController.cs`

Endpoints:
- [x] `GET /api/admin/users`
- [x] `GET /api/admin/users/{userId}`
- [x] `GET /api/admin/users/available-roles`
- [x] `POST /api/admin/users`
- [x] `PUT /api/admin/users/{userId}`
- [x] `PUT /api/admin/users/{userId}/roles`
- [x] `PUT /api/admin/users/{userId}/status`
- [x] `PUT /api/admin/users/{userId}/password`
  - uses token/link-based reset flow (no direct plaintext set endpoint)

Controller style:
- [x] Match `AdminRolesController` result mapping pattern (`ToActionResult`, `ToProblem`)

### A4. Policy + DI wiring

- [x] Update `src/backend/AuthServer/Program.cs`
- [x] Add/register `AdminUsersManagement` policy (`admin.users` OR `admin.full`)
- [x] Register `IUserAdminService` in DI

### A5. Scope/client consistency check

- [x] Verify `admin.users` exists in `src/backend/AuthServer/appsettings.Scopes.json`
- [x] Verify `web-admin` client allows `admin.users` in `src/backend/AuthServer/appsettings.Clients.json`
- [x] No config changes unless mismatch found

---

## B. Frontend (`web-admin`) - Users Management UI

### B1. API client

- [x] Add `src/web-admin/lib/admin-users-api.ts`

Client methods:
- [x] list users
- [x] get detail
- [x] create user
- [x] update user
- [x] update user roles
- [x] update user status
- [x] reset password
- [x] list available roles

### B2. `/users` page

- [x] Add `src/web-admin/app/(authenticated)/users/page.tsx`
- [x] Guard with `AuthGuard` required scopes `['admin.users', 'admin.full']`
- [x] Add loading/error/empty states
- [x] Add search/filter/paging UI

### B3. UI components (`components/admin/users`)

- [ ] Add `users-list.tsx`
- [x] Add `create-user-dialog.tsx`
- [x] Add `edit-user-dialog.tsx`
- [ ] Add `user-roles-editor.tsx`
- [x] Add `user-status-toggle.tsx` (or inline status action)
- [x] Add `reset-password-dialog.tsx`

Behavior:
- [x] disable/enable with confirmation
- [x] block self-disable action in UI (mirror backend guard)
- [x] role assignment inline or dialog-based
- [x] password reset flow with validation/confirmation
  - token/link-based UX in v1

### B4. `/users/activity` placeholder

- [x] Add `src/web-admin/app/(authenticated)/users/activity/page.tsx`
- [x] Simple “Coming soon” content
- [x] Keep same auth guard (`admin.users` OR `admin.full`)

### B5. Navigation alignment

- [x] Update `src/web-admin/components/app-sidebar.tsx` if needed
- [x] Ensure `/users` and `/users/activity` active highlighting works

---

## C. Tests

### C1. Backend integration tests (`AuthServer`)

- [x] Extend `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`

Add tests:
- [x] admin can list/create/update users
- [x] non-admin forbidden on `/api/admin/users/*`
- [x] assign roles updates reflected in token scopes on refresh
- [x] disable user blocks subsequent authorize/refresh flows
- [x] self-disable blocked
- [x] password reset works (old password fails, new password succeeds)

### C2. Frontend Playwright tests

- [x] Add `src/web-admin/tests/users-management-page.spec.ts`

Scenarios:
- [x] renders users list
- [x] create user flow
- [ ] edit user flow
- [x] disable/enable flow
- [ ] role assignment flow
- [ ] password reset flow
- [ ] unauthorized scope route protection

---

## D. Documentation

- [x] Update `README.md`
- [x] Update `docs/ARCHITECTURE.md`
- [x] Update `docs/DEVELOPMENT.md`
- [x] Update `docs/FRONTEND.md`
- [x] Update `docs/TESTING.md`
- [ ] Update `docs/AGENTS.MD` if patterns/guardrails changed

Document:
- New `/api/admin/users/*` endpoints
- Required scopes (`admin.users` OR `admin.full`)
- `/users` page behavior
- Password reset + self-disable guard behavior

---

## E. Validation Checklist

### Build/test

- [x] `dotnet build src/backend/AuthServer/AuthServer.csproj`
- [x] `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`
- [x] `npx tsc --noEmit` in `src/web-admin`
- [x] `npx playwright test src/web-admin/tests/users-management-page.spec.ts`

### Manual smoke

- [ ] Login as admin -> `/users` works
- [ ] Create user + assign role + login with new user works
- [ ] Disable user blocks auth flows
- [ ] Self-disable prevented
- [ ] Password reset works end-to-end

---

## Implementation log

- Implemented backend API: `AdminUsersController` + `IUserAdminService`/`UserAdminService` + `Models/AdminUsers/*`.
- Implemented guards: self-disable blocked; self admin-management scope removal blocked; inactive role assignment rejected; create requires at least one role.
- Implemented frontend: `/users` management UI, dialog workflows (create/edit/roles/reset-password), and `/users/activity` placeholder with guard.
- Added tests:
  - backend integration: admin-users flows and guardrails in `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`
  - frontend Playwright: `src/web-admin/tests/users-management-page.spec.ts`
- Updated docs: `README.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/FRONTEND.md`, `docs/TESTING.md`.
