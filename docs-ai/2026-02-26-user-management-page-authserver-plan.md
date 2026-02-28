# User Management Page + AuthServer Admin Users API Plan

Date: 2026-02-26
Status: Planned

## Audit Summary (what exists vs missing)

### Existing

- Frontend has a `User Management` nav group with links to:
  - `/users`
  - `/users/roles`
  - `/users/activity`
- Fully implemented Roles & Permissions flow:
  - Frontend page: `src/web-admin/app/(authenticated)/users/roles/page.tsx`
  - Backend API: `src/backend/AuthServer/Controllers/AdminRolesController.cs`
  - Service pattern: `IRoleAdminService` + `RoleAdminService` + `Result<T>`
- Auth scope model already includes:
  - `admin.users`
  - `admin.roles`
  - `admin.full`
- `web-admin` client is already allowed to receive `admin.users`.

### Missing (for your request)

- No backend admin users endpoints (`/api/admin/users/*`) currently exist.
- No frontend `/users` page implementation currently exists.
- No user management API client in frontend for users CRUD/disable flows.
- No tests for admin users management flow.

Conclusion:
- We need a new Admin Users backend slice and a new frontend `/users` page.

---

## Scope Recommendation (v1)

Implement v1 with:
- list/search/filter users
- create user
- edit basic profile fields
- enable/disable user (`IsActive`)
- assign roles to user
- no hard delete

Keep scope manageable:
- no full activity/audit UI yet
- include token/link-based admin password reset flow in v1

---

## Architecture Alignment

Follow the existing pattern used by `AdminRoles`:

- Controller: thin, maps `Result<T>` to HTTP responses
- Service: business logic
- Models: dedicated folder (`Models/AdminUsers`)
- Policy-based authorization (`admin.users` OR `admin.full`)
- EF/Identity usage in service layer (`AppDbContext`, `UserManager`, `RoleManager`)

No new EF entity is strictly required for v1 user management because `AppUser` already has:
- `Email`, `PhoneNumber`, `IsActive`, `CreatedAt`, `UpdatedAt`
- role assignments via `AspNetUserRoles`

Optional future extension:
- add profile fields (FirstName/LastName/DisplayName) via `AppUser` migration if needed for richer UI.

---

## Backend Plan (AuthServer)

## Step 1. Add Admin Users Models

Create `src/backend/AuthServer/Models/AdminUsers/`:

- `AdminUserSummaryResponse.cs`
  - `Id`, `Email`, `UserName`, `PhoneNumber`, `IsActive`, `Roles`, `CreatedAt`, `UpdatedAt`
- `AdminUserDetailResponse.cs`
  - same + optional metadata for edit view
- `AdminRoleOptionResponse.cs`
  - role list for assignment dropdown/checklist (`Id`, `Name`, `IsActive`)
- `CreateUserRequest.cs`
  - `Email`, `Password`, `PhoneNumber?`, `RoleNames[]`, `IsActive`
- `UpdateUserRequest.cs`
  - `PhoneNumber?`, `IsActive`
- `UpdateUserRolesRequest.cs`
  - `RoleNames[]`
- `ListUsersQueryRequest.cs` (optional; or controller query params directly)
  - `Search`, `IsActive`, `Role`, `Page`, `PageSize`

Modeling style:
- use `sealed record` + `init/required` where appropriate (current repo convention).

## Step 2. Add Service Interface and Implementation

Add:
- `src/backend/AuthServer/Services/Interfaces/IUserAdminService.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`

Methods (suggested):
- `Task<Result<PagedResult<AdminUserSummaryResponse>>> ListUsersAsync(...)`
- `Task<Result<AdminUserDetailResponse>> GetUserAsync(string userId)`
- `Task<Result<AdminUserDetailResponse>> CreateUserAsync(CreateUserRequest request)`
- `Task<Result<AdminUserDetailResponse>> UpdateUserAsync(string userId, UpdateUserRequest request)`
- `Task<Result<AdminUserDetailResponse>> UpdateUserRolesAsync(string userId, UpdateUserRolesRequest request)`
- `Task<Result<AdminUserDetailResponse>> SetUserStatusAsync(string userId, bool isActive)` (optional separate endpoint)
- `Task<Result<IReadOnlyList<AdminRoleOptionResponse>>> ListAssignableRolesAsync()`

Implementation notes:
- Use `UserManager<AppUser>` for create/update password hashing flows.
- Use `RoleManager<AppRole>` + `UserManager` role methods to sync user roles.
- Validate role names exist and active-only assignment rule (recommended).
- Update `UpdatedAt` on profile/status/role changes.
- Return `Result<T>` with consistent error codes (`NOT_FOUND`, `VALIDATION_FAILED`, `CONFLICT`, etc.).

Guardrails:
- prevent disabling your own current account (recommended safety)
- optionally prevent removing last active admin role holder (optional v1 guard)

## Step 3. Add Admin Users Controller

Add:
- `src/backend/AuthServer/Controllers/AdminUsersController.cs`

Route:
- `[Route("api/admin/users")]`

Auth:
- `[Authorize(Policy = AdminUsersManagementPolicy)]`

