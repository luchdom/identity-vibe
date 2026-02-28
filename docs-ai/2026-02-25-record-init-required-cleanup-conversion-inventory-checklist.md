# Record/Init/Required + Cleanup Conversion Inventory Checklist

Date: 2026-02-25
Status: Tasked (execution inventory)
Related:
- `ai-analysis/2026-02-25-record-init-required-cleanup-plan.md`

## Purpose

This is the execution inventory for the cleanup initiative.

Use it to track each file/class as one of:
- `CONVERT` (record/init/required modernization)
- `KEEP` (intentionally remains class/mutable/framework-bound)
- `REMOVE` (verified unused / stale)
- `REVIEW` (needs validation before decision)

## Confirmed scope decisions (already agreed)

- Include frontend cleanup in the same initiative
- Remove verified-unused code immediately
- Include config binding models in the first conversion batch (with runtime options validation in same pass)

## Batch order (recommended)

1. `REMOVE` verified-unused code
2. `CONVERT` safe AuthServer DTOs/commands/results/admin models
3. `CONVERT` config models + add runtime validation
4. Frontend cleanup/consolidation
5. Architecture/docs/analyzer hardening
6. Validation pass

---

## A. Backend inventory (`AuthServer`, `Orders`, `Shared`)

### A1. `AuthServer` model/config conversion candidates

#### Convert in batch 1 (safe/high-value)

- [x] `CONVERT` `src/backend/AuthServer/Models/Commands/LoginCommand.cs`
Notes:
- Prefer `sealed record`
- `required` + `init` for `Email`, `Password`

- [x] `CONVERT` `src/backend/AuthServer/Models/Commands/RegisterCommand.cs`
Notes:
- Prefer `sealed record`
- `required` + `init` for required fields

- [x] `CONVERT` `src/backend/AuthServer/Models/Results/LoginResult.cs`
Notes:
- Prefer immutable result shape (`sealed record`)
- Evaluate if booleans/defaults should remain or use factory methods

- [x] `CONVERT` `src/backend/AuthServer/Models/Results/RegisterResult.cs`

- [x] `CONVERT` `src/backend/AuthServer/Models/ViewModels/UserViewModel.cs`
Notes:
- Read-model/projection, good record candidate
- Update mappers/static factory accordingly

- [x] `CONVERT` `src/backend/AuthServer/Models/AdminRoles/AdminRoleResponse.cs`
- [x] `CONVERT` `src/backend/AuthServer/Models/AdminRoles/AdminRoleDetailResponse.cs`
- [x] `CONVERT` `src/backend/AuthServer/Models/AdminRoles/AvailableScopeResponse.cs`
- [x] `CONVERT` `src/backend/AuthServer/Models/AdminRoles/CreateRoleRequest.cs`
- [x] `CONVERT` `src/backend/AuthServer/Models/AdminRoles/UpdateRoleRequest.cs`
- [x] `CONVERT` `src/backend/AuthServer/Models/AdminRoles/UpdateRoleScopesRequest.cs`
Notes:
- JSON DTOs, strong candidates for `sealed record`
- Use `required` selectively (e.g., `Name`, maybe `ScopeNames`)

#### Convert in batch 1 (config models + validation same pass)

- [x] `CONVERT` `src/backend/AuthServer/Configuration/ClientConfiguration.cs`
- [x] `CONVERT` `src/backend/AuthServer/Configuration/ClientSettings.cs`
- [x] `CONVERT` `src/backend/AuthServer/Configuration/ScopeSettings.cs`
- [x] `CONVERT` `src/backend/Shared/Authorization/Models/AuthorizationConfig.cs` (`AuthorizationConfig`, `PolicyConfig`)

Required paired tasks (same batch):
- [x] Add runtime validation for `ClientConfiguration` options
- [x] Add runtime validation for scope configuration models
- [x] Add runtime validation for shared authorization policy config
- [x] Add startup fail-fast (`ValidateOnStart`) where applicable

Notes:
- Keep collection defaults where empty lists/dicts are valid semantic defaults
- Do not rely on `required` alone for config binding correctness

#### Keep as mutable classes for now (Razor/form binding)

- [ ] `KEEP` `src/backend/AuthServer/Models/Requests/LoginRequest.cs`
Why:
- Razor form postback model, mutable binding + view reuse

