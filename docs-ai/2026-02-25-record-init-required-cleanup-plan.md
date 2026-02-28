# Record/Init/Required Refactor + Full Cleanup Plan (Architecture & Best Practices)

Date: 2026-02-25
Status: Planning (pre-implementation)
Related:
- `ArchZ.sln`
- `src/backend/AuthServer/*`
- `src/backend/Orders/*`
- `src/backend/Shared/*`
- `tests/backend/*`
- docs: `README.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/AGENTS.MD`, `docs/TESTING.md`

## Goal

Perform a structured cleanup and modernization pass to:

1. Convert suitable class-based models to `record` types
2. Prefer `init`-only properties where mutation is not needed
3. Replace unnecessary default-value initialization patterns with stronger construction/validation patterns (`required`, factories, validation)
4. Remove/refactor stale or unused code left from prior refactors
5. Reinforce architecture and coding conventions in docs and (ideally) CI/analyzers
6. Include frontend TypeScript cleanup/consolidation where it improves architecture consistency (authz helpers, deprecated paths, model hygiene)

## High-level feasibility (analysis result)

### Short answer
Yes, **many models can be converted**, but not "most classes" across the whole solution.

### What is realistic to convert safely
Good candidates:
- DTOs / request/response models (especially API JSON models)
- command/query models
- result models (when immutable)
- view models used as read models
- configuration models (with caveats)
- test contract models / helper data models

### What should generally remain classes (for now)
Not good candidates for record conversion in this solution:
- EF Core entities (especially tracked aggregates and Identity/OpenIddict entities)
- `DbContext` types
- ASP.NET Controllers
- Middleware classes
- Service/repository implementations
- EF entity configurations (`IEntityTypeConfiguration<T>`)
- Infrastructure wrappers/factories with mutable lifecycle state

## Why not convert everything to records

### 1. EF Core / Identity entities are mutable and tracked
Examples:
- `src/backend/AuthServer/Data/Entities/*`
- `src/backend/Orders/Data/Entities/*`

Reasons to keep as `class`:
- EF change tracking and materialization patterns
- navigation property mutation
- Identity base types (`IdentityUser`, `IdentityRole`, etc.) are class-based and mutable
- record value equality can be misleading/dangerous for tracked entities

### 2. MVC/Razor form models are trickier than JSON DTOs
Examples:
- `src/backend/AuthServer/Models/Requests/LoginRequest.cs`
- `src/backend/AuthServer/Models/Requests/RegisterRequest.cs`

These are used in Razor views and model binding for forms. They *can* be modernized, but are not the best first targets for `record + init + required` because:
- form model binding and view-postback workflows often rely on mutable setters
- some fields are populated after failed submit (`ErrorMessage`, `ReturnUrl`)

### 3. `required` does not replace runtime validation
Important nuance:
- `required` is primarily compile-time enforcement (construction-time)
- config binding/model binding still need runtime validation (`DataAnnotations`, `IValidateOptions`, custom validation)

## Current codebase snapshot (relevant findings)

### Already modernized (good baseline)
- `Orders` model layer already uses records heavily (`Commands`, many `Requests`, `Responses`, `Results`, `Queries`, `ViewModels`)
- several `AuthServer` models already use records (e.g., `GrantFlowResult`, some responses)

### Remaining class-based model/config candidates (high-value)
Observed class-based candidates in model/config areas:
- `src/backend/AuthServer/Configuration/ClientConfiguration.cs`
- `src/backend/AuthServer/Configuration/ClientSettings.cs`
- `src/backend/AuthServer/Configuration/ScopeSettings.cs`
- `src/backend/AuthServer/Models/Commands/LoginCommand.cs`
- `src/backend/AuthServer/Models/Commands/RegisterCommand.cs`
- `src/backend/AuthServer/Models/Results/LoginResult.cs`
- `src/backend/AuthServer/Models/Results/RegisterResult.cs`
- `src/backend/AuthServer/Models/ViewModels/UserViewModel.cs`
- `src/backend/AuthServer/Models/AdminRoles/*.cs` (new admin DTOs currently classes)
- `src/backend/Shared/Authorization/Models/AuthorizationConfig.cs` (`AuthorizationConfig`, `PolicyConfig`)

### Likely stale/unused code found (cleanup candidates)
1. `src/backend/Orders/Services/AuthorizationService.cs`
- Not referenced by `Orders` startup
- Duplicates old auth policy registration approach
- Current app uses `Shared.Authorization.Extensions.AddConfigurablePolicies(...)`
- Strong candidate for removal

2. `src/backend/AuthServer/Services/UserService.cs` + `IUserService`
- Registered in DI, but no controller usage found
- Candidate for removal if not intentionally reserved for upcoming endpoints