Endpoints (recommended):
- `GET /api/admin/users` (list/search/filter/paging)
- `GET /api/admin/users/{userId}` (detail)
- `GET /api/admin/users/available-roles` (role options for assignment)
- `POST /api/admin/users` (create)
- `PUT /api/admin/users/{userId}` (edit basic fields)
- `PUT /api/admin/users/{userId}/roles` (replace roles)
- `PUT /api/admin/users/{userId}/status` (enable/disable)

Response pattern:
- same style as `AdminRolesController` (`ToActionResult`, `ToProblem`).

## Step 4. Wire policy + DI in Program

Update `src/backend/AuthServer/Program.cs`:

- Add policy constant and policy registration:
  - `admin.users` OR `admin.full`
- Register service:
  - `builder.Services.AddScoped<IUserAdminService, UserAdminService>();`

No client/scope config changes expected (already present), but verify:
- `appsettings.Scopes.json` includes `admin.users`
- `appsettings.Clients.json` web-admin includes `admin.users`

## Step 5. (Optional) EF model/migration changes

For v1 as requested, no new entity/table required.

Only add migration if:
- you decide to add new user profile columns (FirstName/LastName/DisplayName), or
- you add audit/activity table now.

---

## Frontend Plan (`web-admin`)

## Step 6. Add Admin Users API client

Add:
- `src/web-admin/lib/admin-users-api.ts`

Include typed calls for all admin user endpoints.
Pattern:
- match `admin-roles-api.ts` style
- use `authApi`
- request/response TypeScript interfaces colocated in this file.

## Step 7. Build `/users` page

Add:
- `src/web-admin/app/(authenticated)/users/page.tsx`

Use:
- `AuthGuard` with `requiredScopes={['admin.users', 'admin.full']}`
- local state + `authApi` pattern (consistent with current app)

UI sections:
- header + summary metrics (total users, active users, disabled users)
- search/filter controls
- users table
- create user dialog
- edit user dialog/sheet
- disable/enable action with confirmation
- roles assignment editor per user

Prefer existing UI blocks already present:
- `card`, `table`/`data-table`, `dialog`, `input`, `select`, `badge`, `button`, `tooltip`.

## Step 8. Add reusable user management components

Suggested files:
- `src/web-admin/components/admin/users/users-list.tsx`
- `src/web-admin/components/admin/users/create-user-dialog.tsx`
- `src/web-admin/components/admin/users/edit-user-dialog.tsx`
- `src/web-admin/components/admin/users/user-roles-editor.tsx`
- optional: `src/web-admin/components/admin/users/user-status-toggle.tsx`

## Step 9. Navigation cleanup

Update `src/web-admin/components/app-sidebar.tsx`:
- ensure `/users` route is active-highlighted correctly
- keep `/users/roles` link
- for `/users/activity`:
  - either hide in v1 or add placeholder page to avoid dead navigation

---

## Testing Plan

## Step 10. Backend integration tests (AuthServer)

Extend:
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`

Add tests:
- admin token can list users
- non-admin token forbidden on `/api/admin/users`
- create user works and can login/authorize afterwards
- update user works
- disable user blocks authorize flow (redirect to login) and refresh token grant (`invalid_grant`)
- role assignment update changes scopes on refresh
- invalid role assignment returns validation error

## Step 11. Frontend Playwright tests

Add:
- `src/web-admin/tests/users-management-page.spec.ts`

Mock API tests:
- page loads and renders user rows
- create user flow
- edit user flow
- disable/enable action flow
- role update flow
- forbidden scope redirect (`/unauthorized`) when missing `admin.users`

---

## Docs Plan

## Step 12. Documentation updates

Update:
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/FRONTEND.md`
- `docs/TESTING.md`
- `docs/AGENTS.MD` (if needed for new admin users API conventions)

Document:
- new `AuthServer` admin users endpoints
- required scopes (`admin.users` or `admin.full`)
- frontend route `/users`
- test commands and expected behavior
- note that disable is soft-deactivation (`IsActive=false`) not hard delete

---

## Rollout Strategy (incremental)

### Phase A
- Backend API + tests first
- validate with HTTP client and integration tests

### Phase B
- Frontend page/components + Playwright tests

### Phase C
- Docs + final smoke checks

This keeps risk low and gives working checkpoints.

---

## Open Questions (need your confirmation before implementation)

1. Role assignment on Users page:
- Option A (Recommended): include role assignment in v1 (`/users` can change user roles)
- Option B: keep role assignment only in `/users/roles` page and user page handles only profile/status

2. Password management in v1:
- Option A (Recommended): create user with initial password only; no reset-password endpoint/UI yet
- Option B: include admin password reset endpoint/UI now

3. Safety rule for self-management:
- Option A (Recommended): do not allow admin to disable their own account from the user management page/API
- Option B: allow self-disable (risk of lockout)

4. `User Activity` nav item:
- Option A (Recommended): hide temporarily until implemented
- Option B: keep and add simple “Coming soon” page

## Confirmed decisions

- Include role assignment directly in `/users` v1.
- Include admin password reset endpoint/UI in v1 using token/link-based reset semantics.
- Block self-disable in user management API/UI.
- Block self-removal of admin-capable roles (`admin.users`/`admin.full`) to avoid lockout.
- Keep `/users/activity` nav item and add a simple “Coming soon” page in v1.
- Guard `/users/activity` with `admin.users` or `admin.full` (same as `/users`).
- Require at least one explicit role when creating a user (reject empty role list).
- Reject assignment of inactive roles.