- [ ] `KEEP` `src/backend/AuthServer/Models/Requests/RegisterRequest.cs`
Why:
- Razor form model, mutable fields (`ErrorMessage`, `ReturnUrl`)

#### Review (optional later)

- [ ] `REVIEW` Other AuthServer request models used for APIs/forms individually (e.g., if any class-based request models expand later)

### A2. `AuthServer` framework/infrastructure classes (keep or cleanup)

#### Keep (not record candidates)

- [ ] `KEEP` `src/backend/AuthServer/Data/AppDbContext.cs`
- [ ] `KEEP` `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- [ ] `KEEP` `src/backend/AuthServer/Controllers/AccountController.cs`
- [ ] `KEEP` `src/backend/AuthServer/Controllers/AdminRolesController.cs`
- [ ] `KEEP` `src/backend/AuthServer/Services/AuthorizationGrantService.cs`
- [ ] `KEEP` `src/backend/AuthServer/Services/RoleAdminService.cs`
- [ ] `KEEP` `src/backend/AuthServer/Services/ScopeConfigurationService.cs`
- [ ] `KEEP` `src/backend/AuthServer/Services/DatabaseSeeder.cs`
- [ ] `KEEP` `src/backend/AuthServer/Data/Configurations/*.cs`
- [ ] `KEEP` `src/backend/AuthServer/Data/Entities/*.cs`
Why:
- EF/Identity tracked entities and framework infrastructure

#### Remove verified-unused candidates (batch 0)

- [x] `REMOVE` `src/backend/AuthServer/Services/UserService.cs`
- [x] `REMOVE` `src/backend/AuthServer/Services/Interfaces/IUserService.cs`
- [x] `REMOVE` `src/backend/AuthServer/Repositories/UserRepository.cs`
- [x] `REMOVE` `src/backend/AuthServer/Repositories/Interfaces/IUserRepository.cs`
Pre-checks:
- confirm no controllers/tests/docs depend on them
- remove DI registrations from `src/backend/AuthServer/Program.cs`

- [x] `REMOVE` unused `IUserRepository` dependency from `src/backend/AuthServer/Services/AuthenticationService.cs`
Notes:
- may be part of the user-service/repo removal batch

#### Review/remove candidate (needs explicit decision during implementation)

- [x] `REVIEW` `src/backend/AuthServer/Controllers/ScopeController.cs`
Questions to resolve during implementation:
- Is this endpoint intentionally kept for diagnostics/manual admin use?
- If kept: clean unused dependency and async/no-await issue
- If not used: remove controller + any docs references

### A3. `Orders` backend cleanup / keep inventory

#### Remove verified-unused candidate (batch 0)

- [x] `REMOVE` `src/backend/Orders/Services/AuthorizationService.cs`
Why:
- Not referenced by startup
- Superseded by `Shared.Authorization.Extensions.AddConfigurablePolicies(...)`

#### Keep (not record candidates)

- [ ] `KEEP` `src/backend/Orders/Controllers/OrdersController.cs`
- [ ] `KEEP` `src/backend/Orders/Services/OrdersService.cs`
- [ ] `KEEP` `src/backend/Orders/Services/IdempotencyService.cs`
- [ ] `KEEP` `src/backend/Orders/Repositories/OrdersRepository.cs`
- [ ] `KEEP` `src/backend/Orders/Middleware/IdempotencyMiddleware.cs`
- [ ] `KEEP` `src/backend/Orders/Data/OrdersDbContext.cs`
- [ ] `KEEP` `src/backend/Orders/Data/Entities/*.cs`
- [ ] `KEEP` `src/backend/Orders/Data/Configurations/*.cs`

#### Already modernized (record-heavy; review only)

- [ ] `KEEP/REVIEW` `src/backend/Orders/Models/**/*`
Notes:
- Largely records already
- review for `required/init` consistency only (no mass conversion needed)

### A4. `Shared` backend cleanup / keep inventory

#### Convert/review

- [ ] `CONVERT` `src/backend/Shared/Authorization/Models/AuthorizationConfig.cs` (`AuthorizationConfig`, `PolicyConfig`) in config batch

#### Keep (not record candidates)

- [ ] `KEEP` `src/backend/Shared/Authorization/Extensions/AuthorizationExtensions.cs`
- [ ] `KEEP` `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- [ ] `KEEP` `src/backend/Shared/Logging/**/*`
- [ ] `KEEP` `src/backend/Shared/OpenTelemetry/**/*`
- [ ] `KEEP` `src/backend/Shared/Common/*` (mostly structs/static helpers already; review only if touched)

