# Roles & Permissions UI + AuthServer Implementation Checklist

Date: 2026-02-25
Status: Tasked (ready for implementation)
Related plan: `ai-analysis/2026-02-25-roles-permissions-ui-authserver-plan.md`

## Objective
Implement a Roles & Permissions admin feature where:
- Admin can create roles
- Admin can enable/disable roles (soft disable)
- Admin can edit role -> scope assignments in UI
- Role->scope assignments persist in DB (runtime source of truth)
- AuthServer token issuance/refresh uses DB role->scope assignments
- Orders/API authorization behavior follows scopes from updated role assignments

## Confirmed decisions (from planning)
- Role->scope assignments runtime source of truth: DB
- `appsettings.Scopes.json` remains scope catalog + bootstrap defaults
- v1 scope management: role assignment only (no scope catalog CRUD UI)
- Role delete behavior: soft disable (`IsActive = false`)
- Scope editor UX: grouped collapsible checklist (tree-like by prefix)
- Backend API shape: separate endpoints

## Scope of work
- Backend/AuthServer EF models + DB migration strategy
- Backend/AuthServer admin roles/permissions APIs
- AuthServer token issuance integration with DB role-scope assignments
- Frontend `/users/roles` page + create role + permission editor
- Backend integration tests + frontend Playwright tests
- Docs updates

## Implementation Checklist

### A. Backend/AuthServer DB model and migration foundation

#### A1. Add role-scope assignment EF entity/table
Files (new):
- `src/backend/AuthServer/Data/Entities/AppRoleScope.cs`
- `src/backend/AuthServer/Data/Configurations/AppRoleScopeConfiguration.cs`

Tasks:
- Add `AppRoleScope` entity with fields:
  - `RoleId` (`string`)
  - `ScopeName` (`string`)
  - `AssignedAt` (`DateTime`)
  - `AssignedBy` (`string?`) (optional audit metadata)
- Configure composite PK (`RoleId`, `ScopeName`)
- Configure FK to `AppRole` with cascade delete
- Configure `ScopeName` max length and optional index
- Configure `AssignedBy` max length

Acceptance checks:
- EF model builds successfully
- Table design supports idempotent replace assignments

#### A2. Add DbSet/navigations for role-scope assignments
Files:
- `src/backend/AuthServer/Data/AppDbContext.cs`
- `src/backend/AuthServer/Data/Entities/AppRole.cs`

Tasks:
- Add `DbSet<AppRoleScope>` to `AppDbContext`
- Add `ICollection<AppRoleScope>` navigation to `AppRole` (recommended)
- Ensure entity configurations are discovered by `ApplyConfigurationsFromAssembly(...)`

Acceptance checks:
- EF context includes role-scope assignment table in model

#### A3. Move AuthServer schema initialization to migrations
Files:
- `src/backend/AuthServer/Services/DatabaseSeeder.cs`
- `src/backend/AuthServer/Data/Migrations/*` (generated)
- `src/backend/AuthServer/AuthServer.csproj` (only if EF tooling package needed)

Tasks:
- Replace `EnsureCreatedAsync()` with `MigrateAsync()` before seeding
- Generate initial/next migration that includes `AppRoleScope` table
- Document local DB reset requirement if existing DB was created via `EnsureCreated`
- This migration transition is confirmed in-scope for this feature

Important note:
- Existing dev DBs may need reset/recreate to transition cleanly to migrations.

Acceptance checks:
- AuthServer starts and applies migration successfully
- New table exists in Postgres test/dev DB

### B. Backend/AuthServer role-scope services (DB-backed runtime source of truth)

#### B1. Add role-scope repository for assignment persistence
Files (new):
- `src/backend/AuthServer/Repositories/Interfaces/IRoleScopeRepository.cs`
- `src/backend/AuthServer/Repositories/RoleScopeRepository.cs`

Tasks:
- Implement methods for:
  - Get assigned scopes for a role
  - Replace assigned scopes for a role (transactional)
  - Get all role assignments (for admin list/detail)
  - Query effective scopes for multiple roles
- Validate repository semantics for full replace (remove missing + add new)

Acceptance checks:
- Repository methods support role detail and token issuance use cases

