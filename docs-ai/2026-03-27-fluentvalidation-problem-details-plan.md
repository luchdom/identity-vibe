# FluentValidation + ProblemDetails Plan

## 1. Overview

Introduce FluentValidation as the standard validator mechanism for backend request/command validation and ensure validation failures are returned as structured ProblemDetails responses through the existing Ardalis Result pipeline.

This plan is based on the current repo state:

- `docs/BACKEND.md` already establishes `Validators/` as the validation layer and `Shared.Common` as the shared HTTP/problem-details translation layer.
- `Orders` currently has custom validator classes under `src/backend/Orders/Validators/`, but they are only registered in DI and are not actually consumed.
- `OrdersService` still contains private/manual validation logic in `src/backend/Orders/Services/OrdersService.cs`.
- API controllers already translate `Result` / `Result<T>` into ProblemDetails via `src/backend/Shared/Common/ResultExtensions.cs`.
- `AuthServer` still has MVC form flows in `src/backend/AuthServer/Controllers/AccountController.cs` that use `ModelState` and view-return behavior, which is a different validation/output model than API controllers.

## 2. Assumptions & Constraints

- Use FluentValidation for backend validators instead of custom `Validate(...)` classes.
- Preserve Ardalis `Result` as the service-layer contract.
- Preserve ProblemDetails as the API error format for backend API endpoints.
- Do not introduce repository-level validation results.
- Avoid creating two competing API validation paths.
- Treat MVC form validation in `AccountController` as an intentional exception for this pass; keep MVC/Razor forms on `ModelState` and focus FluentValidation + ProblemDetails on API endpoints.
- Keep route ids, query params, and auth-derived values on explicit controller/shared guard helpers in this pass.
- Prefer incremental migration: first standardize API validation and shared mapping, then decide whether Razor/MVC forms should adopt FluentValidation in the same pass.

## 3. Architecture / Approach

### 3.1 Target validation flow

Preferred flow for API endpoints:

`HTTP Request -> Request/Command Validator (FluentValidation) -> Service returns Result.Invalid -> Controller uses ToActionResultWithProblemDetails -> HTTP 400 ProblemDetails`

### 3.2 Validation strategy

- Replace custom validator classes with `AbstractValidator<T>`.
- Register validators through FluentValidation DI registration instead of manual `AddScoped<SomeValidator>()` calls.
- Keep validation execution aligned with current architecture:
  - request validators as the default and canonical API validation layer for this pass
  - do not introduce command validators by default in this pass
- Convert FluentValidation failures into Ardalis `Result.Invalid` using a shared adapter in `Shared.Common` or a neighboring shared validation helper.

### 3.3 ProblemDetails strategy

- Reuse `src/backend/Shared/Common/ResultExtensions.cs` as the single translation point.
- Extend the shared mapping so FluentValidation failures become:
  - Ardalis `ValidationError` entries
  - `ProblemDetail.Errors` field dictionaries
  - stable error codes/messages where possible
- Avoid throwing raw FluentValidation exceptions directly to the client if the repo is standardizing on `Result.Invalid`.

### 3.4 Execution strategy

- Execute request validators explicitly in API controllers before mapping requests to commands/services.
- Do not rely on implicit/automatic FluentValidation MVC pipeline behavior for this pass.
- Keep controller logic thin by standardizing a small reusable controller/helper pattern for:
  - resolve validator
  - validate request
  - convert failures to Ardalis invalid result
  - return ProblemDetails
- Keep non-body input handling on explicit guard/helper paths:
  - route ids
  - query params
  - auth-derived values

### 3.5 Service integration strategy

- Remove dead validation code paths:
  - custom `Validate(...)` validators in `Orders`
  - private manual validation methods in services that duplicate validator behavior
- Avoid duplicating request-validator rules as command validators in this pass.
- Services should assume controller-level request validation has already run for transport/input concerns.
- If request-level auto-validation is adopted for APIs, keep command-level validation only for business/use-case invariants that can exist independently of HTTP transport.

## 4. API / Contracts

### 4.1 External API behavior

For API endpoints, validation failures should return ProblemDetails consistently:

- HTTP status: `400`
- `type`, `title`, `status`, `detail`, `instance`, `traceId`
- `errors` dictionary keyed by field/property name when applicable
- stable validation error code(s) where practical

### 4.2 Internal contracts

