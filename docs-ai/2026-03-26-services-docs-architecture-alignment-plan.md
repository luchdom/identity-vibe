# Services And Docs Architecture Alignment Plan

## 1. Overview

### Problem statement

`docs-ai/arch.md` defines a backend architecture centered on service-layer applications organized by technical responsibility, with thin controllers, business logic in services, persistence behind repositories, and a predictable top-level folder layout. The current repository only partially matches that target:

- `AuthServer` already follows parts of the service-layer pattern, but still has service registration in `Program.cs`, mixed folder conventions, and legacy naming such as `ViewModels`.
- `Orders` is farther from the target layout: repositories live outside `Data/`, controllers still perform repeated HTTP/user extraction work, `ViewModels` are used as internal service/repository contracts, and the project lacks explicit `Validators`, `Common`, and `Composition` conventions.
- The repo still relies on a custom `Shared.Common.Result` / `Result<T>` stack and custom ASP.NET translation helpers, so the result contract is tightly coupled to current controller/service/repository patterns.
- The documentation set is split across conflicting sources. `README.md` and `CLAUDE.md` still refer to `web-admin`, while `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/FRONTEND.md`, and `docs/AGENTS.MD` describe `web-app` as the primary frontend. The repo also has both root-level notes and `docs/AGENTS.MD` acting as agent guidance.

Without an alignment pass, the architecture document will remain aspirational rather than operational, and future contributors will keep copying inconsistent patterns.

### Goals

- Align `AuthServer` and `Orders` toward the service-layer architecture described in `docs-ai/arch.md`.
- Replace the custom backend result abstraction with the Ardalis Result package and standardize HTTP/result translation around that choice.
- Define the allowed exceptions to that architecture so the team does not fight the framework or over-refactor.
- Establish explicit naming conventions for folders, models, services, repositories, validators, and mappings, and register them in the docs.
- Establish a default model-shape rule: where possible, DTO-style models should use concise positional `sealed record` syntax instead of verbose property boilerplate.
- Document the purpose of each layer and the mapping between current repo folders/types and target architecture responsibilities.
- Introduce a root `AGENTS.md` entrypoint that routes agents to the correct docs under `docs/` based on task type.
- Keep the current repo workflow rules in root `AGENTS.md`, making it both the agent entrypoint and the workflow source of truth.
- Create a dedicated `docs/BACKEND.md` for .NET/backend architecture, conventions, layer responsibilities, naming, and implementation guidance.
- Perform an explicit `docs/` cleanup to remove, merge, or archive deprecated and low-value documentation.
- Establish one coherent documentation story for architecture, service responsibilities, local development, deployment, testing, and agent workflow.
- Make the intended structure executable enough that future service changes naturally follow the same conventions.

### Non-goals

- Rebuilding the solution into full DDD, vertical slice, CQRS infrastructure, or domain-event patterns.
- Replacing OpenIddict, ASP.NET Identity, or the current frontend/backend deployment model.
- Renaming the `Shared` project immediately unless that rename is explicitly approved.
- Redesigning frontend UI behavior beyond documentation consistency.

## 2. Assumptions & Constraints

### Known constraints

- The repo currently contains three backend/runtime code areas: `AuthServer`, `Orders`, and `Shared`.
- `Shared` already acts as the practical equivalent of the “BuildingBlocks” concept from `docs-ai/arch.md`, even though the project name differs.
- `AuthServer` is not a pure API; it includes MVC/Razor assets (`Views`, `wwwroot`, account/login flow) that do not appear in the generic sample layout from `docs-ai/arch.md`.
- Existing auth and orders behavior is already covered unevenly by tests: `AuthServer` has integration coverage, `Shared` has unit coverage, and `Orders` currently has only seed-focused unit tests.
- The custom result stack is used broadly across `Shared`, `AuthServer`, `Orders`, and tests, so replacing it is a cross-cutting refactor rather than a package-only change.
- The repo already mandates a plan-first workflow in `docs/AGENTS.MD`.

### Explicit assumptions

