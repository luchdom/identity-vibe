# Identity Vibe

.NET 8 identity + orders sample with a Next.js frontend.

## Runtime

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- Primary frontend: `src/web-app`
- Legacy frontend kept during migration: `src/web-admin`

The browser calls `AuthServer` and `Orders` directly. There is no proxy/BFF service in this repo.

## Quick Start

Docker Compose:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

Standalone local processes:

```bash
docker compose up -d postgres
cd src/backend/AuthServer && dotnet run
cd src/backend/Orders && dotnet run
cd src/web-app && pnpm install && pnpm dev
```

## Core Behavior

- Auth uses OAuth2 Authorization Code + PKCE against `AuthServer`
- User profile fetches stay on `GET /connect/userinfo`
- `Orders` route authorization is scope-based
- Runtime role-to-scope assignments live in `AuthServer` DB (`AspNetRoleScopes`)

## Docs

- Agent workflow and routing: `AGENTS.md`
- Backend conventions: `docs/BACKEND.md`
- Runtime architecture: `docs/ARCHITECTURE.md`
- Development: `docs/DEVELOPMENT.md`
- Deployment: `docs/DEPLOYMENT.md`
- Frontend: `docs/FRONTEND.md`
- Testing: `docs/TESTING.md`
