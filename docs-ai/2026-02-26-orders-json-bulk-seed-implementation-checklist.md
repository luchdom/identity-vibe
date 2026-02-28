# Orders JSON Bulk Seed Implementation Checklist

Date: 2026-02-26
Status: Tasked (implementation checklist)
Related:
- `ai-analysis/2026-02-26-orders-json-bulk-seed-plan.md`

## Confirmed decisions

- Seed data is primarily for admin/all-orders dashboards (`Order.UserId` uses synthetic IDs)
- Seeding mode: `IfEmpty` (only seed when `Orders` table is empty)
- Dataset source: committed JSON file in repo
- Sample size: ~150 orders
- Replace current hardcoded Orders sample seeding logic entirely

## Goal

Add a deterministic JSON dataset (~150 orders) to `Orders` and seed it on startup when the Orders table is empty, to support richer demos and dashboards.

Note:
- Do not add seed configuration to `appsettings*.json`; use a dedicated seed JSON file only.

---

## A. Seed Dataset Design & File

### A1. Create committed JSON dataset file

- [x] Add `src/backend/Orders/Data/Seed/orders.sample.v1.json`

Requirements:
- root object with metadata + `orders` array
- ~150 orders
- deterministic (committed, not generated at startup)
- unique `orderNumber`
- nested aggregate shape per order:
  - `customer`
  - `items`
  - `statusHistory`

### A2. Dataset coverage quality (for dashboard usefulness)

- [x] Ensure mixed statuses across dataset:
  - `Draft`
  - `Confirmed`
  - `Processing`
  - `Shipped`
  - `Delivered`
  - `Cancelled`

- [x] Ensure date spread (recommended last 90-180 days)

- [x] Ensure varied item counts (1-6)

- [x] Ensure varied categories/prices/customers

- [x] Use synthetic `userId` values (e.g. `seed-user-001`, `seed-user-002`, ...)

---

## B. Backend Seed Import Models (`Orders`)

### B1. Add JSON seed DTO models

Recommended folder:
- [x] Add `src/backend/Orders/Data/Seed/Models/OrdersSeedDataset.cs`
- [x] Add `src/backend/Orders/Data/Seed/Models/SeedOrder.cs`
- [x] Add `src/backend/Orders/Data/Seed/Models/SeedCustomer.cs`
- [x] Add `src/backend/Orders/Data/Seed/Models/SeedOrderItem.cs`
- [x] Add `src/backend/Orders/Data/Seed/Models/SeedOrderStatusHistory.cs`

Conventions:
- prefer `sealed record` + `init` + `required`
- JSON-shape aligned to dataset file

### B2. JSON serialization settings (if needed)

- [x] Decide if custom `JsonSerializerOptions` are needed (case-insensitive enum parsing, comments, etc.)
- [x] Keep parsing explicit and deterministic (avoid overly permissive parsing)

---

## C. Refactor `Orders` `DatabaseSeeder` to JSON-based Seeding

File:
- [x] Refactor `src/backend/Orders/Data/DatabaseSeeder.cs`

### C1. Replace hardcoded seed logic

- [x] Remove/replace `SeedCustomersAsync()` hardcoded customer list
- [x] Remove/replace `SeedSampleOrdersAsync()` hardcoded single order logic

### C2. Add JSON seeding entry point

- [x] Add `SeedOrdersFromJsonIfEmptyAsync()` (or equivalent)

Behavior:
- if `_context.Orders.Any()` => skip and log
- otherwise load dataset and import

### C3. Add dataset loading method(s)

- [x] Add JSON file loading (content-root-relative path)
- [x] Add deserialization into seed DTOs
- [x] Fail fast with clear exception/log if file missing or invalid

### C4. Add dataset validation before writing to DB

- [x] Validate dataset exists and has orders
- [x] Validate order count is at least 100 (or exact/near target threshold)
- [x] Validate unique `orderNumber`
- [x] Validate required fields per order/customer/item/history
- [x] Validate enum values (`OrderStatus`)
- [x] Validate numeric values (non-negative prices/amounts, quantity > 0)
- [x] Validate each order has >= 1 item

### C5. Customer import (dedupe by email)

- [x] Implement customer lookup/reuse by email (unique index safe)
- [x] Create missing customers only
- [x] Do not overwrite existing customers in v1

### C6. Order import

- [x] Map seed order -> `Order` entity
- [x] Map seed items -> `OrderItem` entities
- [x] Map seed history -> `OrderStatusHistory` entities
- [x] Set `Source = "SEED_JSON"` (or consistent seed source marker)
- [x] Preserve provided timestamps (`CreatedAt`, `UpdatedAt`, `ShippedAt`, etc.)
- [x] Set status-specific timestamps consistently with final status

### C7. Totals and consistency

- [x] Compute item `LineTotal` via business logic (`CalculateLineTotal()`)
- [x] Compute order totals via `UpdateTotals()`
- [x] Apply tax/shipping/discount from seed data before total recompute
- [ ] (Optional) log warning if JSON-provided totals differ from computed totals

### C8. Save strategy

