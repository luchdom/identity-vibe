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
- Roles map to permission scopes in `AuthServer` (`ScopeConfiguration:UserScopes:RoleScopes`)
- APIs (for example `Orders`) authorize routes by scopes/policies, not role fallback
- User PKCE tokens include role-derived permission scopes (project-specific behavior), while protocol scopes remain request-driven
- Refresh token grants recompute current role-derived permission scopes (role changes apply on refresh/re-login/expiry)
- For `AuthServer` auth changes, prefer real backend integration tests (`tests/backend/AuthServer.IntegrationTests`) over mocks for regression coverage.
- For scope/claims helper changes, add/update `tests/backend/Shared.UnitTests`.

## Manual Smoke

- Login through the web admin UI
- Load orders list from `Orders`
- Verify role-based permissions manifest as scopes in API behavior (user vs admin paths)
- Confirm auth + orders logs include correlation IDs
