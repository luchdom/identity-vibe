# Orders PostgreSQL-Only Plan (Remove SQLite Fallback)

Date: 2026-02-26
Status: Planned (awaiting final decisions)

## Request summary

Change `Orders` to **always use PostgreSQL** (same operational style as `AuthServer`), remove SQLite fallback behavior, and update Docker/docs accordingly.

Also document a command to start only DB services with Docker Compose.

## Current state (audited)

- `AuthServer` already uses PostgreSQL in Docker and local dev can point to PostgreSQL.
- `Orders` currently:
  - can use PostgreSQL (provider wiring was added)
  - still falls back to SQLite in `src/backend/Orders/Program.cs`
  - still runs `EnsureCreatedAsync()` on startup
  - has an empty `src/backend/Orders/Data/Migrations/` folder (no real migrations baseline)
- Docker Compose currently uses **one PostgreSQL container** with **separate DBs**:
  - `AuthServer`
  - `Orders`
  (via init SQL)

## Recommended target architecture

- Keep **one PostgreSQL server/container** in dev Docker Compose
- Use **separate databases per service** (recommended):
  - `AuthServer`
  - `Orders`
- Make `Orders` **PostgreSQL-only**
  - no SQLite provider fallback
  - no `orders.db` dev path
- Move `Orders` startup from `EnsureCreatedAsync()` to **EF Core migrations (`MigrateAsync`)**
  - same direction as `AuthServer`

This aligns better with service boundaries and avoids SQLite-vs-Postgres behavior drift.

## Why separate DBs (recommended)

- Preserves service isolation boundaries
- Avoids accidental cross-service schema coupling
- Keeps migration/seeding lifecycles independent
- Still simple operationally (same Postgres container/server)

## Detailed implementation plan

### 1. Orders DB provider hardening (Postgres-only)

Files:
- `src/backend/Orders/Program.cs`
- `src/backend/Orders/Orders.csproj`

Changes:
- Remove SQLite provider fallback branch in `Program.cs`
- Always call `UseNpgsql(connectionString)`
- Remove `Microsoft.EntityFrameworkCore.Sqlite` package from `Orders.csproj` (optional but recommended in same pass)
- Keep/add `Npgsql.EntityFrameworkCore.PostgreSQL`

Validation:
- `dotnet build src/backend/Orders/Orders.csproj`

### 2. Orders schema lifecycle: switch to migrations

Files:
- `src/backend/Orders/Program.cs`
- `src/backend/Orders/Data/Migrations/*` (new)

Changes:
- Replace `await context.Database.EnsureCreatedAsync()` with `await context.Database.MigrateAsync()`
- Create initial `Orders` migration (PostgreSQL provider)
- Ensure startup seeding still runs after migrations

Risk / note:
- Existing local SQLite DBs are no longer used
- Existing Postgres `Orders` DBs created by `EnsureCreated()` (if any) may require reset, same pattern as AuthServer migration transition

Validation:
- Start `Orders` against a clean Postgres `Orders` DB
- Confirm migrations apply and seeding runs

### 3. Configuration cleanup (Orders appsettings/env)

Files:
- `src/backend/Orders/appsettings.json`
- `src/backend/Orders/appsettings.Development.json`
- `src/backend/Orders/appsettings.Docker.json`
- optional root `.env*` docs/examples if connection strings are documented there

Changes:
- Set development/default `ConnectionStrings:DefaultConnection` to Postgres (localhost)
  - e.g. `Host=localhost;Database=Orders;Username=postgres;Password=postgres`
- Keep Docker connection string using service hostname `postgres`
- Remove SQLite `orders.db` connection strings and docs references

Recommendation:
- Keep local dev connection string in `appsettings.Development.json`
- Allow env override via `ConnectionStrings__DefaultConnection`

### 4. Docker Compose / Postgres init alignment

Files:
- `docker-compose.yml`
- `docker-compose.dev.yml`
- `docker-compose.prod.yml`
- `docker/postgres/init/01-create-orders-db.sql`

Changes:
- Confirm `orders` service always gets Postgres connection string
- Keep separate DB names:
  - `AuthServer`
  - `Orders`
- Keep Postgres init script mount in base/prod compose
- (Optional) add a second init script for roles/permissions if ever needed later (not now)

Validation:
- `docker compose ... config`
- `docker compose up -d postgres`
- confirm both DBs exist

### 5. Seeder compatibility (Orders JSON sample data)

Files:
- `src/backend/Orders/Data/DatabaseSeeder.cs`

Changes:
- No logic change expected
- Verify JSON seed import works against PostgreSQL provider
- Keep `IfEmpty` behavior

Validation:
- Fresh Postgres `Orders` DB -> startup imports ~150 orders
- `/orders/all` returns seeded data

### 6. Tests

Files:
- `tests/backend/Orders.UnitTests/*` (existing seeder tests)
- potentially new integration test later (optional)

Changes:
- Existing seeder tests can remain SQLite in-memory **only if** we decide unit tests may use provider-independent local DBs
  - They test seeder logic, not provider-specific SQL
- Optional follow-up: add a PostgreSQL integration smoke test for `Orders` startup + seeding

Recommendation:
- Keep unit tests as-is for speed
- Add one manual runtime validation checklist for Docker/Postgres

### 7. Docs / README updates

Files:
- `README.md`
- `docs/DEVELOPMENT.md`
- `docs/ARCHITECTURE.md` (small note, optional)
- `docs/TESTING.md` (if test guidance changes)

Changes:
- State that `Orders` uses PostgreSQL (same Postgres server, separate DB)
- Remove SQLite local dev references (`orders.db`) if we go fully Postgres-only
- Add/keep "DBs only" startup command:
  - `docker compose up -d postgres`
- Add reset guidance for Orders/Postgres reseeding:
  - `docker compose down -v`
  - restart `postgres` then services

## What I’d implement by default (recommended)

1. PostgreSQL-only `Orders` provider (remove SQLite fallback)
2. Separate DBs on same Postgres server (`AuthServer` + `Orders`)
3. Switch `Orders` startup to `MigrateAsync()` and add initial migration
4. Update docs to reflect Postgres-only local/dev workflow
5. Keep `Orders.UnitTests` seeder tests on SQLite in-memory for speed (unit scope)

## Open questions (to confirm before implementation)

1. Do you want `Orders` local **non-Docker** runs (`dotnet run`) to also use PostgreSQL by default (recommended), meaning no more `orders.db` workflow at all?
2. Are you okay with an `Orders` migration transition now (`EnsureCreated` -> `MigrateAsync` + initial migration)?
3. Do you want to keep `Orders.UnitTests` using SQLite in-memory (fast unit tests), or move them to PostgreSQL too?

## Confirmed decisions

- `Orders` must be PostgreSQL-only everywhere (Docker + local `dotnet run`)
- Do not keep SQLite fallback / `orders.db` workflow
