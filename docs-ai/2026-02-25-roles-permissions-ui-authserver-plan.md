# Roles & Permissions UI + AuthServer RBAC Admin Implementation Plan

Date: 2026-02-25
Status: Planning (pre-implementation)
Related:
- `ai-analysis/2026-02-24-rbac-role-scope-token-policy-plan.md`
- `ai-analysis/2026-02-24-rbac-role-scope-token-policy-implementation-checklist.md`

## Goal
Add a frontend admin page to:
- create a new Role
- view/edit which permission Scopes are assigned to a Role

And add the backend/AuthServer support to persist and serve this safely, including:
- EF models/tables for editable role->scope assignments
- admin APIs for roles + role scope assignments
- auth/RBAC integration updates so token issuance uses DB-managed role scopes
- tests (backend + frontend)

## High-level recommendation (best approach)

### Source-of-truth split (recommended)
Use a **hybrid model**:
- **Scope catalog/metadata** (name/displayName/description/resources): keep config/OpenIddict-seeded (`appsettings.Scopes.json` + OpenIddict scopes)
- **Role definitions** (Name, Description, Active): DB (`AspNetRoles` / `AppRole`)
- **Role -> Scope assignments**: DB (new table)

Why this is the best fit here:
- You need runtime editing via UI (config-only role mappings won’t work)
- Scope metadata and OpenIddict scope seeding already exist and are stable
- Minimal change to existing scope catalog pipeline while enabling editable permissions
- Keeps token issuance fast and deterministic

### UI layout recommendation
Build a dedicated page under:
- `src/web-admin/app/(authenticated)/users/roles/page.tsx`

Use shadcn primitives already in project + a few additions:
- Existing: `card`, `table`, `dialog`, `form`, `collapsible`, `badge`, `input`, `select`, `button`
- Add (recommended): `checkbox`, `tabs`, `scroll-area`, `alert-dialog` (confirm destructive actions later), optionally `command` for scope search/filter

For the scope editor, use a **grouped collapsible checklist** (tree-like) rather than a strict tree component.
- shadcn does not provide a first-party `TreeView` component in the main component list.
- The `Collapsible` docs include a file-tree style pattern, which is a good fit for grouped scopes.
- Group by scope prefix (e.g. `orders.*`, `profile.*`, `admin.*`) and show leaf checkboxes.

## Current codebase findings (relevant to this feature)

### Frontend
- There is a placeholder nav item for roles/permissions:
  - `src/web-admin/components/app-sidebar.tsx` -> `/users/roles`
- No actual route/page exists yet for `/users/roles`
- `web-admin` already uses shadcn-style components and react-hook-form
- No frontend data-fetching library (React Query/SWR) is currently used; axios + local state patterns are used
- Current frontend guards are role-based, but this feature should move page/feature access checks to scopes for RBAC alignment

### AuthServer backend
- No admin controller exists for role CRUD / role-scope assignment management
- `ScopeController` only exposes read/validation endpoints, not editable role mappings
- Role->scope mapping currently comes from config only:
  - `appsettings.Scopes.json` (`ScopeConfiguration:UserScopes:RoleScopes`)
- Token issuance currently uses `ScopeConfigurationService`, which reads config role mappings
- Seeder uses `EnsureCreated()` and the migrations folder is effectively empty

### Key implication
If we want UI edits to persist, we must move **role->scope assignment lookup** for token issuance from config to DB (while keeping config as seed/bootstrap defaults).

## Detailed implementation plan

## 1) Backend/AuthServer: persistence model for editable role->scope assignments

### 1.1 Add EF entity for role-scope assignment (new table)
Files (new):
- `src/backend/AuthServer/Data/Entities/AppRoleScope.cs`
- `src/backend/AuthServer/Data/Configurations/AppRoleScopeConfiguration.cs`

Suggested model:
- `RoleId` (`string`) FK -> `AppRole.Id`
- `ScopeName` (`string`) (references known scope name from scope catalog / OpenIddict scopes)
- `AssignedAt` (`DateTime`)
- `AssignedBy` (`string?`) optional (future audit support)

