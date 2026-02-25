# Identity Vibe

.NET 8 identity + orders sample with a Next.js admin UI.

Architecture (development):

- `AuthServer` (OAuth2/OpenID Connect + user info): `http://localhost:5000`
- `Orders` API: `http://localhost:5003`
- `web-admin` (Next.js): `http://localhost:3000`

The frontend calls `AuthServer` and `Orders` directly. There is no proxy/BFF service in this repo.

## Quick Start (Docker Compose)

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

## Quick Start (Local Processes)

Backend:

```bash
cd src/backend/AuthServer && dotnet run
cd src/backend/Orders && dotnet run
```

Frontend:

```bash
cd src/web-admin && npm install && npm run dev
```

## Frontend Environment (dev)

`src/web-admin/.env.local`

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
```

## API Usage Notes

- Login uses OAuth2 Authorization Code + PKCE against `AuthServer`.
- User profile retrieval uses `GET /connect/userinfo`.
- Orders UI calls `Orders` endpoints directly under `/orders/*`.

## RBAC Model (Current)

- Users have roles (`Admin`, `User`, `Support`).
- `AuthServer` maps roles to permission scopes using `src/backend/AuthServer/appsettings.Scopes.json`.
- APIs authorize routes by scope-based policies (for example `orders.read`, `orders.write`, `orders.manage`).
- User PKCE tokens include role-derived permission scopes (project-specific behavior), while protocol scopes (`openid`, `profile`, `email`, `offline_access`, `roles`) remain request-driven.
- Refresh token grants recompute current role-derived permission scopes (role changes apply on refresh/re-login/expiry for existing sessions).

## Useful Endpoints

- Auth health: `GET http://localhost:5000/health`
- Auth userinfo: `GET http://localhost:5000/connect/userinfo`
- Orders health: `GET http://localhost:5003/health`
- Orders API: `http://localhost:5003/orders`

## Backend Integration Tests

AuthServer authentication flow integration tests (xUnit + FluentAssertions + WebApplicationFactory + PostgreSQL Testcontainers):

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj
```

Shared claims helper tests (scope parsing/admin helper):

```bash
dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj
```

## Docs

- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/DEPLOYMENT.md`
- `docs/FRONTEND.md`
- `docs/TESTING.md`