3. `src/backend/AuthServer/Repositories/UserRepository.cs` + `IUserRepository`
- Registered in DI
- Only referenced by `UserService` and injected into `AuthenticationService`
- `AuthenticationService` currently does not use the `IUserRepository` dependency
- Candidate for removal/simplification depending on decision for `UserService`

4. `src/backend/AuthServer/Services/AuthenticationService.cs`
- Has unused injected dependency `IUserRepository`
- At minimum remove unused dependency; possibly simplify service dependencies

5. `src/backend/AuthServer/Controllers/ScopeController.cs`
- No frontend references found
- Contains unused injected dependency (`IOpenIddictApplicationManager`)
- Contains an `async` method with no `await`
- Candidate for deprecation/removal or scope-limiting (dev/admin-only) if not needed

6. `src/web-admin/contexts/auth-context.tsx`
- Deprecated direct password-login branch still present (commented as deprecated)
- Candidate for removal in a cleanup pass (frontend)

7. Solution/test drift
- `tests/backend/Shared.UnitTests` exists but is **not listed** in `ArchZ.sln`
- Candidate to add to solution for consistency, or explicitly document as excluded

## Proposed standards (what to convert, how to model properties)

### A. Type selection guidelines

#### Use `record` / `sealed record` for immutable data carriers
Examples:
- API request/response DTOs (JSON endpoints)
- commands/queries
- results (if pure data)
- projections/view models (read-only)

Preferred form:
- `public sealed record Xyz` for reference types
- `public readonly record struct` only for tiny value objects (rare in this codebase)

#### Keep `class` for framework/infrastructure/mutable tracked objects
Examples:
- EF entities
- controllers
- services
- middleware
- configs that must be incrementally built/mutated during binding (unless validated and converted carefully)

### B. Property mutability guidelines

#### Prefer `init` for immutable DTOs
Example pattern:
```csharp
public sealed record CreateRoleRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true; // optional default when semantic default exists
}
```

#### Keep `set` when model binding/UI workflows require mutation
Examples:
- Razor form request models with postback state (`LoginRequest`, `RegisterRequest`)
- EF entities

### C. `required` vs default value guidelines (important)

Use `required` for:
- non-null scalar inputs that must be provided by callers
- DTOs constructed in code where compile-time enforcement is valuable

Keep defaults (do not force `required`) for:
- optional collections where empty is a valid/default semantic (`[]`, `new List<string>()`)
- framework-bound models where binder may omit fields and runtime validation handles it
- booleans with intentional domain defaults (`RequirePkce = true`, etc.)

Use runtime validation in addition to `required` for:
- config binding (`ValidateOnStart`, `DataAnnotations`, custom `IValidateOptions`)
- incoming HTTP requests (DataAnnotations / FluentValidation if adopted)

### D. Equality guidance

- Do not rely on record equality for EF entities.
- Record equality is good for command/query/result DTOs and test fixtures.

## Detailed implementation plan (recommended order)

### Workstream 1: Inventory and classification (no behavior change)

Tasks:
1. Build an inventory of all public classes/records across backend projects.
2. Classify into buckets:
- `EF Entity / Tracked`
- `Infra / Service / Controller / Middleware`
- `Config model`
- `API DTO`
- `Command/Query/Result`
- `ViewModel/Projection`
- `Test contract/helper`
3. Mark conversion eligibility:
- `safe`
- `safe with runtime validation changes`
- `defer`
- `do not convert`

Deliverable:
- Inventory table (markdown in `ai-analysis`) or checklist with file-by-file classification.

### Workstream 2: Safe record conversion batch (low risk, high value)

Target files (recommended initial batch):
- `src/backend/AuthServer/Models/Commands/LoginCommand.cs`
- `src/backend/AuthServer/Models/Commands/RegisterCommand.cs`
- `src/backend/AuthServer/Models/Results/LoginResult.cs`
- `src/backend/AuthServer/Models/Results/RegisterResult.cs`
- `src/backend/AuthServer/Models/ViewModels/UserViewModel.cs`
- `src/backend/AuthServer/Models/AdminRoles/*.cs`
- `src/backend/Shared/Authorization/Models/AuthorizationConfig.cs` (optional in batch 1; see Workstream 3 caveats)
- `tests/backend/AuthServer.IntegrationTests/Contracts/TokenResponse.cs` (test-only, low risk)

Tasks:
1. Convert eligible classes to `sealed record`.
2. Replace mutable `set` with `init` where safe.
3. Replace `= string.Empty` for required inputs with `required` where construction is code-driven.
4. Update object initializers/mappers and tests.
5. Run backend builds/tests.