#### B2. Add DB-backed role-scope domain service (runtime provider)
Files (new):
- `src/backend/AuthServer/Services/Interfaces/IRoleScopeService.cs`
- `src/backend/AuthServer/Services/RoleScopeService.cs`

Tasks:
- Responsibilities:
  - Return available scope catalog (delegate to config/OpenIddict-backed source)
  - Return role assignments from DB
  - Replace role assignments after validating scope names
  - Compute effective permission scopes for a user from active roles
- Validation behavior:
  - Reject unknown scopes (not in config scope catalog via `ScopeConfigurationService`, v1)
  - Deduplicate scope names
- Disabled-role behavior (confirmed default to implement):
  - roles with `IsActive = false` do not contribute scopes

Acceptance checks:
- Service returns correct effective scopes for active vs disabled roles
- Unknown scope assignment is rejected

#### B3. Keep/adjust scope catalog service responsibilities
Files:
- `src/backend/AuthServer/Services/ScopeConfigurationService.cs`

Tasks:
- Narrow `ScopeConfigurationService` to scope catalog metadata concerns (if needed)
- Remove or deprecate config-based user role->scope runtime lookup methods once DB-backed `IRoleScopeService` is integrated
- Keep methods used by `ScopeController` and admin UI for scope catalog listing

Acceptance checks:
- No ambiguous dual source-of-truth behavior remains for runtime role->scope assignment lookup

### C. Backend/AuthServer seeding/bootstrap behavior

#### C1. Seed bootstrap role-scope assignments from config defaults (non-destructive)
Files:
- `src/backend/AuthServer/Services/DatabaseSeeder.cs`

Tasks:
- Add seeding step after roles/scopes exist:
  - For each role in config `UserScopes.RoleScopes`
  - If role has no DB assignments yet, insert bootstrap assignments from config
  - If role already has DB assignments, do not overwrite (preserve UI edits)
- Log bootstrap decisions clearly (seeded vs skipped)

Acceptance checks:
- First startup bootstraps role-scope assignments from config
- Subsequent startups preserve DB-edited assignments

#### C2. Optional sync toggle (defer unless needed)
Files (optional):
- `src/backend/AuthServer/appsettings*.json`
- `src/backend/AuthServer/Services/DatabaseSeeder.cs`

Tasks:
- Optional config flag to force sync from config on startup (default false)
- Defer unless you need repeatable demo resets without DB reset

Acceptance checks:
- N/A if deferred

### D. Backend/AuthServer token issuance integration (RBAC runtime)

#### D1. Switch PKCE user token issuance to DB-backed effective scopes
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Services/Interfaces/IRoleScopeService.cs`
- `src/backend/AuthServer/Services/RoleScopeService.cs`

Tasks:
- Replace config-derived user permission scope lookup with DB-backed `IRoleScopeService`
- Preserve current project-specific behavior:
  - include all role-derived permission scopes in PKCE user token (not request-limited)
  - protocol scopes remain request-driven
- Ensure disabled roles do not contribute scopes

Acceptance checks:
- Token issuance reflects DB-edited role-scope assignments
- Disabled role assignments do not appear in tokens

#### D2. Switch refresh-token recomputation to DB-backed effective scopes
Files:
- `src/backend/AuthServer/Services/AuthorizationGrantService.cs`

Tasks:
- Replace current config-based role-scope lookup with DB-backed `IRoleScopeService`
- Preserve current confirmed behavior:
  - refresh recomputes current role-derived permission scopes
  - refresh can add/remove permission scopes after role or assignment changes
  - protocol scopes remain constrained to previously granted/requested set
- Keep role claims reattached in refreshed access tokens

Acceptance checks:
- Role assignment changes are reflected on refresh token grant
- Disabled role no longer contributes scopes on refresh

#### D2b. Align `userinfo` roles output with disabled-role behavior (required)
Files:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`

Tasks:
- Filter disabled roles out of `/connect/userinfo` role list to avoid UI display confusion
- Keep in mind frontend page gating will use scopes, so this is a UX consistency improvement, not core authorization enforcement

Acceptance checks:
- `userinfo` role list behavior matches chosen product decision for disabled roles

#### D3. Keep client_credentials behavior unchanged
Files:
- `src/backend/AuthServer/Services/AuthorizationGrantService.cs`