- FluentValidation validators become the canonical validator contract.
- Services continue returning `Result` / `Result<T>`.
- Shared validation adapter converts `FluentValidation.Results.ValidationResult` to Ardalis invalid results.
- Controllers continue using `ToActionResultWithProblemDetails(...)`.

### 4.3 Scope candidates

Primary candidates in the first pass:

- `Orders` command/request validators in `src/backend/Orders/Validators/`
- admin/API request models in `AuthServer` where API controllers already return ProblemDetails

Special handling / current decision:

- `src/backend/AuthServer/Controllers/AccountController.cs`
- Razor/MVC request models that currently depend on `ModelState` and view re-rendering remain on the existing MVC validation path for this pass

## 5. Data Model & Storage

- No database schema changes are required.
- No persistence contract changes are required.
- The main data-shape change is error payload structure consistency for validation failures.
- Field-name mapping should be reviewed so property names in FluentValidation line up with API contract expectations.

## 6. Implementation Steps

### Step 1. Inventory current validation entry points

- Identify every active validation path across `Orders`, `AuthServer`, and `Shared`:
  - custom validator classes
  - manual service validation methods
  - `ModelState` / DataAnnotations paths
  - any existing config validators that should remain `IValidateOptions<T>`
  - route/query/auth guard helpers that intentionally remain outside FluentValidation in this pass
- Classify each as:
  - API request validation
  - command/service validation
  - MVC form validation
  - options/config validation

### Step 2. Add FluentValidation packages and registration shape

- Add FluentValidation package(s) for backend projects that need request/command validators.
- Add centralized validator scanning/registration in composition/DI.
- Remove one-off manual validator registrations where FluentValidation scanning replaces them.
- Keep FluentValidation registration explicit and do not enable automatic MVC validation pipeline behavior in this pass.
- Treat DI registration for explicit `IValidator<T>` resolution as the intended integration mode for this pass.

Likely touchpoints:

- `src/backend/Orders/Orders.csproj`
- `src/backend/AuthServer/AuthServer.csproj`
- `src/backend/Orders/Composition/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/backend/AuthServer/Program.cs` or a new composition registration point if introduced

### Step 3. Build shared FluentValidation -> Ardalis Result adapter

- Add a shared adapter in `Shared.Common` or an adjacent shared validation helper that converts FluentValidation results into:
  - `Result.Invalid(...)`
  - `Result<T>.Invalid(...)`
- Ensure field names and messages survive the translation into `ProblemDetail.Errors`.
- Keep existing `ToActionResultWithProblemDetails(...)` as the final HTTP mapper.

Likely touchpoints:

- `src/backend/Shared/Common/ResultExtensions.cs`
- possibly a new `src/backend/Shared/Common/FluentValidation*` helper file

### Step 4. Migrate `Orders` validators first

- Replace the custom validator classes under `src/backend/Orders/Validators/` with FluentValidation `AbstractValidator<T>` implementations.
- Remove private/manual validation logic from `OrdersService` that is now covered by request validators.
- Execute validation explicitly in controllers against request models as the default pattern for this pass.
- Keep route/query/user guards explicit in controller/shared helper code rather than moving them into FluentValidation.
- Ensure invalid `CreateOrder`, `UpdateOrder`, `CancelOrder`, `UpdateOrderStatus`, and `AddTrackingNumber` flows return ProblemDetails consistently.

Likely touchpoints:

- `src/backend/Orders/Validators/*`
- `src/backend/Orders/Services/OrdersService.cs`
- possibly `src/backend/Orders/Controllers/OrdersController.cs` if request-level validation flow is added

### Step 5. Migrate AuthServer API/admin validation

- Add FluentValidation validators for admin/API request models that currently rely on ad hoc checks in services/controllers.
- Convert those failures into the same Ardalis invalid -> ProblemDetails path.
- Execute those validators explicitly in API controllers before service calls.
- Keep route ids and auth-derived guards explicit in controllers/shared helpers rather than moving them into FluentValidation.
- Keep option/config validation on `IValidateOptions<T>` where it already fits.

Likely touchpoints:

- `src/backend/AuthServer/Controllers/AdminUsersController.cs`
- `src/backend/AuthServer/Controllers/AdminRolesController.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/AuthServer/Services/RoleAdminService.cs`
- new or updated validators under a backend validator location

### Step 6. Preserve MVC form policy explicitly