Acceptance checks:
- No behavior changes
- Serialization/model binding still works for affected API DTOs
- Tests pass

### Workstream 3: Configuration model modernization (medium risk; binder/validation sensitive)

Target files:
- `src/backend/AuthServer/Configuration/ClientConfiguration.cs`
- `src/backend/AuthServer/Configuration/ClientSettings.cs`
- `src/backend/AuthServer/Configuration/ScopeSettings.cs`
- `src/backend/Shared/Authorization/Models/AuthorizationConfig.cs`

Tasks:
1. Decide whether to convert config models to records now or later.
2. If converting:
- use `record` + `init`
- keep collection defaults where empty is semantically valid
- use `required` only for truly required scalars (carefully)
3. Add runtime options validation:
- `ValidateDataAnnotations()` and/or custom validation
- `ValidateOnStart()` where configuration must be present
4. Document that `required` is not sufficient for config correctness.

Acceptance checks:
- App startup fails fast with clear message for invalid/missing config
- Existing appsettings bind successfully

### Workstream 4: MVC/Razor model review (defer-by-default, selective)

Target examples:
- `src/backend/AuthServer/Models/Requests/LoginRequest.cs`
- `src/backend/AuthServer/Models/Requests/RegisterRequest.cs`

Recommendation:
- Do **not** mass-convert Razor form request models in the first pass.
- Keep as mutable classes unless there is a clear proven benefit and form binding/view behavior is verified.

If touched:
- Prefer DataAnnotations for runtime validation
- Avoid mixing `required` conversion with form-flow changes in same PR

### Workstream 5: Full cleanup / dead code audit and removal (high value)

#### 5.1 Remove or justify unused/stale code (backend)
Candidate list (from current analysis):
- `src/backend/Orders/Services/AuthorizationService.cs` (unused)
- `src/backend/AuthServer/Services/UserService.cs` + interface (likely unused)
- `src/backend/AuthServer/Repositories/UserRepository.cs` + interface (likely unused if UserService removed)
- unused `IUserRepository` dependency in `AuthenticationService`
- `src/backend/AuthServer/Controllers/ScopeController.cs` (verify necessity)

Tasks:
1. Verify no runtime/tests/docs depend on each candidate.
2. Remove or deprecate with explicit comment and issue link (if intentionally retained).
3. Remove stale DI registrations from `Program.cs`.
4. Update docs if endpoints are removed.

Acceptance checks:
- No broken references
- Solution builds/tests pass
- Startup DI graph remains clean

#### 5.2 Frontend cleanup candidates (in scope; user-confirmed)
Candidates found:
- Deprecated direct password login branch in `src/web-admin/contexts/auth-context.tsx`
- legacy/duplicated auth helper semantics (`ordersApi.isAdmin()` mixed role/scope heuristics) -> centralize authz helpers over time

Tasks:
- Remove deprecated paths if no UI depends on them
- Consolidate scope-based permission helpers in one place (`authz.ts`)
- Replace ad-hoc role/scope checks in pages/components with shared helpers
- Normalize frontend model types/interfaces where immutable patterns are appropriate (TypeScript side)
- Remove stale UI code/comments left from gateway-era and password-login-era flows

Acceptance checks:
- Frontend authz checks are consistent and scope-first
- Deprecated login branch removed (if no longer used)
- No duplicated permission heuristics remain in `ordersApi` / page components

### Workstream 6: Architecture conformance audit (enforce the documented architecture)

Goal:
- Ensure actual code matches `docs/ARCHITECTURE.md` and current RBAC/token design.

Tasks:
1. Review controllers/services for role-vs-scope authorization mismatches.
2. Review config and docs for outdated claims about source of truth (config vs DB).
3. Review cross-project dependency directions (AuthServer/Orders/Shared).
4. Optionally add architecture tests (recommended) using `NetArchTest.Rules`:
- Controllers depend on services/interfaces, not repositories directly (if desired convention)
- `Shared` does not depend on app projects
- Domain/model layers avoid infrastructure references where intended

Acceptance checks:
- Architecture docs align with runtime behavior
- Optional architecture tests added and passing

### Workstream 7: Analyzer and quality-gate hardening (prevents regression)

Tasks:
1. Enable/standardize .NET analyzers across projects:
- `EnableNETAnalyzers`
- `AnalysisLevel`
2. Gradually tighten warnings:
- start with info/suggestion reports
- then treat selected warnings as errors
3. Consider `WarningsAsErrors` in CI for new code only (incremental approach)
4. Add solution hygiene checks:
- ensure all intended test projects are in solution (`tests/backend/Shared.UnitTests` currently appears excluded)

