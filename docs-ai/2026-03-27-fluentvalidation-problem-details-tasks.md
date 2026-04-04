# FluentValidation + ProblemDetails Tasks

## Scope Lock

- Use FluentValidation for API endpoint validation.
- Keep MVC/Razor forms such as `AuthServer` `AccountController` on the current `ModelState` flow.
- Use request validators only in this pass.
- Execute validators explicitly in API controllers.
- Keep route/auth/query guards as explicit controller/shared guard helpers in this pass.
- Preserve Ardalis `Result` as the service-layer contract.
- Preserve ProblemDetails as the API error format.
- Standardize validation error codes where practical, but do not block the migration on a perfect catalog.

## Task 1. Inventory and classify current validation paths

### Goal

Establish the exact current validation mechanisms so no duplicate or orphaned paths remain after migration.

### Work

1. Inventory all current validation paths:
   - custom validators in `src/backend/Orders/Validators/`
   - manual validation logic in `src/backend/Orders/Services/OrdersService.cs`
   - ad hoc null/empty/format checks in AuthServer admin API services/controllers
   - MVC `ModelState` validation in `src/backend/AuthServer/Controllers/AccountController.cs`
   - options/config validation using `IValidateOptions<T>`
2. Classify each as:
   - API request validation
   - service/use-case validation
   - MVC form validation
   - options/config validation
3. Confirm which validations will migrate now and which remain intentionally unchanged.
4. Explicitly tag non-body input validation as keep-on-guard-helper for this pass:
   - route ids
   - auth-derived values
   - query params

### Files to review

