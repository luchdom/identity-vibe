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
- For `AuthServer` auth changes, prefer real backend integration tests (`tests/backend/AuthServer.IntegrationTests`) over mocks for regression coverage.

## Manual Smoke

- Login through the web admin UI
- Load orders list from `Orders`
- Confirm auth + orders logs include correlation IDs
