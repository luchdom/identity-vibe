# AGENTS Notes (Project-Specific)

## Current Architecture

- Backend services: `AuthServer`, `Orders`, `Shared`
- Frontend: `src/web-admin`
- Frontend calls `AuthServer` and `Orders` directly (no proxy layer)

## Local Startup

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

## Working Rules

- Update frontend HTTP calls in `src/web-admin/lib/api.ts` (`authApi`, `ordersApi`) instead of introducing a new proxy layer.
- Keep user profile fetches on `/connect/userinfo`.
- When changing auth scopes/clients, update both:
- `src/backend/AuthServer/appsettings.Clients.json`
- `src/backend/AuthServer/appsettings.Scopes.json`
- RBAC model in this repo:
- Runtime role->permission assignments live in AuthServer DB (`AspNetRoleScopes`)
- `ScopeConfiguration:UserScopes:RoleScopes` is bootstrap/default seeding config, not runtime source of truth
- APIs (for example `Orders`) authorize routes by scopes/policies, not role fallback
- User PKCE tokens include role-derived permission scopes (project-specific behavior), while protocol scopes remain request-driven
- Refresh token grants recompute current role-derived permission scopes (role changes apply on refresh/re-login/expiry)
- `web-admin` roles/permissions UI uses scope-based guards (`admin.roles` or `admin.full`) and calls `AuthServer` admin APIs under `/api/admin/roles`
- For `AuthServer` auth changes, prefer real backend integration tests (`tests/backend/AuthServer.IntegrationTests`) over mocks for regression coverage.
- For scope/claims helper changes, add/update `tests/backend/Shared.UnitTests`.

## Modeling Conventions (.NET)

- Prefer `sealed record` for DTOs, commands, results, API response models, and read/projection models.
- Prefer `class` for EF/Identity entities, `DbContext`, controllers, services, repositories, middleware, and framework infrastructure.
- Prefer `init` for immutable DTO/config properties.
- Use `required` to make construction intent explicit, but do not treat it as runtime validation.
- For configuration models, pair `record/init/required` with runtime options validation (`IValidateOptions<T>` + `ValidateOnStart()`).
- Keep semantic collection defaults only when empty is a valid meaning (for example optional `Roles`/`Scopes` lists in policy config).

## Cleanup Expectations

- When a refactor makes code unused, remove the dead code in the same pass (including DI registrations/usings/tests/docs references).
- Treat repeated warnings like unused parameters and `async` without `await` as cleanup debt to fix while touching the file.
- Keep solution/project hygiene up to date (for example add new test projects to `ArchZ.sln`).

## Manual Smoke

- Login through the web admin UI
- Load orders list from `Orders`
- Verify role-based permissions manifest as scopes in API behavior (user vs admin paths)
- Confirm auth + orders logs include correlation IDs