Tasks:
- Confirm no regression in client_credentials requested-scope intersection behavior
- Add comments/tests if behavior becomes less obvious after refactor

Acceptance checks:
- `client_credentials` grant still validates/intersects requested scopes as before

#### D4. Allow OIDC `roles` scope for `web-admin` client (UI role display support)
Files:
- `src/backend/AuthServer/appsettings.Clients.json`

Tasks:
- Add OIDC `roles` scope to `web-admin` allowed scopes so frontend can request role claims for `userinfo` display
- Keep permission scope behavior unchanged (`admin.roles` is a separate permission scope)

Acceptance checks:
- `web-admin` can request OIDC `roles` without token request errors

### E. Backend/AuthServer admin roles & permissions API

#### E1. Add admin API request/response contracts
Files (new, suggested):
- `src/backend/AuthServer/Models/Requests/CreateRoleRequest.cs`
- `src/backend/AuthServer/Models/Requests/UpdateRoleRequest.cs` (for metadata/enable-disable)
- `src/backend/AuthServer/Models/Requests/UpdateRoleScopesRequest.cs`
- `src/backend/AuthServer/Models/Responses/AdminRoleSummaryResponse.cs`
- `src/backend/AuthServer/Models/Responses/AdminRoleDetailResponse.cs`
- `src/backend/AuthServer/Models/Responses/AvailableScopeResponse.cs`

Tasks:
- Add DTOs for separate endpoint contracts
- Include role metadata (`id`, `name`, `description`, `isActive`, timestamps if useful)
- Include assigned scope names in role detail response
- Include scope metadata in available scopes response (`name`, `displayName`, `description`, `resources`)
- Source available scopes from config scope catalog (`ScopeConfigurationService`) in v1

Acceptance checks:
- DTOs are explicit and stable for frontend consumption

#### E2. Add role admin service using `Result<T>` pattern
Files (new):
- `src/backend/AuthServer/Services/Interfaces/IRoleAdminService.cs`
- `src/backend/AuthServer/Services/RoleAdminService.cs`

Tasks:
- Use `Shared.Common.Result<T>` for admin API business operations
- Implement operations:
  - list roles (with scope counts / summaries)
  - get role detail (metadata + assigned scopes)
  - create role
  - update role metadata (`description`, `isActive`) only (no rename in v1)
  - replace role scopes
  - list available scope catalog
- Validation:
  - unique role name
  - safe role name format
  - cannot assign unknown scopes
  - optional guardrails (e.g., prevent disabling last admin role) [defer unless needed]

Acceptance checks:
- Service returns meaningful failure codes/messages for controller mapping

#### E3. Add admin roles controller endpoints (separate endpoints)
Files (new):
- `src/backend/AuthServer/Controllers/AdminRolesController.cs`

Route recommendation:
- `[Route("api/admin/roles")]`

Endpoints (v1 required):
- `GET /api/admin/roles`
- `GET /api/admin/roles/available-scopes`
- `GET /api/admin/roles/{roleId}` (confirmed route identifier)
- `POST /api/admin/roles`
- `PUT /api/admin/roles/{roleId}` (metadata incl. `isActive` soft-disable)
- `PUT /api/admin/roles/{roleId}/scopes` (replace assignments)

Tasks:
- Apply policy requiring `admin.roles` or `admin.full`
- Map `Result<T>` to `ProblemDetails`/standard action results
- Keep endpoint responses frontend-friendly and explicit
- Do not support role rename in v1 API contracts

Acceptance checks:
- Endpoint set supports full v1 UI flows without extra hidden endpoints

#### E4. Add AuthServer authorization policy for roles admin endpoints
Files:
- `src/backend/AuthServer/Program.cs`

Tasks:
- Add named policy for roles admin management (e.g., `AdminRolesManagement`)
- Require `admin.roles` or `admin.full` scope
- Apply to `AdminRolesController`

Acceptance checks:
- Non-admin token denied
- Admin token with `admin.roles` (or `admin.full`) allowed

### F. Frontend (web-admin) roles & permissions page

#### F1. Add route page for roles & permissions
Files (new):
- `src/web-admin/app/(authenticated)/users/roles/page.tsx`