- [x] Use transaction for the import batch (recommended)
- [x] Save in sensible batches or single transaction (dataset is only ~150 orders)
- [x] Log summary counts:
  - customers created/reused
  - orders created
  - items created
  - status history rows created

---

## D. Orders App Startup / Config

### D1. Keep startup seeding integration working

- [x] Confirm `src/backend/Orders/Program.cs` still calls seeder once on startup
- [x] Ensure JSON seeding path uses app content root correctly when run via:
  - local `dotnet run`
  - Docker
  - tests (if seeder tests instantiate app host)

### D2. No `appsettings` seed config (confirmed)

- [x] Use a dedicated seed JSON file path in seeder code (content-root-relative)
- [x] Do not add `SeedData` sections to `src/backend/Orders/appsettings*.json`
- [x] Document dataset path and `IfEmpty` behavior in `docs/DEVELOPMENT.md`

---

## E. Data Generation (one-time) & Dataset Quality Checks

### E1. Generate dataset (one-time script/tool)

- [ ] Create one-time generator script/tool (repo `tools/` or temporary script)
- [x] Generate ~150 orders with realistic distributions
- [x] Commit the resulting JSON file

### E2. Sanity-check generated file before implementation finalization

- [x] Confirm valid JSON
- [x] Confirm no duplicate order numbers
- [x] Confirm no duplicate customer emails unless intended
- [x] Confirm statuses and timestamps align logically
- [x] Confirm some orders include shipping/tracking and some do not
- [x] Confirm status history progression is sensible

---

## F. Tests (recommended)

### F1. Orders seeder tests (new)

- [x] Add Orders seeder test project if missing (or extend existing Orders tests)
- [x] Add test: seeds JSON dataset when DB is empty
- [x] Add test: skips seeding when orders already exist (`IfEmpty`)
- [x] Add test: reuses existing customer by email
- [x] Add test: computes order/item totals correctly
- [x] Add test: invalid dataset fails fast (duplicate order number / missing items / invalid status)

### F2. Runtime smoke validation (manual)

- [ ] Start `Orders` (with empty DB)
- [ ] Verify startup logs show JSON seed import summary
- [ ] Call `GET /orders/all` and confirm multiple pages/statuses
- [ ] Open several order details and verify items/history render

---

## G. Docs Updates

### G1. Development docs

- [x] Update `docs/DEVELOPMENT.md`

Add:
- Orders sample JSON seeding behavior (`IfEmpty`)
- location of dataset file
- note that sample orders are mainly for admin/all-orders dashboards
- reset guidance for Orders DB if reseeding is desired

### G2. Testing docs (if seeder tests added)

- [x] Update `docs/TESTING.md` with Orders seeder test command(s)

### G3. Architecture/docs note (optional)

- [ ] Update `docs/ARCHITECTURE.md` or `docs/FRONTEND.md` with a short note that sample data is demo-only and synthetic

---

## H. Validation Checklist (final)

### Backend/runtime

- [x] `dotnet build src/backend/Orders/Orders.csproj`
- [ ] Start `Orders` with empty DB and confirm seeding completes
- [ ] `GET /orders/all?page=1&pageSize=10` returns seeded results
- [ ] Status filtering returns non-empty results for multiple statuses
- [ ] `GET /orders/{id}` returns detail with items + status history for seeded order(s)

### UX/demo readiness

- [ ] `web-admin` admin/all-orders views show enough sample data for demos
- [ ] Sample data supports dashboard development (status/time distribution)

---

## Implementation notes / guardrails

- Keep backend API contracts unchanged; this task is seed-data only.
- Do not couple Orders seeding to AuthServer DB/user IDs (synthetic `UserId`s are intentional).
- Prefer deterministic committed data over runtime randomness.
- Keep seeding idempotent and startup-safe (`IfEmpty` skip).
- Replace the old hardcoded sample seed path to avoid duplicate maintenance.

## Implementation log (update during execution)

- Implemented JSON-based Orders seeding via committed dataset file `src/backend/Orders/Data/Seed/orders.sample.v1.json` (~150 deterministic orders).
- Refactored `src/backend/Orders/Data/DatabaseSeeder.cs` to `IfEmpty` JSON import with validation, transaction, customer dedupe, totals recompute, and summary logging.
- Added seed DTO records under `src/backend/Orders/Data/Seed/Models/`.
- Wired `DatabaseSeeder` startup to use app content root and logger in `src/backend/Orders/Program.cs`.
- Added content-copy metadata for the seed file in `src/backend/Orders/Orders.csproj` (build/publish friendly).
- Added `tests/backend/Orders.UnitTests` seeder tests (`4/4` passing with SQLite in-memory).
- Added docs for Orders sample JSON seeding and test command in `docs/DEVELOPMENT.md` and `docs/TESTING.md`.
- Validation executed:
  - `dotnet build src/backend/Orders/Orders.csproj -p:OutDir=C:\\temp\\orders-seed-build\\` ✅
  - `dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj -p:OutDir=C:\\temp\\orders-unit-tests\\` ✅
- Not run yet: manual runtime smoke (`Orders` startup import logs, `/orders/all` and detail endpoints against a freshly reset DB).