Suggested analyzers/rules to emphasize:
- unused parameters/fields (IDE0060/IDE0052 etc.)
- nullability issues
- async method lacks await
- unnecessary allocations/object initializers where applicable

### Workstream 8: Documentation / coding standard update (establish the pattern)

Update docs to make this a project standard:
- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md` (brief modeling principles section)
- `docs/DEVELOPMENT.md` (practical coding conventions)
- optional `README.md` (short pointer only)

Document these rules explicitly:
1. Prefer `sealed record` for immutable DTOs/commands/queries/results
2. Prefer `init` over `set` for immutable data carriers
3. Use `required` for required inputs constructed in code
4. Keep defaults for optional collections/semantic defaults
5. Keep EF entities/controllers/services/middleware as classes unless a deliberate design change is made
6. Add runtime validation for config/request models; do not rely on `required` alone
7. Remove dead code + stale DI registrations during refactors (cleanup obligation)

### Workstream 9: Validation and regression testing

Validation commands (minimum):
- `dotnet build ArchZ.sln`
- `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`
- `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj` (add to solution or run directly)

If cleanup touches Orders behavior:
- add/run Orders tests (when available)

If frontend cleanup is included:
- `npx tsc --noEmit` in `src/web-admin`
- targeted Playwright specs for affected pages
- `npm run build` if/when pre-existing lint debt is addressed (or lint run with scoped suppression plan)

## Suggested rollout strategy (pragmatic)

### Option A (recommended): staged cleanup in multiple PRs/commits
1. Inventory + standards docs
2. Safe record conversion batch (DTOs/commands/results)
3. Config model modernization + validation
4. Dead code removal batch
5. Analyzer/architecture hardening

Pros:
- easier review
- lower regression risk
- clear rollback points

### Option B: single large cleanup pass
Pros:
- faster end state
Cons:
- harder debugging/review if regressions appear
- mixes behavior, structure, and cleanup concerns

Recommendation: **Option A**

## Concrete suggestions (high-impact, low-regret)

1. Start with AuthServer model/config cleanup, not entities
- Biggest readability gain with lowest runtime risk

2. Remove truly unused code before mass record conversion
- Deleting `Orders/Services/AuthorizationService.cs` and unused AuthServer user service/repo stack reduces noise before refactor

3. Add `Shared.UnitTests` to `ArchZ.sln`
- Prevent test drift and missed checks in solution-level runs

4. Centralize frontend scope checks (`authz.ts`)
- Avoid repeating mixed role/scope checks like `ordersApi.isAdmin()`

5. Add options validation before aggressive `required` usage in config models
- Prevent false confidence from compile-time-only guarantees

6. Treat EF entities as a separate future modernization stream
- If you want richer domain modeling later, do that intentionally (constructors/factories/private setters), not via blanket record conversion

## Open questions / decisions to confirm (one-by-one recommended)

### Q1. Refactor scope size
Do you want this cleanup initiative to include **frontend TypeScript cleanup** (deprecated auth branches, authz helper consolidation, etc.) in the same effort, or keep it **.NET/backend-only** for now?

Options:
1. **Backend/.NET only first (Recommended)**
- keeps scope focused on records/init/required + backend cleanup

2. **Include frontend cleanup too**
- broader cleanup win
- larger scope and review surface

Decision (user-confirmed):
- **Option 2**: Include frontend TypeScript cleanup in the same initiative.

### Q2. Unused code policy for this pass
For likely-unused components (`Orders.AuthorizationService`, `AuthServer.UserService/UserRepository`, maybe `ScopeController`), should we:

Options:
1. **Remove immediately if verified unused (Recommended)**
2. **Keep but mark obsolete/deprecated**
3. **Only document candidates; defer removal**

Decision (user-confirmed):
- **Option 1**: Remove immediately if verified unused.

### Q3. Config model conversion timing
Should config binding models (`ClientSettings`, `ScopeSettings`, `AuthorizationConfig`) be included in the first record conversion batch?

Options:
1. **Defer config models to a second pass (Recommended)**
- convert DTOs/commands/results first
- add validation before touching config binders

2. **Include config models in first pass**
- faster consistency
- more binder/validation risk

Decision (user-confirmed):
- **Option 2**: Include config binding models in the first record conversion batch.

Implementation note (recommended):
- Pair config model conversion with runtime options validation (`ValidateOnStart`, DataAnnotations/custom validators) in the same pass.

## Acceptance criteria (overall initiative)

- Clear modeling standard documented (record/init/required with exceptions)
- Safe DTO/model classes converted to `record`/`init` where appropriate
- No EF entities/controllers/middleware mistakenly converted
- Stale/unused code candidates audited and resolved (removed or explicitly justified)
- Solution and tests remain green
- Architecture docs and runtime code are aligned