Tasks:
- Create page shell with `AuthGuard` using scope-based checks (`admin.roles` or `admin.full`)
- Initial layout:
  - header + action buttons
  - roles panel + detail/permissions editor panel
- Show loading/error/empty states
- Keep page responsive (stack on mobile)

Acceptance checks:
- Navigating to `/users/roles` renders a protected page using scope-based frontend UX gating

#### F2. Add frontend API module for AuthServer admin role endpoints
Files (new):
- `src/web-admin/lib/admin-roles-api.ts`
- optional `src/web-admin/lib/types/admin-roles.ts`

Tasks:
- Use `authApi` for all calls
- Keep page data loading/mutations in local React state (no React Query/SWR in v1)
- Add typed functions:
  - `listRoles`
  - `getAvailableScopes`
  - `getRole`
  - `createRole`
  - `updateRole`
  - `updateRoleScopes`
- Normalize API errors for UI display

Acceptance checks:
- API module supports all page interactions without page-level axios duplication

#### F2b. Add frontend scope source + scope-aware auth helpers (required)
Files:
- `src/web-admin/lib/oauth2-auth.ts`
- `src/web-admin/contexts/auth-context.tsx`
- `src/web-admin/components/auth/AuthGuard.tsx`
- optional `src/web-admin/lib/authz.ts`

Tasks:
- Persist granted token scopes (`scope` from `/connect/token`) in frontend storage/auth state
- Parse scope string into array/set and expose it in auth context
- Extend `AuthGuard` to support `requiredScopes` (any-of for v1; all-of optional)
- Keep role-based checks only where still needed for display/compatibility (not primary page gating)
- Update `/users/roles` page to use scope-based guard (`admin.roles` or `admin.full`)
- Restore explicit auth scope request in `OAuth2Auth.authorize()` and include OIDC `roles` for role display (`openid profile email offline_access roles`)

Acceptance checks:
- Frontend can gate pages/features by scopes without depending on `userinfo.roles`
- Frontend still receives role names in `userinfo` for display (if user has OIDC `roles` scope)

#### F3. Add roles list panel component(s)
Files (new, suggested):
- `src/web-admin/components/admin/roles/roles-list.tsx`
- `src/web-admin/components/admin/roles/role-status-badge.tsx` (optional)
- `src/web-admin/components/admin/roles/roles-list-skeleton.tsx` (optional)

Tasks:
- Display roles list with columns:
  - name
  - description
  - status (active/disabled)
  - assigned scope count
- Add client-side search/filter by role name
- Support selection and highlight current role
- Warn on unsaved changes when selecting another role (confirm dialog) [can defer if needed]

Acceptance checks:
- Roles can be selected and loaded into detail editor

#### F4. Add create role dialog/form
Files (new):
- `src/web-admin/components/admin/roles/create-role-dialog.tsx`

Tasks:
- Implement with shadcn `Dialog` + `Form` + `Input`
- Add zod schema validation for role name/description
- Submit to `POST /api/admin/roles`
- Refresh list/select new role after creation

Acceptance checks:
- Admin can create a new role end-to-end from UI

#### F5. Add role metadata editor (soft disable/enable)
Files (new or merged with detail panel):
- `src/web-admin/components/admin/roles/role-details-panel.tsx`

Tasks:
- Show/edit role description
- Toggle active/disabled (`isActive`)
- Save via `PUT /api/admin/roles/{roleId}`
- Clearly label disabled state and impact
- Do not expose role rename in v1 UI

Acceptance checks:
- Admin can disable and re-enable role from UI
- Disabled status persists after refresh

#### F6. Add grouped scope checklist editor (tree-like)
Files (new):
- `src/web-admin/components/admin/roles/role-scope-editor.tsx`
- `src/web-admin/components/admin/roles/scope-group-list.tsx`
- `src/web-admin/components/admin/roles/scope-group-item.tsx`
- `src/web-admin/components/admin/roles/scope-checkbox-item.tsx`
- optional helper: `src/web-admin/lib/scope-grouping.ts`

Tasks:
- Group available scopes by prefix (`orders`, `profile`, `admin`, misc)
- Render each group with `Collapsible`
- Render leaf scopes with `Checkbox`, label, description, optional resources
- Group-level actions:
  - select all in group
  - clear group