For this pass:

- API endpoints => FluentValidation + ProblemDetails
- Razor/MVC forms => existing DataAnnotations / `ModelState` flow

Document this split explicitly so API validation standardization does not unintentionally change server-rendered form UX.

### Step 7. Update docs

- Update `docs/BACKEND.md` to declare FluentValidation as the default backend validator mechanism.
- Document how FluentValidation failures map to Ardalis invalid results and then to ProblemDetails.
- Document the API-vs-MVC validation policy if they intentionally differ.
- Update testing docs with validator and invalid-payload expectations.

Likely touchpoints:

- `docs/BACKEND.md`
- `docs/TESTING.md`
- potentially `docs/ARCHITECTURE.md` only if error-flow documentation needs a short runtime note

## 7. Testing Strategy

### 7.1 Unit tests

- Validator tests for each FluentValidation validator:
  - valid payload/command passes
  - invalid payload/command returns expected field/message failures
- Shared adapter tests for FluentValidation -> Ardalis invalid result conversion
- ProblemDetails mapping tests for validation results

Likely touchpoints:

- `tests/backend/Shared.UnitTests`
- `tests/backend/Orders.UnitTests`
- a new `AuthServer` unit/architecture test area if needed

### 7.2 Integration tests

- Extend API integration/smoke tests so invalid payloads return:
  - `400`
  - ProblemDetails body
  - field-level `errors`
- Cover at least:
  - `Orders` invalid create/update flows
  - one AuthServer admin invalid request flow

Likely touchpoints:

- `tests/backend/Orders.IntegrationTests`
- `tests/backend/AuthServer.IntegrationTests`

### 7.3 Architecture/guardrail tests

- Add a lightweight check that validator types follow FluentValidation base classes where the repo expects them.
- Optionally add a guardrail that API validation failures remain ProblemDetails-based instead of ad hoc 400 payloads.

## 8. Observability / Debuggability

- Validation failures should be treated as expected client errors, not exception-noise paths.
- Preserve `traceId` in ProblemDetails through current shared infrastructure.
- Keep error payloads deterministic enough for frontend debugging and integration testing.
- If logging is added around validation, keep it low-noise and avoid full payload dumps of sensitive request data.

## 9. Rollout Plan

1. Land shared FluentValidation/result adapter.
2. Migrate `Orders` validators and invalid-response tests.
3. Migrate AuthServer admin/API validators and invalid-response tests.
4. Update docs once the runtime path is settled.
5. Decide whether MVC form validation joins this pass or is documented as intentionally separate.

## 10. Risks & Mitigations

- Risk: duplicate validation paths remain active.
  - Mitigation: explicitly remove dead custom validators and private service validation methods as part of migration.

- Risk: MVC forms break if API-oriented auto-validation is applied globally.
  - Mitigation: scope FluentValidation behavior carefully and document an API-vs-MVC split if needed.

- Risk: validation failures lose field-level detail in ProblemDetails.
  - Mitigation: add shared adapter tests for property-name/message preservation.

- Risk: controllers/services begin mixing exception-based and result-based validation flows.
  - Mitigation: standardize on `Result.Invalid` for API validation failures.

- Risk: config/options validation gets unintentionally rewritten even though `IValidateOptions<T>` is still appropriate.
  - Mitigation: keep `Configuration/` validators as option validators unless there is a strong reason to unify them differently.

## 11. Open Questions

1. Should this FluentValidation standard apply only to API endpoints, or also to MVC/Razor form flows like `AccountController`?
   - Decided: API endpoints only for this pass; MVC/Razor forms stay on `ModelState`.
2. Do you want request validators, command validators, or both as the default pattern for backend APIs?
   - Decided: request validators only for this pass.
3. Should validation execute automatically at the HTTP boundary for APIs, or explicitly inside services with `IValidator<T>` injection?
   - Decided: execute validators explicitly in API controllers.
4. Do you want validation error codes standardized beyond field/message mapping, or is field/message + ProblemDetails enough for this pass?
   - Decided: standardize where practical without blocking the migration on a perfect taxonomy.
5. How should route/auth/query inputs be handled?
   - Decided: keep route/auth/query guards as explicit controller/shared helper logic in this pass.
6. How should FluentValidation be integrated?
   - Decided: DI-only for explicit `IValidator<T>` use; do not enable MVC automatic validation behavior in this pass.