- The architecture in `docs-ai/arch.md` is intended as the new source of truth for backend project shape, with pragmatic exceptions documented rather than ignored.
- Service reshaping should be incremental and behavior-preserving where possible.
- Documentation should reflect the current implementation target (`src/web-app`) and mark legacy surfaces (`src/web-admin`, Docker `client`) explicitly instead of mixing names casually.
- The first implementation pass should refactor `AuthServer` and `Orders` as equal architecture-alignment targets, even though `Orders` currently diverges more.
- `Configuration/` will remain a top-level backend folder and be documented as an approved exception to the generic sample layout.
- This phase should include lightweight architectural enforcement such as reviewable boundary checks and focused tests around naming/layer responsibilities.
- Backend `ViewModel` types should be fully renamed in this pass to the architecture vocabulary (`Request`, `Response`, `Command`, `Query`, `Result`).
- Ardalis `Result` is a service-layer contract in this repo; repositories should return persistence/domain values and let services translate expected failures into `Result`.
- `Orders` should gain a small automated integration/smoke verification layer for the core endpoints affected by this refactor.
- Where possible, `Request`, `Response`, `Command`, `Query`, and `Result` models should use positional `sealed record` syntax, for example `public sealed record Person(string FirstName, string LastName);`.

## 3. Architecture / Approach

### Proposed solution

Run the alignment in three coordinated tracks:

1. Discovery worktree and baseline inventory
   - Capture the exact current shape before moving files:
     - backend folder maps by service
     - current DI registrations and startup responsibilities
     - current result/error translation paths
     - current naming patterns and conflicts
     - doc/source-of-truth conflicts
   - Produce a current-to-target mapping matrix so implementation work follows an explicit baseline instead of inference.

2. Backend structure and result-strategy alignment
   - Normalize both backend services around a shared responsibility map:
     - `Controllers`: HTTP only
     - `Services`: business/application logic
     - `Validators`: request/command validation
     - `Models`: DTOs, commands, queries, results, enums, constants
     - `Data`: entities, repositories, mappings, configurations, db context, seeders, external persistence-facing clients
     - `Common`: app-local cross-cutting concerns that should not live in `Shared`
     - `Middleware`: pipeline behavior
     - `Composition`: DI and startup registration
   - Adopt the Ardalis Result package as the backend result contract and remove the custom `Shared.Common.Result` types after migration.
   - Treat `Shared` as the cross-service building-block layer already present in the repo.
   - Document approved exceptions:
     - `AuthServer/Views`, `wwwroot`, and account pages remain because AuthServer serves interactive auth UI.
     - `Configuration/` remains a top-level folder in backend services and is treated as a documented repo-specific exception.

3. Documentation consolidation
   - Make `docs/ARCHITECTURE.md` the repo-specific architecture source of truth for actual runtime topology.
   - Create a root `AGENTS.md` that acts as both:
     - the agent entrypoint/router
     - the canonical workflow source of truth for Plan -> Clarify -> Task it out -> Audit -> Implement
   - Merge the actionable rules from the current `docs/AGENTS.MD` into the new doc structure, keeping only the right level of guidance in each destination file.
   - Create `docs/BACKEND.md` as the backend/.NET implementation source of truth.
   - Update `README.md`, `CLAUDE.md`, `docs/DEVELOPMENT.md`, `docs/DEPLOYMENT.md`, and `docs/TESTING.md` so they all describe the same runtime names, active frontend target, backend layout, and service responsibilities.
   - Add a canonical naming-conventions section and a layer-responsibility/mapping section to the docs so future updates do not rely on tribal knowledge.
   - Add an explicit docs cleanup pass to remove, merge, redirect, or archive stale/deprecated content under `docs/`.

### Canonical mapping by service

### Canonical naming conventions to document

- `*Controller` for HTTP entrypoints
- `I*Service` / `*Service` for application/business services
- `I*Repository` / `*Repository` for persistence access
- `*Request` for transport input models
- `*Response` for transport output models
- `*Command` for service write inputs
- `*Query` for service read inputs
- `*Result` for service outputs
- `*Entity` where a name would otherwise be ambiguous with persistence models
- `*Validator` for request/command validation
- `*Mappings` or `*Mapper` only where mapping responsibility is clear and bounded

### Canonical model-shape conventions to document

- Prefer `public sealed record TypeName(...);` positional syntax for DTO-style models where possible
- Apply that default especially to:
  - `Requests`
  - `Responses`
  - `Commands`
  - `Queries`
  - `Results`
- Keep services, repositories, controllers, middleware, and entities as `class`
- Allow exceptions only where framework binding, serialization, or tooling makes positional records impractical; those exceptions should be documented rather than applied casually

