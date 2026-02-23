# Agent Operations Guide

## Current Stack Snapshot
- Backend: three .NET 8 services (`AuthServer`, `Gateway`, `Orders`) plus shared libraries in `src/backend/Shared`
- Frontend: Next.js 15 App Router admin portal in `src/web-admin` using a custom OAuth2 Authorization Code + PKCE flow (`lib/oauth2-auth.ts`)
- Data & infra: PostgreSQL via Docker Compose; observability wired through shared logging (`Shared.Logging`) and OpenTelemetry helpers (`Shared.OpenTelemetry`)
- Solution entrypoint: `ArchZ.sln` builds all backend projects; frontend managed with npm + `package-lock.json`

## Local Workflow Checklist
1. `dotnet restore ArchZ.sln && dotnet build ArchZ.sln`
2. `npm install` inside `src/web-admin` (keep `package-lock.json` committed)
3. Launch supporting services as needed:
   - `docker compose up postgres authserver gateway orders`
   - `npm run dev` from `src/web-admin`
4. Frontend talks only to the Gateway at `http://localhost:5002`; AuthServer and Orders stay internal
5. Seed or reset the database with `dotnet ef` tools from `src/backend/AuthServer` when migrations change

## Implementation Guardrails
- Preserve the clean architecture boundaries in each service (controllers -> services -> repositories); new code must follow existing dependency direction
- Use primary constructors for dependency injection across backend projects, matching guidance in `docs/ARCHITECTURE.md`
- Reuse shared logging & telemetry helpers (`AddLoggingServices`, `AddOpenTelemetryAutoInstrumentation`, correlation middleware) instead of rolling custom plumbing
- Maintain the Gateway BFF contract: all browser/API traffic should traverse the Gateway; add YARP routes plus downstream endpoints rather than bypassing it
- Frontend authentication must continue to flow through `OAuth2Auth`; do not introduce NextAuth or alternate token stores without updating backend contracts
- Keep frontend route folders in kebab-case and prefer Server Components unless interactivity requires `use client`

## Quality Gates Before Shipping
- `dotnet test ArchZ.sln` (or focused project test runs) with clean output
- `dotnet format ArchZ.sln` to enforce coding standards
- `npm run lint` and `npm run build` from `src/web-admin`
- Manual smoke: authenticate through the admin UI, hit an Orders endpoint via Gateway, and confirm correlation IDs in service logs

## Documentation & Config Follow-up Queue
- `docs/DEVELOPMENT.md` and `docs/DEPLOYMENT.md` still reference `pnpm` and `src/client`; align them with the npm-based `src/web-admin` app
- `docs/FRONTEND.md` mentions NextAuth and Vite-era structure; update it to describe the Next.js 15 App Router layout plus `lib/oauth2-auth.ts`
- `docs/LOGGING_IMPLEMENTATION.md` lists a `servicea` service; revise examples and text to match the actual `Orders` service naming
- `docker-compose*.yml` files build `./src/client`; update build contexts/ports to target `src/web-admin` if/when the frontend container is restored
- Cross-check environment variable docs so `NEXT_PUBLIC_*` values and OAuth endpoints mirror the current code

## Handy References
- Backend services live under `src/backend/{AuthServer,Gateway,Orders}` with shared utilities in `src/backend/Shared`
- Frontend entry points: routes in `src/web-admin/app`, auth helpers in `src/web-admin/lib/oauth2-auth.ts`, data clients in `src/web-admin/lib/api.ts`
- Observability middleware registered in each backend `Program.cs` via shared extension methods
