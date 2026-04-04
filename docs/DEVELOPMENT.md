# Development Guide

## Services

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- `src/web-app`: `http://localhost:3001` in standalone dev
- Legacy Docker `client`: `http://localhost:3000`

## Start Locally

Docker Compose:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

Standalone:

```bash
docker compose up -d postgres
cd src/backend/AuthServer && dotnet run
cd src/backend/Orders && dotnet run
cd src/web-app && pnpm install && pnpm dev
```

## Frontend Dev Env

`src/web-app/.env.local`

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
NEXT_PUBLIC_APP_BASE_URL=http://localhost:3001
```

## Backend Notes

- Backend architecture and naming rules: `docs/BACKEND.md`
- Runtime/security model: `docs/ARCHITECTURE.md`
- Role-to-scope runtime source of truth: `AuthServer` DB (`AspNetRoleScopes`)
- Keep `/connect/userinfo` for profile fetches

## Frontend Notes

- Frontend implementation rules live in `docs/FRONTEND.md`
- Prefer `src/web-app` for active work unless a task explicitly targets `src/web-admin`

## Smoke Checklist

1. Login through `src/web-app`
2. Verify `/connect/userinfo` returns profile data
3. Verify orders list loads from `Orders`
4. Verify scope-based access behaves correctly for user/admin paths

## Troubleshooting

- CORS issues: check `Cors:AllowedOrigins`
- Token validation issues in `Orders`: check `Authentication:Authority`
- Auth migration/local DB mismatch (`42P07`): reset local Docker volumes if needed
