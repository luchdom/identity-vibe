# Architecture

## Runtime Topology

The active runtime in this repo is:

- `AuthServer` for identity, OpenID Connect, admin auth APIs, and account UI
- `Orders` for orders APIs
- `src/web-app` as the primary frontend

Legacy compatibility surfaces still exist:

- Docker Compose still uses `client` as the frontend container/runtime name
- `src/web-admin` remains in the repo during migration

The browser calls `AuthServer` and `Orders` directly. There is no proxy/BFF layer.

## Request Flow

1. User opens `src/web-app`
2. Frontend starts OAuth2 Authorization Code + PKCE against `AuthServer`
3. Frontend exchanges the code for tokens on `AuthServer`
4. Frontend loads profile data from `GET /connect/userinfo`
5. Frontend calls `Orders` with bearer tokens

## Security Model

- `AuthServer` issues JWT access tokens
- `Orders` validates tokens using `AuthServer` as authority
- APIs authorize by scopes/policies, not role fallback
- Runtime role-to-scope assignments live in `AuthServer` DB (`AspNetRoleScopes`)
- `ScopeConfiguration:UserScopes:RoleScopes` is bootstrap/seeding config, not the runtime source of truth

Project-specific token behavior:

- PKCE user tokens include role-derived permission scopes
- protocol scopes remain request-driven
- refresh-token grants recompute current role-derived permission scopes

## Repo-Specific Architecture Notes

- `Shared` is the cross-service building-block layer
- `Configuration/` remains a top-level backend folder by design
- `AuthServer` keeps `Views/` and `wwwroot/` because it serves interactive auth UI

Backend implementation details, layer responsibilities, and naming rules live in `docs/BACKEND.md`.
