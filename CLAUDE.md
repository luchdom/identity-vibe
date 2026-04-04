# Project Notes

Read `AGENTS.md` first for workflow rules and document routing.

## Runtime

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- Primary frontend: `src/web-app`
- Legacy frontend: `src/web-admin`

## Key Rules

- Frontend calls `AuthServer` and `Orders` directly
- Auth uses Authorization Code + PKCE
- User profile retrieval uses `/connect/userinfo`
- Orders authorization is scope-based
- Backend architecture rules live in `docs/BACKEND.md`

## Key Config Files

- `src/web-app/lib/api.ts`
- `src/backend/AuthServer/appsettings.Clients.json`
- `src/backend/AuthServer/appsettings.Scopes.json`
- `src/backend/Orders/appsettings*.json`