- Track draft vs original assignments and dirty state
- Save via `PUT /api/admin/roles/{roleId}/scopes` (replace assignment set)
- Reset/reload current role assignments

Recommended shadcn components:
- `Collapsible` (existing)
- `Checkbox` (add if missing)
- `ScrollArea` (recommended for long lists)
- `Badge` (group counts)
- `Button`

Acceptance checks:
- Admin can edit role scope assignments and persist changes
- Draft state reflects server-assigned scopes after save

#### F7. Add missing shadcn primitives (if needed)
Files (new via shadcn CLI or manual):
- `src/web-admin/components/ui/checkbox.tsx`
- `src/web-admin/components/ui/tabs.tsx` (recommended for mobile)
- `src/web-admin/components/ui/scroll-area.tsx` (recommended)
- optional `src/web-admin/components/ui/command.tsx` (scope search/filter)
- optional `src/web-admin/components/ui/alert-dialog.tsx` (unsaved changes confirm)

Tasks:
- Add components via shadcn CLI or manual copy pattern consistent with existing `components.json`
- Keep UI style aligned with current project theme (`new-york`, neutral)

Acceptance checks:
- New page uses consistent shared UI primitives (no one-off ad-hoc controls)

### G. Backend tests (AuthServer)

#### G1. Add admin roles API integration tests
Project:
- `tests/backend/AuthServer.IntegrationTests`

Files (new recommended):
- `tests/backend/AuthServer.IntegrationTests/Flows/AdminRolesManagementTests.cs`

Test cases (required):
1. `GET /api/admin/roles` requires auth/admin scope
2. `GET /api/admin/roles/available-scopes` returns configured scope catalog metadata
3. `POST /api/admin/roles` creates role successfully
4. `PUT /api/admin/roles/{id}` updates description and soft-disable state
5. `PUT /api/admin/roles/{id}/scopes` replaces assignments and persists
6. invalid scope assignment returns `400`
7. duplicate role name returns conflict/validation error

Acceptance checks:
- Admin API contract and security are covered by integration tests

#### G2. Extend auth-flow integration tests for DB-edited role scopes
Project:
- `tests/backend/AuthServer.IntegrationTests`

