# Orders PostgreSQL Migration Error Plan (`datetime('now')` on Npgsql)

Date: 2026-02-26
Status: Implemented

## Symptom

`Orders` startup fails during EF migration with:

- `Npgsql.PostgresException ... 42883: function datetime(unknown) does not exist`

This occurs during `context.Database.MigrateAsync()` in:
- `src/backend/Orders/Program.cs`

## Root cause (confirmed)

The `Orders` EF model still uses SQLite-specific default SQL expressions:

- `HasDefaultValueSql("datetime('now')")`

These appear in entity configurations such as:
- `src/backend/Orders/Data/Configurations/CustomerConfiguration.cs`
- `src/backend/Orders/Data/Configurations/OrderConfiguration.cs`
- `src/backend/Orders/Data/Configurations/OrderItemConfiguration.cs`
- `src/backend/Orders/Data/Configurations/OrderStatusHistoryConfiguration.cs`
- `src/backend/Orders/Data/Configurations/IdempotencyRecordConfiguration.cs`

When the PostgreSQL initial migration was generated, those values were copied into:
- `src/backend/Orders/Data/Migrations/20260226213343_InitialCreate.cs`
- `src/backend/Orders/Data/Migrations/*.Designer.cs`
- `OrdersDbContextModelSnapshot.cs`

PostgreSQL has no `datetime(...)` function, so migration execution fails.

## Selected fix strategy

Remove DB-side timestamp defaults entirely and rely on application/entity-side timestamps.

Decision:
- User selected option `2` (no DB-side timestamp defaults).

Rationale (accepted tradeoff):
- avoids provider-specific SQL defaults entirely
- simplifies model portability across PostgreSQL and tests
- timestamps are already set by entity initializers/application code in normal flows and seeding

## Implemented fix

### 1. Remove timestamp default SQL from Orders EF configurations

Files:
- `src/backend/Orders/Data/Configurations/CustomerConfiguration.cs`
- `src/backend/Orders/Data/Configurations/OrderConfiguration.cs`
- `src/backend/Orders/Data/Configurations/OrderItemConfiguration.cs`
- `src/backend/Orders/Data/Configurations/OrderStatusHistoryConfiguration.cs`
- `src/backend/Orders/Data/Configurations/IdempotencyRecordConfiguration.cs`

Change applied:
- remove `HasDefaultValueSql(...)` for timestamp columns (`CreatedAt`, `UpdatedAt`) entirely

### 2. Regenerated the initial Orders migration

Because the current migration is brand new and failing on first application:

- removed the generated migration files:
  - `20260226213343_InitialCreate.cs`
  - `20260226213343_InitialCreate.Designer.cs`
  - `OrdersDbContextModelSnapshot.cs`
- regenerate `InitialCreate`

Reason:
- cleaner than hand-editing migration + snapshot
- ensures model snapshot and migration stay consistent

Alternative (not preferred):
- manually patch migration + designer + snapshot

### 3. Reset the Orders database (or full Postgres volume) before retry

If migration partially executed, the `Orders` DB may be in a half-created state.

Recommended local reset:

```bash
docker compose down -v
docker compose up -d postgres
```

Then restart services.

If you want to avoid resetting `AuthServer` too, you can drop only the `Orders` database and recreate it.

### 4. Validation

- `dotnet build src/backend/Orders/Orders.csproj -p:OutDir=C:\temp\orders-datetime-fix-build\` ✅
- `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj -p:OutDir=C:\temp\orders-datetime-fix-tests\` ✅ (`4/4`)
- Verified regenerated migration contains no `datetime('now')` or `defaultValueSql` timestamp expressions ✅
- Runtime retry against a fresh Postgres `Orders` DB is still required (manual)

## Consequences / follow-up checks

- No DB-level fallback timestamps if inserts omit `CreatedAt`/`UpdatedAt`
- Validate main write paths still set timestamps explicitly or via entity defaults:
  - create order
  - create order items
  - status history writes
  - idempotency records
  - JSON seed import

This is acceptable for this project as long as runtime paths are covered by tests/smoke validation.