---

## B. Frontend cleanup inventory (`src/web-admin`)

### B1. Auth/authz cleanup (in scope)

#### Remove deprecated path

- [x] `REMOVE` deprecated direct password login branch in `src/web-admin/contexts/auth-context.tsx`
Notes:
- branch currently redirects to OAuth flow and is marked deprecated
- remove dead signature/branch if no caller uses it

#### Consolidate scope/permission helpers

- [x] `CONVERT/REFACTOR` create `src/web-admin/lib/authz.ts` (new)
Recommended exports:
- `hasScope(scopes, scope)`
- `hasAnyScope(scopes, [...])`
- `canCreateOrder(scopes)`
- `canManageRoles(scopes)`
- `isAdminByScope(scopes)` (if needed)

- [x] `REFACTOR` `src/web-admin/components/auth/AuthGuard.tsx`
Notes:
- keep component as classless function (not TS class issue)
- move generic scope/role helper logic to `authz.ts` where sensible

- [x] `REFACTOR` `src/web-admin/lib/orders-api.ts`
Notes:
- remove/replace duplicated mixed role+scope `isAdmin()` heuristic or route through shared `authz.ts`

- [x] `REFACTOR` `src/web-admin/hooks/use-orders.ts`
Notes:
- use shared authz helper instead of local `ordersApi.isAdmin()` semantics if possible

- [x] `REFACTOR` `src/web-admin/components/app-sidebar.tsx`
Notes:
- use shared scope helper for admin-only nav visibility where practical

- [x] `REFACTOR` `src/web-admin/app/(authenticated)/admin/page.tsx`
Notes:
- keep scope-based guarding consistent via shared helper(s)

### B2. Frontend model/type hygiene

#### Review TypeScript interfaces/types for immutability/readability (not a blanket rewrite)

- [ ] `REVIEW` `src/web-admin/contexts/auth-context.tsx` (`User`, `AuthState`, `AuthContextType`)
Notes:
- consider extracting shared auth types to `src/web-admin/lib/types/auth.ts`
- prefer readonly properties where practical

- [ ] `REVIEW` `src/web-admin/lib/admin-roles-api.ts` exported interfaces
- [ ] `REVIEW` `src/web-admin/types/orders.ts`
Notes:
- evaluate `readonly` arrays/properties for response shapes where practical
- avoid churn unless there is concrete benefit

### B3. Frontend stale code/comments cleanup

- [ ] `REMOVE/REFACTOR` stale comments and flow remnants in `src/web-admin/lib/oauth2-auth.ts` (gateway/password-era leftovers if any remain)
- [ ] `REVIEW` `src/web-admin/lib/api.ts` and `oauth2-auth.ts` for duplicated scope/token persistence logic that can be centralized

---

## C. Solution/Project hygiene inventory

### C1. Solution file consistency

- [x] `REVIEW/ADD` `tests/backend/Shared.UnitTests/Shared.UnitTests.csproj` to `ArchZ.sln`
Why:
- prevent test-project drift
- include in solution-level build/test workflows

### C2. Project references and package hygiene

- [ ] `REVIEW` package references for obsolete/unneeded dependencies after removals (per project)
Examples to inspect:
- `src/backend/AuthServer/AuthServer.csproj`
- `src/backend/Orders/Orders.csproj`
- `src/backend/Shared/Shared.csproj`

---

## D. Documentation pattern establishment (required)

### D1. Coding conventions for modeling

- [x] Update `docs/AGENTS.MD` with modeling rules:
  - when to use `record`
  - when to use `class`
  - `init` and `required` guidance
  - cleanup obligation during refactors

- [x] Update `docs/DEVELOPMENT.md` with practical examples (`record`, `required`, config validation caveat)

- [x] Update `docs/ARCHITECTURE.md` with a short “Modeling Conventions” section (or link)

