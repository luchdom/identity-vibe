# Order Detail View Crash Fix Plan (`toFixed` on undefined)

Date: 2026-02-26
Status: Planned (pre-implementation)

## Symptom

Frontend order detail page crashes on:

- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx:307`
- `item.totalPrice.toFixed(2)`

Error:

- `Cannot read properties of undefined (reading 'toFixed')`

## Root Cause (confirmed)

This is a **frontend/backend contract mismatch** for order detail payloads, not just a missing null guard.

The frontend detail page (`src/web-admin/app/(authenticated)/orders/[id]/page.tsx`) and frontend types (`src/web-admin/types/orders.ts`) expect:

- `order.customer.name`
- `item.sku`
- `item.description`
- `item.totalPrice`
- `shipping.method`
- `history.status`
- `history.statusDisplay`
- `history.createdBy`

But the Orders API (`src/backend/Orders/Models/Responses/*.cs`) returns (camel-cased in JSON):

- `customer.fullName` (not `name`)
- `item.productSku` (not `sku`)
- `item.productDescription` (not `description`)
- `item.lineTotal` (not `totalPrice`)
- `shipping.shippingMethod` (not `method`)
- `history.toStatus` / `statusChange` / `changedByUserName` (not `status` / `statusDisplay` / `createdBy`)

The immediate crash occurs because `item.totalPrice` is `undefined`, and `.toFixed()` is called.

## Why this needs a proper fix (not only a null-check)

A local guard like:

- `item.totalPrice?.toFixed(2) ?? ...`

would stop the crash, but the page would still render incorrect/missing values for:

- customer name
- SKU / description
- shipping method
- status history

So the correct fix is to **align the frontend detail model with the API response** (via mapping or contract change), and add rendering guards as a safety net.

## Recommended Approach (v1)

### Use a frontend API adapter/normalizer (Recommended)

Keep the backend response contract as-is and normalize it in `web-admin`:

- Introduce API response types for Orders detail payloads (raw wire shape)
- Map raw API detail payload -> current frontend `OrderDetails` UI shape in `orders-api`
- Add small formatting guards in the detail page to prevent hard crashes if data is partial

Why this is recommended:

- Lowest backend risk
- Fastest fix for UI crash + hidden mismatches
- Keeps backend DTO naming free for domain/API concerns
- Centralizes translation logic in one place (`orders-api`), not scattered in UI components

## Alternative (not recommended for this bug fix)

Change backend `Orders` response DTO names to match frontend expectations (`LineTotal` -> `TotalPrice`, etc.).

Cons:

- Larger API contract change
- Higher regression risk (other consumers/tests)
- Does not address missing defensive rendering in UI

## Detailed Implementation Tasks

### 1. Add explicit raw API types for order detail response (frontend)

File:

- `src/web-admin/types/orders.ts` (or split into `src/web-admin/types/orders-api.ts`)

Add raw/wire types for the detail endpoint:

- `ApiCustomerResponse`
- `ApiOrderItemResponse`
- `ApiShippingResponse`
- `ApiOrderStatusHistoryResponse`
- `ApiOrderDetailsResponse`

These should match backend JSON shape (camelCase from .NET serializer), e.g.:

- `fullName`
- `productSku`
- `productDescription`
- `lineTotal`
- `toStatus`
- `statusChange`
- `changedByUserName`

### 2. Normalize `getOrderById()` response in `orders-api`

File:

- `src/web-admin/lib/orders-api.ts`

Change:

- `getOrderById(id): Promise<OrderDetails>`

to:

- fetch `ApiOrderDetailsResponse`
- map raw payload to existing `OrderDetails` UI type

Normalization examples:

- `customer.name = raw.customer.fullName`
- `item.sku = rawItem.productSku`
- `item.description = rawItem.productDescription`
- `item.totalPrice = rawItem.lineTotal`
- `shipping.method = raw.shippingMethod ?? 'Standard'` (or empty string; see open question)
- `history.status = rawHistory.toStatus`
- `history.statusDisplay = rawHistory.toStatus` enum -> label mapping or derive from numeric enum
- `history.createdBy = rawHistory.changedByUserName ?? rawHistory.changedByUserId`

Important:

- Keep list endpoints (`getOrders`, `getAllOrders`) unchanged unless you find similar mismatches there.
- Keep the mapper local/private in `orders-api.ts` or move to `src/web-admin/lib/mappers/orders.ts` if it grows.

### 3. Add defensive numeric rendering in order detail page

File:

- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx`

Even after normalization, avoid hard-crashing UI on malformed/partial data:

- Wrap numeric formatting with a safe formatter helper (`formatMoney(value)`)
- Guard `order.items` array access if needed

Example behavior:

- if number is missing/invalid -> render `--` instead of crashing

This is a resilience improvement, not the main contract fix.

### 4. Align frontend detail-page field usage with normalized model

File:

- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx`

Verify all fields rendered on the page exist after mapping:

- customer section
- items section
- shipping section
- status history section

Hidden mismatch likely to fix here:

- `history.status` + `history.statusDisplay` + `history.createdBy` derived from backend history response

### 5. Add regression tests (frontend)

#### 5a. Unit test for order detail mapper (recommended)

New file (recommended):

- `src/web-admin/lib/__tests__/orders-api.mapper.test.ts`

Test cases:

- maps `lineTotal -> totalPrice`
- maps `productSku -> sku`
- maps `productDescription -> description`
- maps `fullName -> customer.name`
- maps history fields (`toStatus`, `changedByUserName`) to UI fields
- handles missing optional values safely

If no unit-test setup exists for frontend, this can be deferred, but it is high value for this specific bug class.

#### 5b. Playwright detail page spec update/add

File:

- `src/web-admin/tests/orders-page.spec.ts` and/or a new detail-specific spec

Add a mocked order detail payload using **backend-real field names**:

- `lineTotal`, `productSku`, `fullName`, `toStatus`, etc.

Assertions:

- page renders item totals correctly
- no runtime crash
- status history renders

This prevents future frontend assumptions from drifting away from backend contracts.

### 6. Optional backend contract documentation (recommended)

Docs:

- `docs/FRONTEND.md`
- `docs/ARCHITECTURE.md` (only if you want a general contract note)

Add a short note:

- frontend should normalize backend DTOs at API boundary (`orders-api`) instead of assuming UI models are identical to transport models

## Validation Checklist (after implementation)

1. Open `/orders/{id}` for an order with items and status history
2. Confirm item totals render (no `toFixed` crash)
3. Confirm customer name renders correctly
4. Confirm shipping section renders expected method/address values
5. Confirm status history renders with status badge + actor
6. Run `npx tsc --noEmit` in `src/web-admin`
7. Run affected Playwright spec(s)

## Suggested Implementation Order

1. Add raw API detail types
2. Add `orders-api` mapper + normalize `getOrderById`
3. Add safe number formatter in detail page
4. Run manual detail-page smoke test
5. Add/update tests

## Open Question (single decision)

For `shipping.method` in the frontend UI, the backend `ShippingResponse` currently may not populate `shippingMethod` for some existing records.

Choose display behavior:

1. Show `Unknown`/`N/A` when missing (Recommended)
2. Hide the method line when missing
3. Display a fallback literal like `Standard`

Recommended: **Option 1** (`N/A`) to avoid inventing data.
