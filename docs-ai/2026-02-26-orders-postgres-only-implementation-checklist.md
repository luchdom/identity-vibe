# Orders PostgreSQL-Only Implementation Checklist

Date: 2026-02-26
Status: Tasked
Related:
- `ai-analysis/2026-02-26-orders-postgres-only-plan.md`

## Confirmed decisions

- `Orders` must be PostgreSQL-only everywhere (Docker + local `dotnet run`)
- Keep separate databases per service on the same PostgreSQL server (`AuthServer` and `Orders`)
- Transition `Orders` startup from `EnsureCreated()` to `MigrateAsync()` now
- Keep `Orders.UnitTests` (seeder logic) on SQLite in-memory for speed

## Goal

Remove SQLite from `Orders`, make PostgreSQL the only runtime database, add EF migrations for `Orders`, and align Docker/local docs.

---

## A. Orders runtime provider (PostgreSQL-only)

### A1. Remove SQLite fallback from runtime

- [x] Update `src/backend/Orders/Program.cs`
- [x] Remove provider branching/fallback logic
- [x] Always use `UseNpgsql(connectionString)` for `OrdersDbContext`
- [x] Keep clear error if connection string missing

### A2. Package cleanup

- [x] Update `src/backend/Orders/Orders.csproj`
- [x] Remove `Microsoft.EntityFrameworkCore.Sqlite` package reference from runtime project
- [x] Keep `Npgsql.EntityFrameworkCore.PostgreSQL`
- [x] Keep `Microsoft.EntityFrameworkCore.Design`

Note:
- `Orders.UnitTests` may still use SQLite provider independently (test project package only)

---

## B. Orders schema lifecycle (migrations)

### B1. Switch startup to migrations

- [x] Update `src/backend/Orders/Program.cs`
- [x] Replace `await context.Database.EnsureCreatedAsync()` with `await context.Database.MigrateAsync()`
- [x] Ensure seeder still runs after migrations

### B2. Create initial `Orders` EF migration

- [x] Generate migration under `src/backend/Orders/Data/Migrations/`
- [x] Confirm migration includes current `Orders` schema (customers/orders/items/history/idempotency)
- [x] Confirm migration is Npgsql-compatible

### B3. Migration transition docs

- [x] Document reset guidance for local Postgres volumes when switching from old schema state (if needed)

---

## C. Orders config (Postgres-only defaults)

### C1. Appsettings defaults

- [x] Update `src/backend/Orders/appsettings.json`
  - set Postgres localhost connection string (`Orders` DB)
- [x] Update `src/backend/Orders/appsettings.Development.json`
  - set Postgres localhost connection string (`Orders` DB)
- [x] Keep `src/backend/Orders/appsettings.Docker.json` on Postgres service-host connection string

### C2. Remove SQLite references

- [x] Remove stale `orders.db` references from Orders docs/config comments/examples

---

## D. Docker Compose / Postgres (shared server, separate DBs)

### D1. Compose validation (already partially wired)

- [x] Confirm `docker-compose.yml` passes `ConnectionStrings__DefaultConnection` to `orders`
- [x] Confirm `docker-compose.dev.yml` passes `ConnectionStrings__DefaultConnection` to `orders`
- [x] Confirm `docker-compose.prod.yml` passes `ConnectionStrings__DefaultConnection` to `orders`

### D2. Postgres init scripts

- [x] Confirm `docker/postgres/init/01-create-orders-db.sql` creates `Orders` DB
- [x] Confirm compose mounts init scripts for base/dev/prod startup path (base + prod overlays as applicable)

### D3. DB-only startup command docs

- [x] Ensure docs/README include `docker compose up -d postgres` (both DBs on one service)

---

## E. Seeder compatibility (Orders JSON sample import)

### E1. Runtime compatibility smoke (manual / validation)

- [ ] Confirm JSON seeding still imports on fresh Postgres `Orders` DB (`IfEmpty`)
- [x] Confirm seed file is still copied to build/publish output
- [ ] Confirm `/orders/all` returns seeded data after startup

---

## F. Tests and validation

### F1. Existing unit tests

- [x] `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj`
- [x] Confirm seeder tests still pass (SQLite in-memory unit scope)

### F2. Build & compose validation

- [x] `dotnet build src/backend/Orders/Orders.csproj`
- [x] `docker compose -f docker-compose.yml -f docker-compose.dev.yml config`
- [x] `docker compose -f docker-compose.yml -f docker-compose.prod.yml config`

### F3. Optional runtime validation

- [ ] `docker compose down -v`
- [ ] `docker compose up -d postgres`
- [ ] Start `authserver` + `orders` and verify migrations + seeding logs

---

## G. Docs updates

- [x] Update `README.md`
  - Orders is PostgreSQL-only
  - shared Postgres server / separate DBs
  - DB-only startup command
- [x] Update `docs/DEVELOPMENT.md`
  - local `Orders` uses Postgres (no SQLite workflow)
  - reseed/reset guidance via Postgres volume reset
- [x] Update `docs/TESTING.md` only if commands/notes changed materially

---

## Implementation log

- Implemented PostgreSQL-only runtime for `Orders`:
  - removed SQLite fallback/provider usage from `src/backend/Orders/Program.cs`
  - removed runtime `Microsoft.EntityFrameworkCore.Sqlite` package from `src/backend/Orders/Orders.csproj`
- Switched `Orders` startup schema creation to EF migrations (`MigrateAsync`) in `src/backend/Orders/Program.cs`.
- Generated initial PostgreSQL migration baseline under `src/backend/Orders/Data/Migrations/`:
  - `20260226213343_InitialCreate.cs`
  - `20260226213343_InitialCreate.Designer.cs`
  - `OrdersDbContextModelSnapshot.cs`
- Updated Orders appsettings defaults (`appsettings.json`, `appsettings.Development.json`) to PostgreSQL localhost `Orders` DB.
- Kept Docker `Orders` config on PostgreSQL and validated compose env wiring + init script mount for `Orders` DB creation.
- Updated docs (`README.md`, `docs/DEVELOPMENT.md`) to remove SQLite runtime workflow and document Postgres-only usage + DB-only startup command.
- Validation executed:
  - `dotnet ef migrations add InitialCreate --project src/backend/Orders/Orders.csproj --startup-project src/backend/Orders/Orders.csproj --output-dir Data/Migrations --framework net8.0 --configuration Release` ✅
  - `dotnet build src/backend/Orders/Orders.csproj -p:OutDir=C:\\temp\\orders-postgres-only-build\\` ✅
  - `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj -p:OutDir=C:\\temp\\orders-postgres-only-tests\\` ✅ (`4/4`)
  - `docker compose -f docker-compose.yml -f docker-compose.dev.yml config` ✅
  - `docker compose -f docker-compose.yml -f docker-compose.prod.yml config` ✅
- Still pending manual runtime smoke on a fresh Postgres volume:
  - confirm `Orders` migrations apply on startup
  - confirm JSON seed import runs on empty `Orders` DB
  - confirm `/orders/all` returns seeded data