Files:
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`
- optional shared helpers under `tests/backend/AuthServer.IntegrationTests/Helpers/*`

Tasks:
- Add tests proving token issuance uses DB role-scope assignments (not config-only assumptions):
  - PKCE login reflects edited role scopes
  - refresh token reflects assignment changes (upgrade/downgrade)
  - disabled role does not contribute scopes
- Prefer using admin API to mutate assignments (end-to-end), fallback to DB helper only if simpler for v1

Acceptance checks:
- Core RBAC admin feature value is covered (UI-editable assignments affect tokens)

### H. Frontend tests (web-admin)

#### H1. Add Playwright test for roles & permissions page (mocked API)
Files (new):
- `src/web-admin/tests/roles-permissions-page.spec.ts`

Tasks:
- Mock `authApi` endpoints used by roles page
- Cover:
  - page loads with roles list + scopes
  - create role dialog submits successfully
  - scope checkbox toggle marks dirty state and save sends expected payload
  - soft-disable role action persists UI state
  - unauthorized API response handling (redirect/error)

Acceptance checks:
- Frontend RBAC admin UI behavior is covered without requiring live backend

#### H2. Optional helper tests (defer if Playwright is sufficient)
Files (optional):
- helper test files if you add pure helpers like `groupScopesByPrefix`

Tasks:
- Test grouping logic/diff calculation if extracted and non-trivial

Acceptance checks:
- N/A if deferred

### I. Docs updates (required)

#### I1. Update architecture and developer docs
Files:
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/TESTING.md`
- `docs/AGENTS.MD`
- `docs/FRONTEND.md`

Tasks:
- Document runtime role->scope source of truth moved to DB (scope catalog still config/OpenIddict)
- Document admin roles page route: `/users/roles`
- Document admin API endpoints and `admin.roles` scope requirement
- Document soft-disable role behavior and token issuance effect (disabled role contributes no scopes)
- Document migration requirement (`MigrateAsync`) and local DB reset notes
- Add frontend and backend test commands for this feature

Acceptance checks:
- Docs match implemented API routes, UI routes, and runtime behavior

### J. Validation and rollout checks

#### J1. Backend runtime validation
Tasks:
- Start AuthServer and confirm migrations apply
- Confirm bootstrap role-scope assignments seed only when missing
- Confirm editing role assignments persists across restart

Acceptance checks:
- DB changes persist and are not overwritten on startup

#### J2. End-to-end RBAC validation (manual)
Tasks:
- Create new role in UI
- Assign scopes to role in UI
- Assign role to a test user (existing user management/manual DB for now)
- Login / refresh and verify token-derived behavior in Orders and userinfo/admin flows
- Disable role and verify role no longer contributes scopes on refresh/login

Acceptance checks:
- End-to-end behavior matches product rules

## Recommended implementation order
1. A (EF entity + DbSet + migrations strategy)
2. B + C (DB-backed role-scope services + bootstrap seeding)
3. D (token issuance/refresh integration)
4. E (admin roles/permissions API + auth policy)
5. G (backend integration tests)
6. F (frontend API + page + dialogs + scope editor)
7. H (frontend Playwright tests)
8. I (docs)
9. J (manual validation)

## Notes / guardrails for implementation
- Keep scope catalog metadata config/OpenIddict-managed in v1; do not add scope CRUD to this feature.
- Use `Result<T>` for admin REST APIs/services, but keep OpenIddict token endpoint protocol mapping patterns unchanged.
- Soft-disabled roles should not contribute scopes to token issuance/refresh.
- Do not let seeding overwrite DB-edited role-scope assignments.
- Prefer grouped collapsible checklist over a custom tree widget for v1 (shadcn primitives fit this well).

## Final Validation Checklist (done criteria)
- [x] DB-backed role-scope assignment table exists and is migrated
- [x] AuthServer uses DB role-scope assignments for PKCE user tokens
- [x] Refresh token recomputes DB role-scope assignments (add/remove)
- [x] Disabled roles do not contribute scopes
- [x] Admin roles API is protected by `admin.roles` / `admin.full` (as implemented)
- [ ] Admin can create role in UI
- [ ] Admin can enable/disable role in UI
- [ ] Admin can edit role scopes in grouped checklist UI and save
- [ ] Changes persist across restart and are not overwritten by seeder
- [x] Backend integration tests pass
- [ ] Frontend Playwright roles-page tests pass
- [x] Docs updated

## Implementation Log (to update while coding)
- 2026-02-25: Implemented AuthServer DB-backed role-scope assignments (`AspNetRoleScopes`) with EF migration `InitialIdentityAndOpenIddictRoleScopes`.
- 2026-02-25: Switched AuthServer startup seeding from `EnsureCreatedAsync()` to `MigrateAsync()` and added bootstrap seeding for role-scope assignments (seed only when missing).
- 2026-02-25: Added AuthServer roles admin API (`/api/admin/roles*`) with `admin.roles` OR `admin.full` policy, create/update/soft-disable/scope replacement endpoints.
- 2026-02-25: Updated token issuance/refresh + `/connect/userinfo` to use active DB-backed role assignments and filter disabled roles from role output.
- 2026-02-25: Added/extended AuthServer integration tests covering admin roles APIs, DB-backed token issuance, and disabled-role userinfo filtering (27 passing).
- 2026-02-25: Implemented frontend roles & permissions page (`/users/roles`) with scope-based `AuthGuard`, local-state data loading, create role dialog, metadata editor, and grouped collapsible scope checklist editor.
- 2026-02-25: Added frontend admin roles API module and scope persistence in OAuth/token refresh flow (`grantedScopes`) for scope-based UI gating.
- 2026-02-25: Added Playwright smoke spec `src/web-admin/tests/roles-permissions-page.spec.ts` (not executed in this session).
- 2026-02-25: Updated README + docs (`ARCHITECTURE`, `DEVELOPMENT`, `TESTING`, `AGENTS`) for DB-backed RBAC assignments and admin roles UI/API.
- Validation run: `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj` -> Passed (27/27).
- Validation run: `npx tsc --noEmit` in `src/web-admin` -> Passed.
- Note: `npm run build` in `src/web-admin` still fails on pre-existing ESLint issues unrelated to this feature (`oauth2-auth.ts` no-explicit-any, admin page apostrophes, other existing warnings).