### D2. Cleanup/audit expectations

- [x] Document dead-code removal policy (verified-unused code should be removed in cleanup passes)
- [x] Document architecture-conformance review expectations for major refactors

---

## E. Analyzer / architecture hardening (recommended)

### E1. Analyzer baseline

- [ ] Add/standardize .NET analyzers settings across backend projects
- [ ] Decide which warnings to elevate (unused params, async-without-await, nullability)
- [ ] Add incremental enforcement plan (avoid big-bang warning cleanups)

### E2. Optional architecture tests (future-friendly)

- [ ] `REVIEW` add architecture tests (e.g., `NetArchTest.Rules`) for dependency-direction rules

---

## F. Validation checklist (execution)

### Backend

- [x] `dotnet build ArchZ.sln`
- [x] `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`
- [x] `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj`

### Frontend (because frontend cleanup is in scope)

- [x] `npx tsc --noEmit` in `src/web-admin`
- [ ] Run targeted Playwright specs for affected areas
- [ ] `npm run build` in `src/web-admin` (or explicitly document pre-existing lint debt if not resolved in this initiative)

---

## Execution notes / guardrails

- Do not convert EF/Identity entities to records.
- Do not convert controllers/services/middleware to records.
- For config models converted to records, add runtime validation in the same pass.
- Prefer staged commits/PRs even if implemented in one working branch.
- Remove verified-unused code and corresponding DI registrations/docs/tests in the same commit.

## Implementation log (to update during execution)

- 2026-02-26: Completed Batch 0 verified-unused removals (`AuthServer` user service/repository stack, stale `Orders` authorization service) and removed corresponding DI registrations/unused constructor dependency.
- 2026-02-26: Completed safe Batch 1 DTO/admin-model conversions to `sealed record` + `init/required` (commands/results/view model/admin roles DTOs). Adjusted `RoleAdminService.UpdateRoleScopesAsync(...)` null handling to avoid constructing a `required` request DTO.
- 2026-02-26: Validation: repo-wide reference sweep confirms removals are unused. `dotnet build src/backend/AuthServer/AuthServer.csproj -p:OutDir=C:\\dev\\luchdom\\identity-vibe\\.tmpbuild\\authserver\\` succeeds. Standard build output remains subject to local file lock if `AuthServer.exe` is running.
- 2026-02-26: Completed config-model conversion batch (`AuthServer` client/scope config + shared authorization policy config) with explicit `IValidateOptions<T>` validators and `ValidateOnStart()` fail-fast wiring in `AuthServer` and `Orders`.
- 2026-02-26: Completed frontend auth/authz consolidation batch: removed deprecated direct-password login branch in `auth-context`, added shared `src/web-admin/lib/authz.ts`, and refactored `AuthGuard`/sidebar/admin/orders hook + `ordersApi.isAdmin()` to use scope-based helpers. `npx tsc --noEmit` passes.
- 2026-02-26: Reviewed `ScopeController` and kept it as a diagnostics controller, but removed unused `IOpenIddictApplicationManager` injection and `async`/no-`await` warning debt.
- 2026-02-26: Added `tests/backend/Shared.UnitTests/Shared.UnitTests.csproj` to `ArchZ.sln` (solution drift cleanup).
- 2026-02-26: Broader validation completed: `dotnet build ArchZ.sln -p:OutDir=...` succeeds. `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj` passes (5/5). `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj -p:OutDir=...` passes (27/27).
- 2026-02-26: Adjusted config validators after integration-test regressions to preserve semantic empty-collection defaults for optional lists (`ScopeSettings.Resources`, `ClientSettings.RedirectUris`, `ClientSettings.PostLogoutRedirectUris`) while keeping required/runtime validation on truly required config values.
- 2026-02-26: Cleaned remaining .NET nullability warnings in `AuthServer` and `Orders` (`AuthorizationController`, `AuthorizationGrantService`, `OrdersController`) by adding explicit guard clauses/non-null locals after runtime checks. Validation rerun: `dotnet build ArchZ.sln -p:OutDir=...` now succeeds with `0 warnings, 0 errors`; backend tests still pass (`AuthServer.IntegrationTests` 27/27, `Shared.UnitTests` 5/5).