These conventions should be written into the repo docs, with explicit notes on which legacy names are temporary and which are disallowed going forward.

#### Orders target shape

- Move `Repositories/` into `Data/Repositories/`.
- Convert repository interfaces to `Data/Repositories/Interfaces/`.
- Consolidate entity persistence and entity mapping under `Data/`.
- Reclassify `Models/ViewModels` into the architecture terms from `docs-ai/arch.md` such as `Requests`, `Responses`, `Commands`, `Queries`, or `Results`, or remove them where they are unnecessary.
- Introduce `Validators/` for request/command validation currently handled ad hoc in controllers/services.
- Add `Composition/DependencyInjection/ServiceCollectionExtensions.cs` and move DI wiring out of `Program.cs`.
- Introduce `Common/` only for Orders-local concerns not appropriate for `Shared`.

#### AuthServer target shape

- Keep current controllers/services/data split.
- Add `Composition/DependencyInjection/` to remove large registration blocks from `Program.cs`.
- Audit `Models/ViewModels` and migrate names toward the architecture terms from `docs-ai/arch.md`, especially `Requests`, `Responses`, `Commands`, `Queries`, and `Results`.
- Keep `Configuration/` top-level and document its role clearly.
- Document MVC/Razor folders as intentional service-specific extensions to the generic architecture.

#### Shared target shape

- Keep `Shared` as the existing cross-service building-block library.
- Remove the custom `Result` implementation from `Shared.Common` once the Ardalis migration is complete.
- Tighten docs around what belongs in `Shared` versus service-local `Common`.
- Avoid dumping service-specific helpers into `Shared` during the refactor.

### Data/control flow changes

The target request flow for both services should be:

HTTP Request
-> Controller receives `Request`
-> Controller maps to `Command` / `Query`
-> Validator checks transport and command invariants
-> Service enforces business rules and orchestration
-> Repository handles persistence
-> Service returns `Result`
-> Controller maps `Result` to `Response`
-> HTTP Response

### Key components and responsibilities

- `Program.cs`
  - stays minimal
  - config binding
  - middleware pipeline
  - calls extension methods from `Composition`
- `Composition`
  - DI wiring
  - service/repository registration
  - options validation registration
- `Controllers`
  - no `DbContext`
  - no repository access
  - minimal user/tenant/correlation extraction helpers only if not already centralized
- `Services`
  - business decisions
  - cross-repository orchestration
  - no HTTP concerns
- `Repositories`
  - EF Core access only
  - persistence queries and writes
- `Validators`
  - request/command shape validation
  - not a second business-logic layer

## 4. API / Contracts

### Endpoint impact

This alignment should be contract-preserving in the first implementation pass:

- No endpoint URL changes are required.
- No auth-flow changes are required.
- Existing RBAC policy behavior remains unchanged.

### Contract cleanup targets

- Standardize request/command/result/response naming so transport models are distinct from persistence and internal projections.
- Standardize service/controller contracts on Ardalis Result instead of the custom `Shared.Common.Result` types.
- Keep repository contracts out of the Ardalis `Result` abstraction unless a later explicit architecture decision changes that.
- Remove or rename ambiguous `ViewModel` types in backend projects where they are actually `Requests`, `Responses`, `Commands`, `Queries`, or `Results`.
- Ensure controller actions consistently translate Ardalis results into `ProblemDetails` responses using shared helpers/adapters rather than bespoke patterns where practical.

### Authn/authz expectations

- Continue to use `AuthServer` as the token issuer and authority.
- Continue scope-based authorization for API policies.
- Preserve existing `admin.roles`, `admin.users`, `admin.full`, `orders.read`, `orders.write`, and `orders.manage` semantics.
- Keep `/connect/userinfo` as the profile source for the frontend.

## 5. Data Model & Storage

### Schema impact

The architecture-alignment work should avoid schema changes unless a refactor exposes a real correctness bug. Folder moves, naming cleanup, DI extraction, validator introduction, and model separation should all be possible without changing database schema.

### Persistence refactor scope

- `Orders`
  - repository namespace and folder relocation
  - possible mapper relocation into `Data/Mappings`
  - possible internal model cleanup replacing `ViewModel` names with the architecture vocabulary
- `AuthServer`
  - likely naming and placement refactors only

### Backward compatibility

- Preserve EF Core entity/configuration namespaces carefully or update all references atomically.
- Keep migration history and `DbContext` names stable in the first pass.
- Do not couple documentation cleanup to a database migration unless unavoidable.

