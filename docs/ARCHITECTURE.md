# Architecture

## Overview

The system is composed of three runtime components in development:

- `web-admin` (Next.js UI)
- `AuthServer` (.NET 8, OpenIddict)
- `Orders` (.NET 8 API)

The browser calls `AuthServer` and `Orders` directly. There is no separate reverse-proxy service.

## Request Flow

1. User opens `web-admin` (`http://localhost:3000`)
2. Frontend initiates OAuth2 Authorization Code + PKCE against `AuthServer`
3. Frontend exchanges code for tokens on `AuthServer`
4. Frontend loads user profile from `GET /connect/userinfo`
5. Frontend calls `Orders` endpoints directly with bearer tokens

## Security Model

- `AuthServer` issues JWT access tokens.
- `Orders` validates tokens using `AuthServer` as authority.
- RBAC model:
  - Roles are identity/grouping metadata on the user (`Admin`, `User`, `Support`).
  - Scopes are API permissions (for example `orders.read`, `orders.write`, `orders.manage`).
  - Runtime role -> scope assignments are stored in `AuthServer` DB (`AspNetRoleScopes`).
  - `ScopeConfiguration:UserScopes:RoleScopes` is used for bootstrap defaults/seeding and not as the runtime source of truth.
- User PKCE token issuance behavior (project-specific):
  - Permission scopes are derived from the user's roles and included in the token (not limited by requested business scopes).
  - Protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`) remain request-driven.
- Refresh-token behavior:
  - `AuthServer` re-evaluates the user's current role-derived permission scopes.
  - Role downgrades remove permissions on refresh.
  - Role upgrades can add permissions on refresh.
- `Orders` route authorization is scope-based via policies (`orders.read`, `orders.write`, `orders.manage`) and should not rely on role fallback.
- Admin-like branching inside `Orders` should align to permission scopes (`orders.manage`) and may also accept `admin.full`.
- AuthServer admin management APIs are split by scope:
  - roles/permissions APIs: `admin.roles` or `admin.full`
  - user-management APIs: `admin.users` or `admin.full`
- User-management guardrails:
  - self-disable is blocked
  - self-removal of admin-management access (`admin.users`/`admin.full`) is blocked
  - inactive roles cannot be assigned through admin user endpoints

## RBAC Extension Workflow

When adding a new permission:

1. Add a scope to `src/backend/AuthServer/appsettings.Scopes.json`
2. (Optional bootstrap) map the scope in `ScopeConfiguration:UserScopes:RoleScopes` for default seeding/new environments
3. Assign the scope to roles in the Roles & Permissions UI (`/users/roles`) or via `AuthServer` admin APIs
4. Add the scope to the relevant client(s) in `src/backend/AuthServer/appsettings.Clients.json` if needed
5. Add/update an API policy (for example in `src/backend/Orders/appsettings.json`)
6. Add/update integration/unit tests for issuance and route enforcement

## Guardrails

- Do not authorize OAuth-protected API routes directly by role claims when the permission model is scope-based.
- Do not assume the `scope` claim arrives in only one format (`scope` may be repeated claims or a space-separated value).
- With self-contained JWTs, RBAC changes affect existing access tokens only after refresh/re-login/expiry.

## Modeling Conventions

- Domain/infrastructure runtime components (`controllers`, `services`, `repositories`, `middleware`, `DbContext`, EF entities) stay as `class`.
- Boundary/data-carrying models (DTOs, commands, results, response/read models) should prefer immutable `sealed record` types.
- Configuration models may use `record` + `init` + `required`, but must be backed by runtime options validation (`IValidateOptions<T>`) and startup fail-fast (`ValidateOnStart()`).

## Refactor Cleanup Expectations

- Major refactors should include a dead-code sweep (unused services/repos/helpers/DI registrations).
- Solution structure should remain current (include active test projects; remove obsolete projects).

## Service Ports (dev)

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- `web-admin`: `http://localhost:3000`

## Key Files

- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/appsettings.Clients.json`
- `src/backend/AuthServer/appsettings.Scopes.json`
- `src/backend/AuthServer/Controllers/AdminRolesController.cs`
- `src/backend/AuthServer/Controllers/AdminUsersController.cs`
- `src/backend/AuthServer/Data/Entities/AppRoleScope.cs`
- `src/backend/Orders/Program.cs`
- `src/web-admin/lib/api.ts`
- `src/web-admin/lib/admin-roles-api.ts`
- `src/web-admin/lib/admin-users-api.ts`
- `src/web-admin/lib/oauth2-auth.ts`
- `src/web-admin/lib/orders-api.ts`
