# Tasks: Services And Docs Architecture Alignment

## Scope Lock

- Source plan: `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- Architecture source vocabulary: `Request`, `Response`, `Command`, `Query`, `Result`, `Entity`, `Service`, `Repository`, `Validator`, `Configuration`
- Fixed clarification decisions:
  - root `AGENTS.md` is the agent entrypoint/router and the canonical workflow-rules document
  - create `docs/BACKEND.md`
  - merge useful rules from `docs/AGENTS.MD` into the new doc structure
  - clean up `docs/` explicitly using a keep/merge/trim/redirect/archive/delete inventory
  - keep `Configuration/` as a top-level backend folder and document it as an approved exception
  - refactor `AuthServer` and `Orders` equally in the first pass
  - replace custom `Shared.Common.Result` with Ardalis Result
  - Ardalis `Result` is a service-layer contract; repositories should return persistence/domain values instead
  - fully rename backend `ViewModel` types to the architecture vocabulary in this pass
  - add a small automated `Orders` integration/smoke verification layer in this phase
  - where possible, DTO-style models should use positional `sealed record` syntax
  - include lightweight architectural enforcement in this phase

## Delivery Notes

- This breakdown is intended to be executable by one or more agents with minimal ambiguity.
- Preserve runtime behavior unless a task explicitly says otherwise.
- Prefer small coherent commits/PR slices even if implemented in one branch.
- Do not leave half-migrated naming or result-contract usage behind between backend projects.

## Task 1. Build the discovery and docs-cleanup inventory

### Goal

Create a concrete current-state inventory that captures the real backend shape, naming mismatches, result usage, and stale docs before any structural edits begin.

### Likely files

- new `docs-ai/2026-03-26-services-docs-architecture-alignment-inventory.md`
- `src/backend/AuthServer/*`
- `src/backend/Orders/*`
- `src/backend/Shared/*`
- `README.md`
- `CLAUDE.md`
- `docs/*.md`

### Work

1. Inventory the current folder structure for `AuthServer`, `Orders`, and `Shared`.
2. Record where DI/startup responsibilities currently live.
3. Record where custom `Result`/`Result<T>` and `ResultExtensions` are used.
4. Record all backend `ViewModel` types and propose their target names.
5. Audit the `docs/` tree and classify each significant doc as:
   - keep
   - merge
   - trim
   - redirect
   - archive
   - delete
6. Produce a current-to-target mapping table for backend folders and model categories.

### Acceptance criteria

- An inventory doc exists under `docs-ai/`.
- The inventory lists current backend folders, naming issues, result usage hotspots, and docs cleanup decisions.
- The inventory is specific enough that later tasks can reference it directly.

### Test notes

- No code tests required.
- Validate by manual review that the inventory cites actual repo files and decisions.

### Dependencies

- None. This task should happen first.

## Task 2. Create the new doc information architecture

### Goal

Define and implement the new top-level documentation structure so contributors and agents know where to read backend, architecture, testing, and workflow guidance.

### Likely files

- new `AGENTS.md`
- new `docs/BACKEND.md`
- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md`
- `README.md`
- `CLAUDE.md`

### Work

1. Create root `AGENTS.md` as a short router/entrypoint.
2. Add explicit routing guidance in root `AGENTS.md`, for example:
   - backend/.NET work -> `docs/BACKEND.md`
   - frontend work -> `docs/FRONTEND.md`
   - system/runtime architecture -> `docs/ARCHITECTURE.md`
   - development/local setup -> `docs/DEVELOPMENT.md`
   - deployment -> `docs/DEPLOYMENT.md`
   - testing -> `docs/TESTING.md`
3. Keep the current workflow rules in root `AGENTS.md`, including:
   - Plan -> Clarify -> Task it out -> Audit -> Implement
   - required planning format
   - approval gate before implementation
4. Create `docs/BACKEND.md` as the backend implementation source of truth.
5. Merge the actionable backend and workflow rules from `docs/AGENTS.MD` into the correct destination docs.
6. Reduce `docs/AGENTS.MD` to either:
   - a trimmed companion file, or
   - a redirect/deprecation stub
7. Update `README.md` and `CLAUDE.md` to point to the new canonical docs.

### Acceptance criteria

- Root `AGENTS.md` exists and clearly routes agents to the correct docs.
- Root `AGENTS.md` preserves the repo workflow rules as the canonical source of truth.
- `docs/BACKEND.md` exists and is treated as the backend implementation guide.
- `docs/AGENTS.MD` no longer duplicates large bodies of backend guidance unnecessarily.
- Root and `/docs` guidance no longer conflict on where backend/agent instructions live.

### Test notes

- Manual doc review only.
- Confirm all links/paths mentioned in root `AGENTS.md` exist.

### Dependencies

- Task 1 inventory should be available first.

## Task 3. Normalize architecture and naming documentation

### Goal

Document the canonical backend architecture, naming rules, layer responsibilities, exceptions, and current-to-target mapping in a way that is directly usable during implementation and review.

### Likely files

- `docs/ARCHITECTURE.md`
- `docs/BACKEND.md`
- `docs-ai/arch.md`
- optionally `README.md` for high-level cross-links

### Work

1. Clean up `docs/ARCHITECTURE.md` so it focuses on:
   - runtime topology
   - system-level request flow
   - service boundaries
   - approved repo-specific exceptions
2. Add to `docs/BACKEND.md`:
   - backend folder/responsibility table
   - naming conventions table
   - model-shape conventions section
   - allowed dependencies by layer
   - forbidden responsibilities by layer
   - examples using the architecture vocabulary only
3. Document the approved exceptions explicitly:
   - top-level `Configuration/`
   - AuthServer MVC/Razor assets
   - `Shared` as the current cross-service building-block project name
4. Add a current-to-target mapping appendix showing how existing folders/types map into the intended architecture.
5. Ensure `docs-ai/arch.md` and the repo docs are aligned in terminology.

### Acceptance criteria

- `docs/BACKEND.md` contains a clear naming table and layer-purpose table.
- `docs/BACKEND.md` documents the positional-record default for DTO-style models and the allowed exceptions.
- `docs/ARCHITECTURE.md` is cleaned up and no longer overloaded with backend implementation detail better suited for `docs/BACKEND.md`.
- The documented examples use only the architecture vocabulary already established by the plan.
- The documented examples use only the approved architecture vocabulary.

### Test notes

- Manual doc review only.
- Verify examples and labels do not reintroduce deprecated backend naming such as `ViewModel`.

### Dependencies

- Task 2 should be completed first.

## Task 4. Clean up the remaining docs tree

### Goal

Remove or consolidate deprecated and low-value docs so the surviving docs tree is smaller, clearer, and easier to trust.

### Likely files

- `docs/*.md`
- `README.md`
- `CLAUDE.md`
- `DOCKER.md`

### Work

1. Apply the Task 1 inventory decisions across the docs tree.
2. Merge duplicated content into canonical docs rather than leaving near-copies behind.
3. Replace legacy docs with short redirect stubs where references may still exist.
4. Remove outdated references to:
   - `web-admin` as the active frontend target
   - contradictory service names/ports where stale
   - outdated workflow/doc ownership notes
5. Ensure development/deployment/testing docs refer back to `docs/BACKEND.md` or `docs/ARCHITECTURE.md` instead of restating backend rules inconsistently.

### Acceptance criteria

- The docs tree has a clear canonical destination for backend, architecture, development, deployment, testing, and agent entrypoint guidance.
- Deprecated or duplicate docs are either removed or clearly marked as redirects.
- Root docs and `docs/` no longer conflict on the active frontend target or backend guidance ownership.

### Test notes

- Manual review only.
- Spot-check internal links and referenced file paths.

### Dependencies

- Tasks 1-3.

## Task 5. Introduce Ardalis Result as the standard backend result contract

### Goal

Replace the custom `Shared.Common.Result` stack with Ardalis Result while preserving current API behavior and error semantics.

### Likely files

- `src/backend/Shared/Shared.csproj`
- `src/backend/Orders/Orders.csproj`
- `src/backend/AuthServer/AuthServer.csproj`
- `src/backend/Shared/Common/Result.cs`
- `src/backend/Shared/Common/ResultVoid.cs`
- `src/backend/Shared/Common/ResultExtensions.cs`
- `src/backend/Shared/Common/CommonErrors.cs`
- backend service interfaces and implementations
- backend controller translation helpers

### Work

1. Add the Ardalis Result package to the appropriate projects.
2. Decide the canonical place for shared Ardalis-to-HTTP translation helpers.
3. Map current error semantics onto Ardalis statuses/metadata without changing public API behavior.
4. Migrate `AuthServer` and `Orders` service contracts from custom `Result` types to Ardalis Result.
5. Refactor repository contracts away from custom `Result` usage toward plain persistence/domain return values.
6. Replace `ToActionResultWithProblemDetails`-style usage with Ardalis-aware helpers.
7. Remove the custom result structs only after all usages are migrated.
8. Update docs/examples to reflect Ardalis Result usage.

### Acceptance criteria

- No backend service contract still depends on the old custom result structs.
- Repository contracts no longer depend on the old custom result structs.
- Controller responses still preserve the intended HTTP semantics and problem-details format.
- The custom `Shared.Common.Result` implementation is removed or fully retired from active use.

### Test notes

- `dotnet build ArchZ.sln`
- `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj`
- `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj` for auth-impacting result translation paths
- run any additional unit tests added for result translation helpers

### Dependencies

- Tasks 1-4 should already define the canonical vocabulary and docs.

## Task 6. Extract composition roots from both backend services

### Goal

Move service and infrastructure registration out of `Program.cs` into a predictable `Composition` structure for both `AuthServer` and `Orders`.

### Likely files

- `src/backend/Orders/Program.cs`
- `src/backend/AuthServer/Program.cs`
- new `src/backend/Orders/Composition/DependencyInjection/ServiceCollectionExtensions.cs`
- new `src/backend/AuthServer/Composition/DependencyInjection/ServiceCollectionExtensions.cs`

### Work

1. Create the `Composition/DependencyInjection` structure in both services.
2. Move DI registrations and option validation registrations out of `Program.cs`.
3. Keep `Program.cs` responsible for:
   - host setup
   - configuration loading
   - middleware pipeline
   - startup orchestration
4. Ensure the extracted registration code still wires all required services, repositories, middleware, and options.

### Acceptance criteria

- Both backend services have a `Composition` entry point.
- Both `Program.cs` files are materially simpler and no longer act as dumping grounds for all registrations.
- App startup behavior is unchanged.

### Test notes

- `dotnet build ArchZ.sln`
- manual startup smoke for both services
- run backend tests affected by DI/configuration changes

### Dependencies

- Task 5 should land first or be coordinated carefully, since service registrations will change alongside result-contract migration.

## Task 7. Refactor Orders to the target structure

### Goal

Bring `Orders` into conformance with the documented service-layer architecture, including folder layout, naming, validators, mappings, and service/controller boundaries.

### Likely files

- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/Orders/Services/*`
- `src/backend/Orders/Services/Interfaces/*`
- `src/backend/Orders/Repositories/*`
- `src/backend/Orders/Data/*`
- `src/backend/Orders/Models/*`
- new `src/backend/Orders/Validators/*`
- new `src/backend/Orders/Common/*` if needed

### Work

1. Move repositories under `Data/Repositories/` and update namespaces/usings.
2. Move repository interfaces under `Data/Repositories/Interfaces/`.
3. Audit all backend `ViewModel` types and rename them to the architecture vocabulary:
   - `Request`
   - `Response`
   - `Command`
   - `Query`
   - `Result`
4. Convert DTO-style models to positional `sealed record` syntax where practical and aligned with the documented rule.
5. Reorganize mapping classes to fit the intended responsibility split.
6. Add explicit validators for request/command validation.
7. Reduce repeated controller logic where possible while keeping controllers thin.
8. Keep business logic in services and persistence in repositories.
9. Ensure no `DbContext` access leaks into controllers.

### Acceptance criteria

- `Orders` repositories live under `Data/Repositories`.
- `Orders` no longer has active backend `ViewModel` naming.
- `Orders` DTO-style models use positional `sealed record` syntax where practical, with exceptions kept intentional.
- Validators exist for the intended request/command validation layer.
- Controllers remain thin and services own business logic.

### Test notes

- `dotnet build ArchZ.sln`
- `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj`
- add/update focused unit tests for validators and services
- add/run a small automated `Orders` integration or smoke suite for the core endpoints touched by the refactor
- manual smoke may remain supplemental, but not the only endpoint-level verification

### Dependencies

- Tasks 5 and 6 first.

## Task 7A. Add minimal Orders integration/smoke coverage for the refactor surface

### Goal

Create a small automated verification layer for the `Orders` endpoints most at risk during the architecture refactor.

### Likely files

- new or expanded `tests/backend/Orders.*`
- possibly `ArchZ.sln`
- any shared test helpers needed for `Orders`

### Work

1. Choose the smallest maintainable integration/smoke harness for `Orders`.
2. Cover the core refactor-sensitive endpoints and behaviors, at minimum:
   - a representative read path
   - a representative write path
   - error/problem-details behavior on a key failure path
3. Ensure the suite is practical to run locally during refactor work.
4. Wire the test project into the solution if a new project is created.

### Acceptance criteria

- `Orders` has at least a minimal automated endpoint-level verification layer.
- The covered endpoints exercise the controller/service/repository seams being refactored.
- The suite is part of the documented validation flow for this work.

### Test notes

- `dotnet build ArchZ.sln`
- run the new `Orders` integration/smoke suite explicitly

### Dependencies

- Tasks 5 and 6 first, or coordinate closely if the suite is built in parallel with Task 7.

## Task 8. Refactor AuthServer to the target structure

### Goal

Bring `AuthServer` into the same documented architecture vocabulary and structure while preserving its interactive auth-specific exceptions.

### Likely files

- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/Services/*`
- `src/backend/AuthServer/Services/Interfaces/*`
- `src/backend/AuthServer/Models/*`
- `src/backend/AuthServer/Data/*`
- new `src/backend/AuthServer/Composition/*`

### Work

1. Apply the `Composition` structure.
2. Audit backend `ViewModel` usage and fully rename to the architecture vocabulary where appropriate.
3. Convert DTO-style models to positional `sealed record` syntax where practical and aligned with the documented rule.
4. Keep MVC/Razor assets intact, but document them as intentional exceptions.
5. Keep `Configuration/` top-level and ensure docs reflect that clearly.
6. Re-check service/controller boundaries and Ardalis Result usage across admin/auth flows.

### Acceptance criteria

- `AuthServer` uses the same naming vocabulary and composition approach as `Orders`.
- Backend `ViewModel` naming is removed from active `AuthServer` backend contracts.
- `AuthServer` DTO-style models use positional `sealed record` syntax where practical, with exceptions kept intentional.
- Interactive auth UI behavior remains intact.

### Test notes

- `dotnet build ArchZ.sln`
- `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`
- `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj`
- manual smoke for login, token, and admin endpoints if needed

### Dependencies

- Tasks 5 and 6 first.

## Task 9. Clarify and enforce Shared vs service-local ownership

### Goal

Document and apply the rule for what belongs in `Shared` versus what must stay in service-local `Common`, `Models`, `Data`, or `Configuration`.

### Likely files

- `src/backend/Shared/*`
- `docs/BACKEND.md`
- `docs/ARCHITECTURE.md`

### Work

1. Audit what remains in `Shared` after Ardalis migration and service refactors.
2. Move service-specific code out of `Shared` if any is clearly misplaced.
3. Document the allowed contents of `Shared`.
4. Document what belongs in service-local layers instead.

### Acceptance criteria

- `Shared` only contains genuinely cross-service concerns.
- The docs clearly describe what should and should not be added to `Shared`.

### Test notes

- `dotnet build ArchZ.sln`
- run affected unit/integration tests after any code movement

### Dependencies

- Tasks 5, 7, and 8.

## Task 10. Add lightweight architectural enforcement

### Goal

Add focused checks so the documented rules are not purely advisory.

### Likely files

- `tests/backend/Orders.UnitTests/*`
- `tests/backend/AuthServer.IntegrationTests/*`
- possibly new lightweight architecture-oriented tests/projects
- `docs/BACKEND.md`

### Work

1. Add simple checks or focused tests around the highest-value architecture rules, such as:
   - controllers do not depend on repositories directly
   - backend contracts do not use `ViewModel`
   - DTO-style backend models follow the positional `sealed record` rule where applicable
   - expected naming/layer boundaries are respected in key areas
2. Add reviewable architecture rules to docs so reviewers know what to enforce manually.
3. Keep the enforcement lightweight and maintainable; do not introduce heavy architecture tooling unless clearly justified.

### Acceptance criteria

- There is at least a minimal automated or semi-automated guardrail for naming/layer drift.
- The checks are focused and understandable rather than broad, brittle meta-tooling.

### Test notes

- Run the newly added checks/tests locally.
- Ensure they pass in addition to normal backend tests.

### Dependencies

- Tasks 7, 7A, 8, and 9.

## Task 11. Final verification and doc deprecation sweep

### Goal

Finish the cleanup by verifying the new doc structure, backend runtime behavior, and architecture consistency end-to-end.

### Likely files

- `AGENTS.md`
- `README.md`
- `CLAUDE.md`
- `docs/*.md`
- affected backend projects/tests

### Work

1. Re-run the docs cleanup inventory and confirm all planned decisions were executed.
2. Re-check root `AGENTS.md` routing against the final docs tree.
3. Re-check `README.md` and `CLAUDE.md` against the final doc ownership model.
4. Run the agreed validation commands.
5. Record any intentional exceptions or deferred cleanup items that remain.

### Acceptance criteria

- The final docs tree is coherent and navigable.
- The backend builds and test suites pass at the agreed level.
- Remaining exceptions/deferred items are explicitly documented, not silently left behind.

### Test notes

- `dotnet build ArchZ.sln`
- `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj`
- `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj`
- `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj`

### Dependencies

- All earlier tasks.

## Suggested implementation order

1. Task 1
2. Task 2
3. Task 3
4. Task 4
5. Task 5
6. Task 6
7. Task 7 and Task 8 in parallel if ownership is split cleanly
8. Task 7A
9. Task 9
10. Task 10
11. Task 11

## Parallelization opportunities

- Task 2 and Task 3 can overlap slightly once the inventory exists, but the canonical doc split should be decided before broad cleanup.
- Task 7 and Task 8 can run in parallel if separate implementers own `Orders` and `AuthServer` respectively.
- Task 7A can overlap with late Task 7 work if the test owner has a stable enough endpoint surface to target.
- Task 10 can begin once enough of Tasks 7, 7A, 8, and 9 have landed to define stable boundaries worth checking.

## Cross-cutting risks to watch during execution

- Ardalis Result migration can accidentally change HTTP status or problem-details behavior.
- Full `ViewModel` renaming can create broad namespace churn; avoid partial half-states.
- Docs cleanup can delete useful context if merges are not done carefully.
- Equal first-pass refactoring across both services increases coordination risk; keep ownership explicit.
- `Orders` still has less mature automated coverage than `AuthServer`, so the new smoke/integration layer should land early enough to reduce blind refactor risk.

## Minimum validation bar before calling the work done

- Canonical doc structure exists and is linked from root `AGENTS.md`.
- `docs/BACKEND.md` contains naming and layer responsibility guidance.
- `docs/BACKEND.md` documents the positional-record model convention.
- `docs/ARCHITECTURE.md` is cleaned up and aligned.
- Custom `Shared.Common.Result` usage is removed from active backend contracts.
- Backend `ViewModel` names are removed from active backend contracts.
- DTO-style backend models follow the positional `sealed record` convention where applicable.
- `AuthServer` and `Orders` both use `Composition`.
- `Orders` has a minimal automated endpoint-level smoke/integration verification layer.
- The agreed backend tests pass, or any failures are explicitly documented.

## Sources

- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- `docs-ai/arch.md`
- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md`
- `README.md`
- `CLAUDE.md`
- `docs/DEVELOPMENT.md`
- `docs/DEPLOYMENT.md`
- `docs/TESTING.md`
- `docs/FRONTEND.md`
- `DOCKER.md`
- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/Controllers/AdminUsersController.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/Orders/Program.cs`
- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/backend/Shared/*`
- `tests/backend/Orders.UnitTests/Data/DatabaseSeederTests.cs`