Suggested constraints:
- Composite PK: (`RoleId`, `ScopeName`)
- Index on `ScopeName` (optional, useful for queries/reporting)
- FK cascade delete from role to assignments

Notes:
- Avoid duplicating scope metadata in this table.
- Treat `ScopeName` as a stable identifier tied to configured scope catalog.

### 1.2 Add DbSet and role navigation (optional but recommended)
Files:
- `src/backend/AuthServer/Data/AppDbContext.cs`
- `src/backend/AuthServer/Data/Entities/AppRole.cs`

Tasks:
- Add `DbSet<AppRoleScope>`
- Add navigation collection on `AppRole` (optional but helpful)

## 2) Backend/AuthServer: migrate role-scope lookup from config to DB (with bootstrap fallback)

### 2.1 Introduce a dedicated role-permission provider/service
Files (new):
- `src/backend/AuthServer/Services/Interfaces/IRoleScopeService.cs`
- `src/backend/AuthServer/Services/RoleScopeService.cs`

Responsibilities:
- Get all available scopes from config scope catalog (`ScopeConfigurationService`) for UI display/validation (v1)
- Get role assignments from DB
- Replace role assignments for a role (transactional)
- Validate assigned scope names exist in catalog
- Compute effective user permission scopes from user roles (DB role assignments)

Why a separate service:
- Keeps `ScopeConfigurationService` focused on config scope catalog
- Keeps DB assignment logic isolated and testable
- Makes token issuance integration straightforward

