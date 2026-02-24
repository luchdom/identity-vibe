# Development Guide

## Services

- `AuthServer` -> `http://localhost:5000`
- `Orders` -> `http://localhost:5003`
- `web-admin` -> `http://localhost:3000`

## Docker (recommended)

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

## Local Process Startup

```bash
cd src/backend/AuthServer && dotnet run
cd src/backend/Orders && dotnet run
cd src/web-admin && npm install && npm run dev
```

## Frontend Env (dev)

`src/web-admin/.env.local`

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
NEXT_PUBLIC_APP_BASE_URL=http://localhost:3000
```

## Auth + Orders Smoke Test

1. Open `http://localhost:3000`
2. Authenticate via OAuth2 redirect to `AuthServer`
3. Verify user profile loads via `/connect/userinfo`
4. Verify orders list loads from `Orders`

## Backend Integration Tests

AuthServer auth-flow integration tests require Docker (Testcontainers).

Run all AuthServer integration tests:

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj
```

Run only auth flow tests:

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj --filter "FullyQualifiedName~AuthenticationFlowTests"
```

See `docs/TESTING.md` for patterns and best practices used in this repo.

## Troubleshooting

1. CORS errors:
- Check `Cors:AllowedOrigins` in `AuthServer` and `Orders` appsettings.

2. Token validation errors in `Orders`:
- Check `Authentication:Authority` points to the active `AuthServer` URL.

3. Stale frontend endpoints:
- Verify `NEXT_PUBLIC_AUTH_URL` and `NEXT_PUBLIC_ORDERS_URL`.
