# FluentValidation + ProblemDetails Audit

## Findings

### 1. High: the task breakdown does not account for route and auth-derived inputs that request validators cannot see

The scope lock explicitly says request validators only and explicit controller execution, but the tasks never define what happens to non-body inputs such as:

- route ids in `OrdersController` like `id` on update/cancel/status/tracking endpoints
- route ids in AuthServer admin endpoints like `userId` and `roleId`
- auth-derived values like `actorUserId` and `HttpContext.GetUserId()`

This matters because current code still depends on those guards:

- [OrdersController](C:\dev\luchdom\identity-vibe\src\backend\Orders\Controllers\OrdersController.cs#L21) through [OrdersController](C:\dev\luchdom\identity-vibe\src\backend\Orders\Controllers\OrdersController.cs#L24)
- [OrdersController](C:\dev\luchdom\identity-vibe\src\backend\Orders\Controllers\OrdersController.cs#L94) through [OrdersController](C:\dev\luchdom\identity-vibe\src\backend\Orders\Controllers\OrdersController.cs#L105)
- [AdminUsersController](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Controllers\AdminUsersController.cs#L62) through [AdminUsersController](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Controllers\AdminUsersController.cs#L69)
- [AdminRolesController](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Controllers\AdminRolesController.cs#L46) through [AdminRolesController](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Controllers\AdminRolesController.cs#L57)

But the tasks only describe request-body validators:

- [2026-03-27-fluentvalidation-problem-details-tasks.md](C:\dev\luchdom\identity-vibe\docs-ai\2026-03-27-fluentvalidation-problem-details-tasks.md#L7)
- [2026-03-27-fluentvalidation-problem-details-tasks.md](C:\dev\luchdom\identity-vibe\docs-ai\2026-03-27-fluentvalidation-problem-details-tasks.md#L141)
- [2026-03-27-fluentvalidation-problem-details-tasks.md](C:\dev\luchdom\identity-vibe\docs-ai\2026-03-27-fluentvalidation-problem-details-tasks.md#L196)

Without an explicit task for route/auth guard strategy, implementers can easily delete the current checks and create regressions.

### 2. Medium: query endpoint validation is not covered, so the repo would end up with mixed validation policy

The tasks standardize API validation around request validators, but they only call out body-bound request types. They do not address query-driven endpoints such as:

- `OrdersController.GetOrders` and `GetAllOrders` pagination/status query handling in [OrdersController](C:\dev\luchdom\identity-vibe\src\backend\Orders\Controllers\OrdersController.cs#L54) through [OrdersController](C:\dev\luchdom\identity-vibe\src\backend\Orders\Controllers\OrdersController.cs#L75)
- `AdminUsersController.ListUsers` query params in [AdminUsersController](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Controllers\AdminUsersController.cs#L17) through [AdminUsersController](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Controllers\AdminUsersController.cs#L26)

Today `Orders` still normalizes pagination manually via `ValidationExtensions.ValidatePagination`, not through validators. The tasks need to decide whether query endpoints remain on small controller helpers or whether they get dedicated query request models plus FluentValidation. As written, the migration would leave a second ad hoc path in place.

### 3. Medium: Task 2 is underspecified on package/integration choice, which creates a real risk of accidentally enabling MVC auto-validation

The plan intentionally chose explicit controller execution and explicitly excluded MVC/Razor forms from this pass, but Task 2 only says “add FluentValidation package(s)” and “add DI registration/scanning”:

- [2026-03-27-fluentvalidation-problem-details-tasks.md](C:\dev\luchdom\identity-vibe\docs-ai\2026-03-27-fluentvalidation-problem-details-tasks.md#L58) through [2026-03-27-fluentvalidation-problem-details-tasks.md](C:\dev\luchdom\identity-vibe\docs-ai\2026-03-27-fluentvalidation-problem-details-tasks.md#L60)

That leaves room for an implementer to wire ASP.NET/MVC auto-validation packages or filters in a way that touches `AccountController`, which directly conflicts with the clarified scope. The task list should explicitly say:

- use DI registration only for explicit `IValidator<T>` resolution
- do not enable automatic MVC validation pipeline behavior in this pass

### 4. Medium: the AuthServer task does not clearly separate request-shape validation from service/domain/runtime checks

Task 8 says to “move request-shape validation out of ad hoc service/controller checks where appropriate,” but many current AuthServer checks are not request-shape concerns:

- duplicate email / duplicate role existence
- inactive/unknown roles
- self-lockout / self-role-removal protections
- identity manager operation failures

Those checks live in:

- [UserAdminService](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Services\UserAdminService.cs)
- [RoleAdminService](C:\dev\luchdom\identity-vibe\src\backend\AuthServer\Services\RoleAdminService.cs)

The task wording is broad enough that an implementer could over-migrate service-layer guards into validators even though they depend on persistence/runtime state and belong in services. The task should explicitly list which classes of checks stay service-side.

## Open Questions / Assumptions

- I assumed the intended end state is:
  - body-bound API input => FluentValidation request validators
  - route/auth/query guards => explicit controller/shared guard helpers unless separately promoted to dedicated request/query models
  - runtime/domain checks => service layer
- If you want query params fully folded into FluentValidation too, the task list should add dedicated query request models as a first-class change.

## Change Summary

The task breakdown is strong on the shared adapter, Orders-first rollout, docs, and testing. The main thing missing is a precise policy for inputs that are not request bodies, especially route ids, auth-derived values, and query params.