### 2.2 Update token issuance to use DB role-scope assignments
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Services/AuthorizationGrantService.cs`
- `src/backend/AuthServer/Services/ScopeConfigurationService.cs` (or leave as scope catalog only)

Tasks:
- Replace current user permission scope lookup source (currently config role mapping) with `IRoleScopeService` DB-backed effective scope calculation.
- Keep protocol scope handling unchanged (request-driven as you decided).
- Preserve PKCE behavior:
  - include all role-derived permission scopes in user tokens (not request-limited)
- Preserve refresh behavior:
  - recompute current role-derived permission scopes (add/remove on refresh)
- Keep `client_credentials` grant behavior unchanged (request/intersection-based)
- Related UI consistency decision:
  - `/connect/userinfo` role list should filter disabled roles from display output

### 2.3 Bootstrap strategy: seed DB role-scope assignments from config defaults (non-destructive)
Files:
- `src/backend/AuthServer/Services/DatabaseSeeder.cs`
- `src/backend/AuthServer/appsettings.Scopes.json` (unchanged as defaults/catalog source)

Recommended strategy:
- Keep config `UserScopes.RoleScopes` as **bootstrap defaults**, not runtime source of truth.
- During seeding:
  - create roles from config if missing (existing behavior)
  - seed role-scope assignments **only when a role has no assignments yet**
  - do not overwrite existing DB assignments (to preserve UI edits)

Optional config flag for explicit sync (defer unless needed):
- `Rbac:SyncRoleScopesFromConfigOnStartup=false`

This prevents startup from undoing admin changes made in the UI.

## 3) Backend/AuthServer: migration strategy (important)

### 3.1 Move from `EnsureCreated()` to migrations for AuthServer schema evolution
Files:
- `src/backend/AuthServer/Services/DatabaseSeeder.cs`
- `src/backend/AuthServer/AuthServer.csproj` (if EF design package missing)
- `src/backend/AuthServer/Data/Migrations/*` (new)

Why this is needed:
- `EnsureCreated()` does not support evolving an existing schema safely
- New RBAC tables require repeatable schema updates across environments/dev DBs/tests

Recommended plan:
1. Replace `EnsureCreatedAsync()` with `MigrateAsync()` in seeder startup path (or move schema migration responsibility out of seeder and call before seeding)
2. Add EF migration for `AppRoleScope` table
3. Validate migrations in local dev and integration tests

Important caveat:
- Existing dev databases created with `EnsureCreated()` may need reset/recreate to start using migrations cleanly.
- Since this repo is dev-only, a documented DB reset is acceptable.

Decision (confirmed):
- Include this migration transition in the feature implementation.

## 4) Backend/AuthServer: admin APIs for roles & permissions management

### 4.1 Add Roles & Permissions admin controller
Files (new):
- `src/backend/AuthServer/Controllers/AdminRolesController.cs` (or `RolesController.cs` under `api/admin/roles` route)

Route recommendation:
- `Route("api/admin/roles")`

Authorization recommendation:
- Require authenticated bearer token + scope `admin.roles` or `admin.full`
- Add named policy in `AuthServer` (recommended), e.g. `AdminRolesPolicy`

Endpoints (v1):
1. `GET /api/admin/roles`
- Returns roles with metadata + assigned scope names (or scope count summary)

2. `GET /api/admin/roles/{roleId}`
- Returns role detail + assigned scopes (available scopes remain separate endpoint)

3. `POST /api/admin/roles`
- Create role (`name`, `description`, `isActive` optional)
- Optional server-side normalization/validation for role name

4. `PUT /api/admin/roles/{roleId}/scopes`
- Replace full assigned scope set for the role (idempotent)
- Validate scope names against catalog

5. `GET /api/admin/roles/available-scopes` (or separate admin scopes endpoint)
- Returns scope catalog grouped metadata for UI (name/displayName/description/resources)

Optional v1.1 endpoints:
- `PUT /api/admin/roles/{roleId}` update role metadata (description, `isActive`; no rename in v1)
- `DELETE /api/admin/roles/{roleId}` soft disable instead of delete (recommended)

### 4.2 Add request/response models and mappers
Files (new, suggested):
- `src/backend/AuthServer/Models/Requests/CreateRoleRequest.cs`
- `src/backend/AuthServer/Models/Requests/UpdateRoleScopesRequest.cs`
- `src/backend/AuthServer/Models/Responses/AdminRoleResponse.cs`
- `src/backend/AuthServer/Models/Responses/AdminRoleDetailResponse.cs`
- `src/backend/AuthServer/Models/Responses/AvailableScopeResponse.cs`

Validation rules:
- Role name required, unique, safe charset (alphanumeric + `.`/`-`/`_` or stricter)
- Scope list unique and non-empty only if desired (empty allowed if role should have no permission scopes)
- All scopes must exist in configured scope catalog/OpenIddict scopes

### 4.3 Add service/repository layer for role admin operations
Files (new, suggested):
- `src/backend/AuthServer/Services/Interfaces/IRoleAdminService.cs`
- `src/backend/AuthServer/Services/RoleAdminService.cs`
- `src/backend/AuthServer/Repositories/Interfaces/IRoleScopeRepository.cs`
- `src/backend/AuthServer/Repositories/RoleScopeRepository.cs`

Pattern recommendation:
- Use existing `Shared.Common.Result<T>` for service/repo operations (this is a normal admin API, unlike OpenIddict token endpoint)
- Controller maps to `ProblemDetails`/standard `IActionResult`

### 4.4 Add AuthServer policy for admin roles management API
Files:
- `src/backend/AuthServer/Program.cs`
- optionally `src/backend/Shared/Authorization/Extensions/...` if you want shared config-driven policies here too (not required)

Tasks:
- Add named policy requiring `admin.roles` or `admin.full` scope
- Apply policy on admin role endpoints

## 5) Frontend (web-admin): Roles & Permissions page UI/UX

### 5.1 Add route and page shell
Files (new):
- `src/web-admin/app/(authenticated)/users/roles/page.tsx`

Page access control:
- Use `AuthGuard` (extend it) with scope-based checks for this page
- Require `admin.roles` or `admin.full` as frontend UX gate (backend remains authoritative)

Page layout recommendation:
- Header: title, description, actions (`New Role` button, refresh)
- Two-column desktop layout:
  - Left: Roles list/table
  - Right: Role detail + permission editor
- Mobile: stack sections with tabs (`Roles`, `Permissions`)

### 5.2 Add frontend API client module for AuthServer admin role endpoints
Files (new):
- `src/web-admin/lib/admin-roles-api.ts`

Functions (v1):
- `listRoles()`
- `getRoleDetails(roleIdOrName)`
- `getAvailableScopes()` (if not included in role detail response)
- `createRole(payload)`
- `updateRoleScopes(roleId, scopeNames)`

Implementation notes:
- Use `authApi` (not `ordersApi`)
- Reuse existing axios interceptors and auth handling
- Typed request/response interfaces in the module or separate `types/` file

### 5.3 Add frontend scope source + scope-aware guard helpers (required for RBAC-aligned UI)
Files:
- `src/web-admin/contexts/auth-context.tsx`
- `src/web-admin/lib/oauth2-auth.ts`
- `src/web-admin/components/auth/AuthGuard.tsx`
- optional `src/web-admin/lib/authz.ts` (new helpers)

Tasks:
- Persist granted scopes from token response (`scope`) in frontend auth state/storage
- Parse scopes into array/set for UI checks
- Extend `AuthGuard` with `requiredScopes` (any-of for v1 is sufficient; all-of optional)
- Use scope-based gating for `/users/roles` page and roles-permissions UI actions
- Optionally migrate other pages/features to scope-based UI gating if low-risk in the same pass
- Keep role names available for UI display:
  - explicitly request OIDC `roles` scope in frontend auth flow
  - ensure `web-admin` client is allowed to request `roles`

Recommendation:
- Use token response `scope` as the source for frontend scope gating (not `userinfo`)

### 5.3 Build role list panel
Components (new, suggested):
- `src/web-admin/components/admin/roles/roles-list.tsx`
- `src/web-admin/components/admin/roles/role-row-actions.tsx` (optional)

UX features:
- Display role name, description, status, assigned scope count
- Search/filter roles by name (client-side initially)
- Select a role to edit permissions
- Highlight unsaved changes when switching roles (confirm dialog)

Use shadcn blocks:
- `Card`, `Input`, `Table`/`data-table`, `Badge`, `Button`, `Skeleton`

### 5.4 Build create role dialog/form
Components (new):
- `src/web-admin/components/admin/roles/create-role-dialog.tsx`

Recommended shadcn stack:
- `Dialog`
- `Form` (react-hook-form)
- `Input`
- `Button`
- `FormMessage`

Validation:
- Zod schema (frontend)
- Server-side validation remains authoritative

Fields (v1):
- `name` (required)
- `description` (optional)
- `isActive` (optional; if omitted, default active)

Out of scope (v1):
- Role rename after creation

### 5.5 Build permissions editor (grouped scope checklist / tree-like)
Components (new, suggested):
- `src/web-admin/components/admin/roles/role-scope-editor.tsx`
- `src/web-admin/components/admin/roles/scope-group-tree.tsx`
- `src/web-admin/components/admin/roles/scope-leaf-checkbox.tsx`

Recommended interaction model:
- Group scopes by prefix before the first `.`:
  - `orders`: `orders.read`, `orders.write`, `orders.manage`
  - `profile`: `profile.read`, `profile.write`
  - `admin`: `admin.full`, `admin.users`, `admin.roles`
  - `misc`: scopes without a prefix
- Render each group in a `Collapsible` section (tree-like UX)
- Render each scope as a `Checkbox` with label + description
- Group-level actions:
  - select all in group
  - clear group
- Page-level actions:
  - save changes (replace assignment set)
  - reset/reload from server

Why this over a strict tree component:
- shadcn provides primitives that already fit this pattern (`Collapsible` + `Checkbox` + `ScrollArea`)
- Simpler accessibility and less custom keyboard logic than a bespoke tree widget
- Easier to maintain with small scope counts (likely <100)

Optional enhancements (v1.1):
- Search/filter scopes (with `Command`-style UI)
- Show inherited/critical scope badges
- Diff preview before save (added/removed scopes)

### 5.6 State management strategy (frontend)
Recommended approach (fit current codebase):
- Local component state + `useEffect`/`useMemo`
- Keep network state in page container (no React Query required for v1)
- Decision confirmed: do not introduce React Query/SWR in v1 for this feature

State shape (v1):
- `roles[]`
- `selectedRoleId`
- `availableScopes[]`
- `assignedScopesOriginal` / `assignedScopesDraft` (Set<string>)
- loading + mutation flags
- error/success toasts (`sonner` if already used)

## 6) Backend/AuthServer tests (required)

### 6.1 Add integration tests for admin role APIs
Project:
- `tests/backend/AuthServer.IntegrationTests` (existing)

Add tests to `AuthenticationFlowTests.cs` or better a new file:
- `tests/backend/AuthServer.IntegrationTests/Flows/AdminRolesManagementTests.cs`

Test cases (high priority):
1. `GET admin roles` requires auth and `admin.roles` scope (or `admin.full` if allowed)
2. `POST create role` creates a role and returns it
3. `PUT role scopes` replaces assignments and persists in DB
4. invalid scope name returns validation failure (400)
5. duplicate role name returns conflict/validation error

### 6.2 Add integration tests proving token issuance uses edited DB role scopes
Project:
- `tests/backend/AuthServer.IntegrationTests`

Test cases:
1. Update `User` role scope assignments via admin API (or DB helper), then PKCE login -> token includes updated scopes
2. Remove scope from role assignment, refresh token -> refreshed token no longer includes that scope
3. Add scope to role assignment, refresh token -> refreshed token includes new scope

These tests are critical because they validate the actual feature value (UI edits affect authorization behavior).

### 6.3 Add repository/service tests (optional if integration coverage is strong)
- If service logic becomes complex (replace semantics, validation, diffing), add focused unit tests for `RoleAdminService` and `RoleScopeService`.
- Use `NSubstitute` only for true external seams; prefer real EF integration for DB behavior.

## 7) Frontend tests (required)

### 7.1 Add Playwright page tests with mocked API responses (fast, stable)
Files (new, suggested):
- `src/web-admin/tests/roles-permissions-page.spec.ts`

Test cases (mocked `authApi` endpoints):
1. Roles page loads and shows roles list + selected role details
2. Create role dialog submits and new role appears in list
3. Toggling scope checkbox marks page dirty and save sends expected payload
4. Group select-all / clear works
5. Unauthorized response shows error state / redirects appropriately

Why Playwright first:
- Existing frontend test setup already uses Playwright
- No need to add Vitest/Jest immediately

### 7.2 Optional frontend unit tests for scope-tree helpers (if extracted)
If you extract pure helpers (recommended):
- `groupScopesByPrefix(scopes)`
- `computeRoleScopeDiff(original, draft)`
- `filterScopes(query, groups)`

Then consider adding a lightweight unit test setup later. This is optional for v1 if Playwright coverage is good.

## 8) Docs updates (required)
Files:
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/TESTING.md`
- `docs/AGENTS.MD`
- `docs/FRONTEND.md`

Add/update:
- Explain role-scope assignment source of truth moved to DB (scope catalog still config/OpenIddict)
- Document admin roles/permissions UI route (`/users/roles`)
- Document migration requirement (AuthServer now uses migrations)
- Document admin APIs and required scopes (`admin.roles`)
- Add frontend testing commands for roles page Playwright tests

## 9) Migration / rollout strategy (dev-focused)
Because this repo is dev-only but already has local DB state:

1. Add EF migration(s) for new RBAC tables
2. Change startup schema creation to migrations (`MigrateAsync`)
3. Reset local AuthServer DB if necessary (documented)
4. Seed bootstrap role-scope assignments from config for roles with no assignments
5. Verify UI edits persist across restarts (seeder must not overwrite assignments)

## Recommended implementation order (pragmatic)
1. Backend EF model + migration strategy (`AppRoleScope`, `MigrateAsync`)
2. `RoleScopeService` + token issuance integration (DB-backed role scope lookup)
3. Admin roles API endpoints (`GET/POST/PUT scopes`) + auth policy (`admin.roles`)
4. AuthServer integration tests for admin API + token issuance from DB assignments
5. Frontend API client + `/users/roles` page shell
6. Roles list + create role dialog
7. Permissions editor (grouped collapsible checklist) + save flow
8. Playwright tests for roles page
9. Docs updates

## UI component recommendation summary (shadcn)
Use these building blocks (all official shadcn primitives/docs-backed):
- `Card`, `Table`, `Dialog`, `Form`, `Input`, `Button`, `Badge`
- `Collapsible` (tree-like grouped scopes)
- `Checkbox` (needs to be added if not present)
- `ScrollArea` (recommended for long scope lists)
- `Tabs` (recommended for mobile layout)
- `Command` (optional for scope search UX)

Avoid for v1:
- Custom true tree widget with complex keyboard navigation
- Heavy client-side state libraries (React Query, Zustand) unless page complexity grows significantly

## Open questions / choices to confirm before implementation

### Q1 (Important): Role->scope runtime source of truth
Editable UI implies DB-backed assignments. Do you want to officially change runtime role->scope lookup from config to DB, using config only as bootstrap defaults?

Options:
- A. Yes, DB is source of truth for role->scope assignments; config is bootstrap/defaults only (Recommended)
- B. Keep config as source of truth and have UI edit config files (not recommended)

Recommendation: A

Decision (user-confirmed):
- A. DB is the runtime source of truth for role->scope assignments.
- `appsettings.Scopes.json` remains the scope catalog + bootstrap defaults source.

### Q2 (Important): Scope catalog editing scope
For this feature, should admins be able to edit only role assignments, or also create/edit scope definitions themselves?

Options:
- A. Role assignment only (scopes remain config/OpenIddict-seeded) (Recommended v1)
- B. Full scope catalog CRUD in UI too (much larger scope)

Recommendation: A

Decision (user-confirmed):
- A. v1 is role assignment management only.
- Scope catalog definitions remain config/OpenIddict-seeded and are not editable in the UI.

### Q3 (Important): Role deletion behavior
What should happen when deleting a role (if we include delete at all)?

Options:
- A. No delete in v1; only create + edit scopes (Recommended)
- B. Soft-disable role (`IsActive = false`) and block assignment/use in admin UI
- C. Hard delete role if no users assigned (more edge cases)

Recommendation: A for v1 (add disable later)

Decision (user-confirmed):
- B. Use soft-disable (`IsActive = false`) for v1 instead of hard delete.

Implementation impact:
- Add role metadata edit endpoint/UI action for enable/disable.
- Admin UI should prevent assigning disabled roles to users (future user-management integration).
- Token issuance behavior for users in disabled roles must be defined (recommended: disabled roles should not contribute scopes).

### Q4 (Frontend UX): Scope editor interaction style
Which editor style do you prefer for v1?

Options:
- A. Grouped collapsible checklist (tree-like by prefix) (Recommended)
- B. Flat searchable table with checkboxes
- C. Dual-list picklist (available/assigned)

Recommendation: A

Decision (user-confirmed):
- A. Use a grouped collapsible checklist (tree-like by scope prefix) for v1.

UI implementation note:
- Group scopes by prefix (e.g. `orders`, `profile`, `admin`) and render leaf scope checkboxes with descriptions.

### Q5 (Backend API shape): role detail payload
Do you prefer one endpoint that returns role + assigned scopes + available scope catalog in one payload, or separate endpoints?

Options:
- A. Separate endpoints (`GET roles`, `GET available-scopes`, `GET role/{id}`, `PUT role/{id}/scopes`) (Recommended, cleaner cache/reuse)
- B. Combined role-detail payload including available scopes (fewer calls, larger response)

Recommendation: A

## Acceptance criteria (feature complete)
- Admin can open `/users/roles` and see roles + available scopes
- Admin can create a new role
- Admin can edit a role's assigned scopes and save
- Changes persist in DB and survive app restart
- PKCE user tokens reflect DB-edited role scopes on login
- Refresh tokens recompute DB-edited role scopes after changes
- Admin APIs are protected by `admin.roles` (and/or `admin.full` as designed)
- Backend + frontend tests cover the main flows
