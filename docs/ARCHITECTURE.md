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
  - `AuthServer` maps roles to permission scopes using `ScopeConfiguration:UserScopes:RoleScopes`.
- User PKCE token issuance behavior (project-specific):
  - Permission scopes are derived from the user's roles and included in the token (not limited by requested business scopes).
  - Protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`) remain request-driven.
- Refresh-token behavior:
  - `AuthServer` re-evaluates the user's current role-derived permission scopes.
  - Role downgrades remove permissions on refresh.
  - Role upgrades can add permissions on refresh.
- `Orders` route authorization is scope-based via policies (`orders.read`, `orders.write`, `orders.manage`) and should not rely on role fallback.
- Admin-like branching inside `Orders` should align to permission scopes (`orders.manage`) and may also accept `admin.full`.

## RBAC Extension Workflow

When adding a new permission:

1. Add a scope to `src/backend/AuthServer/appsettings.Scopes.json`
2. Map the scope to roles in `ScopeConfiguration:UserScopes:RoleScopes`
3. Add the scope to the relevant client(s) in `src/backend/AuthServer/appsettings.Clients.json` if needed
4. Add/update an API policy (for example in `src/backend/Orders/appsettings.json`)
5. Add/update integration/unit tests for issuance and route enforcement

## Guardrails

- Do not authorize OAuth-protected API routes directly by role claims when the permission model is scope-based.
- Do not assume the `scope` claim arrives in only one format (`scope` may be repeated claims or a space-separated value).
- With self-contained JWTs, RBAC changes affect existing access tokens only after refresh/re-login/expiry.

## Service Ports (dev)

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- `web-admin`: `http://localhost:3000`

## Key Files

- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/appsettings.Clients.json`
- `src/backend/AuthServer/appsettings.Scopes.json`
- `src/backend/Orders/Program.cs`
- `src/web-admin/lib/api.ts`
- `src/web-admin/lib/oauth2-auth.ts`
- `src/web-admin/lib/orders-api.ts`