## 6. Implementation Steps

### Phase 0. Build the discovery worktree and current-state inventory

Likely files:

- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- a new discovery artifact such as `docs-ai/2026-03-26-services-docs-architecture-alignment-inventory.md`
- `docs/*`
- `src/backend/AuthServer/*`
- `src/backend/Orders/*`
- `src/backend/Shared/*`
- `README.md`
- `CLAUDE.md`
- `docs/*`

Actions:

- Capture the real current folder shape for `AuthServer`, `Orders`, and `Shared`.
- Build a current-to-target mapping matrix for:
  - folders
  - startup/DI responsibilities
  - result/error handling
  - controller -> service -> repository flow
- Build a naming inventory for:
  - current type suffixes/prefixes by layer
  - ambiguous names such as backend `ViewModel`
  - current mapping class placement
- Record all doc contradictions, especially:
  - `web-app` versus `web-admin`
  - root notes versus `/docs` notes
  - generic architecture guidance versus actual service-specific exceptions
- Audit the `docs/` folder for:
  - stale or deprecated files
  - duplicated guidance
  - docs that should merge into `BACKEND.md` or `ARCHITECTURE.md`
  - docs that should become redirects/pointers instead of full standalone sources
- Use that inventory as the required implementation baseline before any file moves.

Deliverable:

- A discovery worktree/workstream artifact that gives the implementing agent the exact current shape and the desired target mapping.
- A current-to-target naming inventory that shows what must be renamed, preserved temporarily, or documented as an exception.
- A docs cleanup inventory that classifies each significant doc as keep, merge, trim, redirect, archive, or delete.

### Phase 1. Decide and record the canonical architecture mapping

Likely files:

- `docs-ai/arch.md`
- `AGENTS.md`
- `docs/ARCHITECTURE.md`
- `docs/AGENTS.MD`
- `docs/BACKEND.md`

Actions:

- Translate the generic sample structure from `docs-ai/arch.md` into concrete repo rules for `AuthServer`, `Orders`, and `Shared`.
- Add a repo-level layer responsibility table that states, for each layer/folder:
  - purpose
  - allowed dependencies
  - forbidden responsibilities
  - example contents
- Add a repo-level naming convention table that states:
  - canonical suffix/prefix per type
  - where the type should live
  - whether it is transport, application, persistence, or cross-cutting
- Add a repo-level model-shape convention section that states:
  - when positional `sealed record` syntax is the default
  - which model categories should use it
  - which categories should remain `class`
  - what exceptions are allowed
- Add an “allowed exceptions” section covering:
  - AuthServer MVC/Razor assets
  - current `Shared` project name versus “BuildingBlocks”
  - temporary legacy frontend naming where still required by Docker or OAuth clients
- Establish the canonical doc split:
  - root `AGENTS.md` as entrypoint/router and canonical workflow rules document
  - `docs/BACKEND.md` for backend/.NET implementation rules
  - `docs/ARCHITECTURE.md` for system/runtime architecture and high-level structure
  - `docs/AGENTS.MD` merged, trimmed, redirected, or retired depending on the final cleanup shape

Deliverable:

- A repo-specific architecture contract that contributors can follow without guessing how the generic sample maps to this solution.
- A documented doc-information architecture so contributors know where each kind of guidance lives.

### Phase 2. Normalize documentation sources of truth

Likely files:

- `AGENTS.md`
- `README.md`
- `CLAUDE.md`
- `docs/AGENTS.MD`
- `docs/BACKEND.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/DEPLOYMENT.md`
- `docs/TESTING.md`
- `DOCKER.md`

Actions:

- Standardize service naming:
  - backend services: `AuthServer`, `Orders`, `Shared`
  - primary frontend: `src/web-app`
  - legacy frontend: `src/web-admin`
  - legacy Docker runtime name: `client`
- Remove contradictory wording about active frontend targets.
- Add a short backend folder-conventions section to the docs that contributors actually read.
- Add or update a naming-conventions section in contributor-facing docs so engineers can look up:
  - when to use `Request` vs `Command`
  - when to use `Response` vs `Result`
  - where `Entity`, `Validator`, `Mapper`, and `Configuration` types belong
- Add or update a model-shape conventions section so engineers can look up:
  - when to use positional `sealed record` syntax
  - when `class` is still expected
  - what counts as a justified exception