- `src/backend/Orders/Validators/*`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/AuthServer/Controllers/AdminUsersController.cs`
- `src/backend/AuthServer/Controllers/AdminRolesController.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/AuthServer/Services/RoleAdminService.cs`
- `src/backend/AuthServer/Controllers/AccountController.cs`
- `src/backend/Shared/Common/ResultExtensions.cs`

### Acceptance Criteria

- Every active validation path is classified as migrate-now or keep-as-is.
- The implementation pass does not unintentionally touch MVC form validation or options validators.

## Task 2. Add FluentValidation packages and DI registration

### Goal

Introduce FluentValidation as the standard validator mechanism for backend API requests.

### Work

1. Add FluentValidation package(s) to projects that host API request validators.
2. Add DI registration/scanning for validators.
3. Remove manual DI registration for old custom validator classes once FluentValidation registration replaces them.
4. Keep configuration validators on `IValidateOptions<T>`.
5. Do not enable automatic MVC/ASP.NET FluentValidation validation behavior in this pass; register validators only for explicit `IValidator<T>` resolution.
6. Treat DI registration for explicit `IValidator<T>` resolution as the intended integration mode for this pass.

### Files to touch

- `src/backend/Orders/Orders.csproj`
- `src/backend/AuthServer/AuthServer.csproj`
- `src/backend/Orders/Composition/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/backend/AuthServer/Program.cs` or a new AuthServer composition registration file if introduced

### Acceptance Criteria

- FluentValidation is available in the relevant backend API projects.
- Validators are resolved through DI without one-off manual validator registrations.
- Existing options/config validation remains intact.

## Task 3. Add shared FluentValidation -> Ardalis invalid result adapter

### Goal

Make FluentValidation failures flow through the repo’s existing Ardalis `Result` and ProblemDetails path instead of creating a parallel error mechanism.

### Work

1. Add a shared helper in `Shared.Common` that converts FluentValidation failures into:
   - `Result.Invalid(...)`
   - `Result<T>.Invalid(...)`
2. Preserve field/property names for `ProblemDetail.Errors`.
3. Preserve or derive validation error codes where practical.
4. Keep `ToActionResultWithProblemDetails(...)` as the final controller HTTP translation step.

### Files to touch

- `src/backend/Shared/Common/ResultExtensions.cs`
- optionally a new shared helper file under `src/backend/Shared/Common/`

### Acceptance Criteria

- FluentValidation output can be converted to Ardalis invalid results in one shared way.
- Validation failures render through existing ProblemDetails mapping with field-level errors.
- The shared solution does not require throwing FluentValidation exceptions to produce client errors.

## Task 4. Standardize explicit controller-side validation pattern

### Goal

Keep validators explicit and controllers thin, without relying on automatic validation pipeline behavior.

### Work

1. Define a small reusable controller-side validation pattern:
   - resolve validator
   - validate request
   - convert validation failure to Ardalis invalid result
   - return ProblemDetails
2. Define a small reusable non-body guard pattern for:
   - route ids
   - query params
   - auth-derived values
3. Decide whether the reusable piece is:
   - an extension/helper in `Shared`
   - a small local helper per service
4. Keep controller code readable and consistent across endpoints.

### Files to touch

- `src/backend/Shared/Common/*` if shared helper is chosen
- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/AuthServer/Controllers/AdminUsersController.cs`
- `src/backend/AuthServer/Controllers/AdminRolesController.cs`

### Acceptance Criteria

- API request validation is explicit in controllers.
- Route/auth/query guards remain explicit and consistent.
- Controllers stay thin and do not reintroduce repetitive ad hoc validation code.
- Validation failures return the same ProblemDetails shape as other Ardalis failures.

## Task 5. Migrate Orders request validators to FluentValidation

### Goal

Replace the current dead custom validator pattern in `Orders` with active FluentValidation request validators.

### Work

1. Replace custom validator classes in `src/backend/Orders/Validators/` with FluentValidation `AbstractValidator<T>` request validators.
2. Cover the active `Orders` API request types:
   - `CreateOrderRequest`
   - `UpdateOrderRequest`
   - `CancelOrderRequest`
   - `UpdateOrderStatusRequest`
   - `AddTrackingNumberRequest`
3. Include nested request validation where applicable:
   - customer request shape
   - item request shape
   - shipping request shape
4. Standardize practical error codes for validation rules.
5. Leave route/auth/query checks out of FluentValidation for this pass.

### Files to touch

- `src/backend/Orders/Validators/*`
- `src/backend/Orders/Models/Requests/*` only if minor validation-shape alignment is needed
- `src/backend/Orders/Composition/DependencyInjection/ServiceCollectionExtensions.cs`

### Acceptance Criteria

- `Orders` API request validation is implemented with FluentValidation.
- Nested request structures validate correctly.
- Error codes/messages are stable enough for API consumers and tests.

## Task 6. Remove redundant manual validation from Orders service layer

### Goal

Eliminate duplicate validation logic after request validators become the canonical API boundary validation path.

### Work

1. Remove or reduce private validation methods in `OrdersService` that duplicate request-validator rules.
2. Keep true business workflow checks that depend on domain state or service context.
3. Ensure service methods still return meaningful Ardalis failures for non-request-validation failures.

### Files to touch

- `src/backend/Orders/Services/OrdersService.cs`

### Acceptance Criteria

- Transport/input validation is no longer duplicated in `OrdersService`.
- Business/use-case checks that belong in the service layer remain in place.
- `OrdersService` still returns useful domain/application failures.

## Task 7. Wire explicit validation into Orders API controllers

### Goal

Make `OrdersController` execute FluentValidation request validators and return ProblemDetails on invalid requests.

### Work

1. Inject the request validators needed by `OrdersController`.
2. Validate each incoming request before mapping to commands/services.
3. Convert validation failures through the shared Ardalis invalid-result helper.
4. Return ProblemDetails via the existing `ToActionResultWithProblemDetails(...)` path.
5. Keep route id, query, and user-id guards on explicit controller/shared helper paths.

### Files to touch

- `src/backend/Orders/Controllers/OrdersController.cs`
- any shared/controller helper file chosen in Task 4

### Acceptance Criteria

- Invalid `Orders` API payloads return ProblemDetails consistently.
- Request-to-command mapping happens only after validation passes.
- Route/query/user guards still behave consistently and are not accidentally removed.
- Controller behavior is consistent across all relevant `Orders` endpoints.

## Task 8. Migrate AuthServer admin API validation to FluentValidation

### Goal

Bring AuthServer admin APIs onto the same request-validation and ProblemDetails path without changing MVC/Razor form behavior.

### Work

1. Add FluentValidation validators for admin API request models, such as:
   - role create/update/scope update requests
   - admin user create/update/roles/status/password requests
2. Move request-shape validation out of ad hoc service/controller checks where appropriate.
3. Keep service-layer/domain checks that depend on persistence/runtime state.
4. Preserve `AccountController` and related MVC flows on `ModelState`.
5. Keep route/auth/query guards on explicit controller/shared helper paths.

### Files to touch

- `src/backend/AuthServer/Models/AdminRoles/*`
- `src/backend/AuthServer/Models/AdminUsers/*`
- new validator files under a backend validator location
- `src/backend/AuthServer/Controllers/AdminUsersController.cs`
- `src/backend/AuthServer/Controllers/AdminRolesController.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/AuthServer/Services/RoleAdminService.cs`

### Acceptance Criteria

- AuthServer admin API request validation uses FluentValidation.
- MVC/Razor auth forms remain unchanged.
- Request-shape checks are no longer scattered between controllers and services.

## Task 9. Update docs for the new validation standard

### Goal

Make the validation policy explicit so future work follows one approach.

### Work

1. Update backend docs to state:
   - FluentValidation is the default validator mechanism for backend APIs
   - validators run explicitly in API controllers
   - request validators are the standard in this repo for this pass
   - validation failures map to Ardalis invalid results and then ProblemDetails
   - MVC/Razor forms remain on `ModelState` for now
2. Update testing docs with validator and invalid-payload expectations.
3. Add a short note if needed on practical validation error code usage.

### Files to touch

- `docs/BACKEND.md`
- `docs/TESTING.md`
- optionally `AGENTS.md` if the workflow/routing doc needs a short validation note

### Acceptance Criteria

- The repo docs clearly describe the API-vs-MVC validation split.
- The validator execution and ProblemDetails mapping path is documented.

## Task 10. Add and update tests

### Goal

Verify the validator migration at unit, integration, and architecture levels.

### Work

1. Add validator unit tests:
   - valid requests pass
   - invalid requests fail with expected field/message/error-code output
2. Add shared adapter tests for FluentValidation -> Ardalis invalid result conversion.
3. Add API integration coverage for invalid payloads returning ProblemDetails.
4. Extend architecture/guardrail coverage if needed so the repo keeps using the intended validator pattern.

### Files to touch

- `tests/backend/Orders.UnitTests/*`
- `tests/backend/Shared.UnitTests/*`
- `tests/backend/Orders.IntegrationTests/*`
- `tests/backend/AuthServer.IntegrationTests/*`
- `tests/backend/Architecture.UnitTests/*` if needed

### Acceptance Criteria

- Invalid `Orders` payloads are covered by automated ProblemDetails tests.
- At least one AuthServer admin invalid-request flow is covered.
- Shared adapter behavior is unit-tested.
- The test suite protects against regression in the validation pipeline.

## Task 11. Final verification and cleanup

### Goal

Finish the migration in a clean, documented, verifiable state.

### Work

1. Remove old dead custom validator code that is fully replaced.
2. Remove redundant validation helpers if superseded by the shared FluentValidation adapter.
3. Run the full backend build and tests.
4. Confirm docs and code match.

### Verification Bar

- `dotnet build ArchZ.sln`
- `dotnet test ArchZ.sln`
- Invalid request tests confirm ProblemDetails responses for:
  - `Orders`
  - at least one AuthServer admin API path
- MVC/Razor login/register flows are not broken by the API validation migration

### Acceptance Criteria

- No dead custom validator path remains active in migrated API surfaces.
- API validation uses FluentValidation and returns ProblemDetails consistently.
- MVC/Razor validation behavior remains intentionally unchanged.
- Docs and tests reflect the final behavior.

## Risks / Rollout Notes

- Highest risk: accidentally applying API-oriented FluentValidation behavior to MVC/Razor view flows.
  - Mitigation: keep `AccountController` and related form requests explicitly out of scope.
- Highest implementation seam: converting FluentValidation failures into the repo’s Ardalis invalid-result path without losing field names or error codes.
  - Mitigation: land the shared adapter early and test it before broad controller migration.
- Duplicate-validation risk: request validators and service methods enforcing the same input rules.
  - Mitigation: remove duplicated transport/input validation from services as part of the migration.