- Add or update a current-to-target mapping appendix so the docs show how existing folders map onto the target architecture.
- Move backend-specific guidance out of overloaded docs into `docs/BACKEND.md`.
- Merge the useful implementation/workflow rules from the current `docs/AGENTS.MD` into the new doc structure.
- Create root `AGENTS.md` with routing guidance such as:
  - backend/.NET work -> `docs/BACKEND.md`
  - frontend work -> `docs/FRONTEND.md`
  - system/runtime architecture -> `docs/ARCHITECTURE.md`
  - local run/development flows -> `docs/DEVELOPMENT.md`
  - deployment/runtime ops -> `docs/DEPLOYMENT.md`
  - testing expectations -> `docs/TESTING.md`
- Keep the current workflow rules in root `AGENTS.md`, including:
  - Plan -> Clarify -> Task it out -> Audit -> Implement
  - required planning format
  - approval gate before implementation
- Reduce `docs/AGENTS.MD` to either:
  - a trimmed agent-conventions file that complements the root router, or
  - a deprecation stub pointing to the new canonical docs
- Clean up deprecated or low-value docs:
  - merge duplicates
  - delete obsolete docs where safe
  - replace some files with short redirect notes where external references may still exist
- Cross-link the architecture doc from README and AGENTS notes.
- Make testing docs mention the architecture-driven expectations:
  - controller/service/repository separation
  - validator coverage
  - integration versus unit test responsibilities

Deliverable:

- A coherent doc set with no conflicting frontend/runtime naming and a clear pointer to the architecture rules.
- Contributor-facing docs that explicitly register naming conventions and the purpose/mapping of each backend layer.
- A root `AGENTS.md` router plus a cleaned `docs/` tree with clearer ownership and less deprecated noise.

### Phase 3. Replace the custom result stack with Ardalis Result

Likely files:

- `src/backend/Shared/Shared.csproj`
- `src/backend/Orders/Orders.csproj`
- `src/backend/AuthServer/AuthServer.csproj`
- `src/backend/Shared/Common/Result.cs`
- `src/backend/Shared/Common/ResultVoid.cs`
- `src/backend/Shared/Common/ResultExtensions.cs`
- `src/backend/Shared/Common/CommonErrors.cs`
- service interfaces and implementations in `src/backend/AuthServer/Services/*`
- service interfaces and implementations in `src/backend/Orders/Services/*`
- controller translation code in `src/backend/AuthServer/Controllers/*`
- controller translation code in `src/backend/Orders/Controllers/*`

Actions:

- Add the Ardalis Result package where needed and define the repo-standard usage pattern.
- Map current error semantics (`CommonErrors`, error codes, problem-details mapping) onto Ardalis statuses and metadata without changing public API behavior.
- Replace custom `Result` / `Result<T>` usage in service contracts.
- Refactor repository contracts away from the custom result types toward plain persistence/domain return values and let services perform result translation.
- Replace `ToActionResultWithProblemDetails` and similar helpers with Ardalis-aware HTTP translation helpers.
- Remove the custom result structs only after all usages have been migrated.
- Update docs and examples to teach the Ardalis pattern instead of the custom one.

Deliverable:

- A single result strategy across the backend, with Ardalis Result as the canonical service/controller contract.

### Phase 4. Extract composition roots from `Program.cs`

Likely files:

- `src/backend/Orders/Program.cs`
- `src/backend/AuthServer/Program.cs`
- new `src/backend/Orders/Composition/DependencyInjection/ServiceCollectionExtensions.cs`
- new `src/backend/AuthServer/Composition/DependencyInjection/ServiceCollectionExtensions.cs`

Actions:

- Move service, repository, options-validation, and infrastructure registrations out of `Program.cs`.
- Leave only startup orchestration in `Program.cs`.
- Add tests or smoke verification to ensure DI graph remains intact.

Deliverable:

- Minimal `Program.cs` files and a predictable `Composition` entry point in both services.

### Phase 5. Reshape Orders to match the target structure

Likely files/modules:

- `src/backend/Orders/Repositories/*`
- `src/backend/Orders/Services/*`
- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/Orders/Models/*`
- `src/backend/Orders/Data/*`
- new `src/backend/Orders/Validators/*`
- new `src/backend/Orders/Common/*` if needed

Actions:

- Move repositories under `Data/Repositories`.
- Move repository interfaces accordingly.
- Audit `Models/ViewModels`:
  - reclassify as `Requests`, `Responses`, `Commands`, `Queries`, or `Results`
  - remove names that imply MVC presentation models inside the backend
- Extract repeated controller logic where practical:
  - user-id requirement handling
  - pagination validation
  - admin/non-admin query shaping
- Introduce validators for:
  - create order
  - update order
  - status update
  - tracking/cancel requests where validation is currently implicit
- Re-check service boundaries so:
  - controllers stop owning request sanity checks beyond mapping
  - services stop returning transport-shaped objects where internal results would be clearer
- Relocate/rename mapping classes so persistence mapping lives under `Data`, request/response mapping lives with models or `Common/Mapping`.

Deliverable:

- `Orders` visibly matches the technical-responsibility architecture and becomes the reference backend service for future work.

### Phase 6. Align AuthServer naming and startup structure

Likely files/modules:

- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/Services/*`
- `src/backend/AuthServer/Models/*`
- `src/backend/AuthServer/Data/*`
- new `src/backend/AuthServer/Composition/*`

Actions:

- Extract DI/options registration into `Composition`.
- Audit `Models/ViewModels` and rename or relocate them where they are really `Requests`, `Responses`, `Commands`, `Queries`, or `Results`.
- Review whether any current top-level folders should move or be explicitly documented as exceptions:
  - `Configuration`
  - `Connectors`
  - `Filters`
  - `Infrastructure`
- Preserve login/account MVC behavior while documenting why it differs from a pure API layout.

Deliverable:

- `AuthServer` remains behaviorally stable while conforming to the same architecture vocabulary as `Orders`.
- `AuthServer` receives the same first-pass architecture attention as `Orders`, not only compatibility cleanup.

### Phase 7. Clarify `Shared` versus service-local `Common`

Likely files/modules:

- `src/backend/Shared/*`
- `docs/ARCHITECTURE.md`
- `docs/AGENTS.MD`

Actions:

- Document what belongs in `Shared`:
  - reusable auth helpers
  - result/error primitives
  - logging/correlation/telemetry
  - generic policy/config helpers
- Document what must remain service-local:
  - service-specific mappers
  - service-specific options
  - resource/business logic
- Audit any service-local concerns currently living in `Shared` and queue follow-up cleanup only if clearly misplaced.

Deliverable:

- Contributors can decide where new cross-cutting code belongs without creating another catch-all utilities layer.

### Phase 8. Add architecture regression checks

Likely files:

- `tests/backend/Orders.UnitTests/*`
- `tests/backend/AuthServer.IntegrationTests/*`
- possibly new focused unit tests around services/validators
- `ArchZ.sln` if new test projects are added

Actions:

- Expand `Orders` tests beyond seeding:
  - service rule tests
  - validator tests
  - controller/problem-details behavior if worth adding
- Keep `AuthServer` integration coverage focused on runtime behavior, not folder layout.
- Add lightweight architectural review criteria and focused enforcement in the first pass, for example:
  - simple tests or checks around layer boundaries
  - reviewable naming/layer rules
  - focused checks around positional-record conventions for DTO-style models where practical
  - guardrails that catch the most likely regressions without introducing heavy architecture tooling

Deliverable:

- The alignment survives future feature work because the expected structure is documented and partially tested.
- A lightweight enforcement layer exists so naming and responsibility rules are not purely advisory.

### Phase 9. Execute docs cleanup and deprecation removal

Likely files:

- `AGENTS.md`
- `docs/AGENTS.MD`
- `docs/BACKEND.md`
- `docs/ARCHITECTURE.md`
- other `docs/*.md` files identified by the inventory

Actions:

- Apply the cleanup inventory decisions:
  - keep
  - merge
  - trim
  - redirect
  - archive
  - delete
- Ensure no important backend guidance is stranded in deprecated docs after the merge.
- Where a file is retained only for compatibility, replace it with a short pointer to the new canonical destination.
- Re-check root-level docs (`README.md`, `CLAUDE.md`, `AGENTS.md`) so they point to the cleaned `docs/` structure.

Deliverable:

- A smaller, more intentional docs tree where canonical guidance is easy to find and deprecated content is either removed or clearly marked.

## 7. Testing Strategy

### Unit tests

- Add or expand `Orders.UnitTests` for:
  - service business rules
  - validator behavior
  - mapper behavior if the mapping layer is substantially reorganized
- Add targeted tests for any extracted helper/translation logic that centralizes controller behavior.

### Integration tests

- Keep `AuthServer.IntegrationTests` as the primary coverage for auth runtime behavior.
- Add a small `Orders` integration/smoke verification layer for the core endpoints touched by the refactor.

### Edge cases to validate

- Orders create/update/cancel/status flows still return the same HTTP semantics after controller/service cleanup.
- Problem-details formatting remains consistent.
- Ardalis result status mapping preserves the current API error/status behavior.
- Scope-based authorization behavior remains unchanged.
- Seeder startup still succeeds after folder/namespace refactors.
- `Program.cs` composition extraction does not break option validation or database startup.
- Core `Orders` endpoints remain functional under automated smoke/integration coverage after repository/service/controller restructuring.

### Local validation

- `dotnet build ArchZ.sln`
- `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj`
- `dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj`
- `dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj` for any AuthServer-impacting pass
- Manual health/auth/orders smoke for both services after DI/folder moves

## 8. Observability / Debuggability

- Preserve current correlation-id and OpenTelemetry wiring during `Program.cs` extraction.
- Add logs around startup registration failures only if the DI extraction makes debugging harder.
- Keep health endpoints and startup seed logs working.
- Document where logging/telemetry middleware belongs in the target layout so future service additions wire them consistently.

## 9. Rollout Plan

### Recommended rollout order

1. Land the discovery worktree/inventory first.
2. Land the doc-information architecture:
   root `AGENTS.md`, `docs/BACKEND.md`, cleaned `docs/ARCHITECTURE.md`, and the cleanup inventory.
3. Introduce Ardalis Result and migrate shared HTTP/result translation.
4. Extract composition roots from both services.
5. Refactor `Orders` structure and naming.
6. Refactor `AuthServer` naming/startup structure.
7. Expand tests.
8. Execute final docs cleanup/deprecation removal and update root pointers if any drift remains.

### Backward compatibility

- Keep endpoint contracts, ports, auth flows, and client configuration stable during the alignment.
- Avoid a “big bang” rename of every namespace if that would inflate merge risk; use coherent slices.

### Monitoring after release

- Smoke health endpoints.
- Verify login, `/connect/userinfo`, orders list, order mutation flows, and admin users/roles screens.
- Watch startup logs for options validation, migrations, and seed failures after composition extraction.

## 10. Risks & Mitigations

### Risk: architecture drift continues because docs stay generic

Mitigation:

- Translate `docs-ai/arch.md` into repo-specific rules in `docs/ARCHITECTURE.md` and `docs/AGENTS.MD`.
- Include naming tables and layer-purpose mappings, not just prose guidance.

### Risk: folder moves create noisy diffs and broken namespaces

Mitigation:

- Refactor in slices:
  - result-strategy migration first
  - composition second
  - Orders repository relocation third
  - naming cleanup fourth
- Build and test after each slice.

### Risk: Ardalis migration changes API error semantics unintentionally

Mitigation:

- Keep the current error-code catalog and problem-details payload shape during the first pass.
- Introduce a repo-standard Ardalis-to-HTTP adapter before removing the custom result helpers.

### Risk: over-aligning AuthServer breaks interactive auth UI

Mitigation:

- Treat MVC/Razor folders as explicit exceptions instead of forcing them into a pure-API mold.

### Risk: `Shared` becomes even less disciplined during refactor

Mitigation:

- Document explicit placement rules before moving helpers around.

### Risk: docs still contradict because root and `/docs` notes diverge

Mitigation:

- Choose and document canonical ownership:
  - `README.md` for onboarding
  - `docs/ARCHITECTURE.md` for runtime and code-structure rules
  - root `AGENTS.md` for agent routing/entrypoint and workflow rules
  - `docs/BACKEND.md` for backend implementation rules
  - `CLAUDE.md` only if still required by tooling, kept intentionally in sync
- Require naming conventions and layer mappings to live in one canonical architecture doc, with other docs linking back instead of restating them differently.

### Risk: docs cleanup deletes information that is still referenced or useful

Mitigation:

- Do the cleanup from an explicit inventory and classification pass.
- Prefer short redirect stubs before deletion when references may still exist.
- Move useful content into canonical docs before removing the original file.

## 11. Open Questions

No blocking open questions remain from the current clarification round.

## Sources Consulted

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
