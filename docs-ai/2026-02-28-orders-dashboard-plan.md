# Orders Dashboard Plan

Date: 2026-02-28
Status: Planning (pre-implementation)
Related:
- `src/web-admin/app/page.tsx`
- `src/web-admin/app/(authenticated)/dashboard/page.tsx`
- `src/web-admin/components/app-sidebar.tsx`
- `src/web-admin/lib/orders-api.ts`
- `src/web-admin/types/orders.ts`
- `src/web-admin/hooks/use-orders.ts`
- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/Orders/Services/Interfaces/IOrdersService.cs`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/Orders/Repositories/Interfaces/IOrdersRepository.cs`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/backend/Orders/Data/DatabaseSeeder.cs`

## Summary of Requested Changes

1. Replace the placeholder dashboard with a real orders dashboard at `/dashboard`.
2. Keep `/` as the logged-in default route and continue redirecting authenticated users to `/dashboard`.
3. Scope the dashboard to orders only:
- no user-management or role-management widgets
- no separate user dashboard
4. Add graphs so the page is a visual operational overview, not just metric cards.
5. Consider a dedicated Orders backend endpoint that returns the full dashboard payload in one request.
6. Keep the design polished and intentional while staying consistent with the current web admin stack.

## Current Codebase Findings

### Routing and layout
- `src/web-admin/app/page.tsx` already redirects authenticated users to `/dashboard` and unauthenticated users to `/login`.
- `src/web-admin/app/(authenticated)/dashboard/page.tsx` is still the default placeholder content generated from the current shell layout.
- `src/web-admin/app/(authenticated)/layout.tsx` already wraps authenticated routes with `AuthGuard` and the shared sidebar shell.

Implication:
- The default-page requirement is already satisfied.
- The main work is replacing placeholder content and improving navigation polish.

### Sidebar / navigation
- `src/web-admin/components/app-sidebar.tsx` already contains a Dashboard section and an Orders section.
- The brand link currently points to `#` instead of `/dashboard`.
- The collapsible menu currently defaults the Orders group open via `index === 1`, which makes the dashboard feel secondary even though it is the home page.

Implication:
- Navigation changes are small but important for perceived IA quality.

### Orders frontend data access
- `src/web-admin/lib/orders-api.ts` supports:
  - `getOrders(...)`
  - `getAllOrders(...)`
  - `getOrderById(...)`
  - status/update/create helpers
- `src/web-admin/types/orders.ts` currently models order summaries, details, and list pagination only.
- `src/web-admin/hooks/use-orders.ts` supports paged list loading only.

Implication:
- The current frontend API layer is strong for list/detail screens but not ideal for dashboard aggregation.
- A dashboard should not compute its full UI by repeatedly walking paginated list endpoints.

### Orders backend API
- `src/backend/Orders/Controllers/OrdersController.cs` currently exposes:
  - `GET /orders`
  - `GET /orders/all`
  - `GET /orders/{id}`
  - `GET /orders/{id}/status`
  - create/update/cancel/admin actions
- There is no dedicated aggregate endpoint for dashboard data.

Implication:
- The cleanest implementation is to add a summary endpoint such as `GET /orders/dashboard`.

### Existing data is already sufficient for a compelling dashboard
- `src/backend/Orders/Data/DatabaseSeeder.cs` seeds orders from JSON and requires at least 100 orders.
- The current sample dataset contains 150 seeded orders, which is enough for real charts and summary cards.
- The sample data spans six months and includes multiple statuses, customer reuse, and enough volume for trend visualizations.

Relevant current seed shape:
- 150 total orders
- six statuses already modeled in the domain (`Draft`, `Confirmed`, `Processing`, `Shipped`, `Delivered`, `Cancelled`)
- six-month spread across roughly `2025-09` through `2026-02`

Implication:
- The dashboard can ship with meaningful charts immediately and does not need synthetic UI placeholders.

## Recommended Product Scope

Build `/dashboard` as an orders operations overview page.

Primary goals:
- show current order health at a glance
- show volume and revenue trends over time
- provide a fast jump-off point into the main Orders management flow

Explicitly out of scope for this feature:
- user-management metrics
- roles / permissions metrics
- auth / identity analytics
- a separate personalized “user dashboard” concept beyond the normal order scoping rules

## Recommended Backend Approach

### Add a dedicated dashboard endpoint

Recommended endpoint:
- `GET /orders/dashboard`

Authorization:
- require `orders.read`
- if the caller has `orders.manage` (or `admin.full` through existing auth behavior), return global aggregates
- otherwise return only the caller's own order aggregates

Why this is the right design:
- avoids over-fetching paginated list data
- lets the backend perform efficient grouped queries
- guarantees one stable contract for cards + charts + recent activity
- keeps frontend rendering simple and fast

### Recommended response contract

Add a dedicated response model for the dashboard payload, for example:
- `OrderDashboardResponse`
- `OrderDashboardSummaryResponse`
- `OrderDashboardStatusBucketResponse`
- `OrderDashboardTrendPointResponse`
- `OrderDashboardRecentOrderResponse`
- `OrderDashboardFulfillmentResponse`

Recommended response fields:

- `scope`
  - `self` or `all`
- `generatedAt`
- `dateRange`
  - `from`
  - `to`
- `summary`
  - `totalOrders`
  - `totalRevenue`
  - `averageOrderValue`
  - `totalUnits`
  - `activePipelineOrders`
- `statusBreakdown`
  - array of status/count/percentage rows for all six statuses
- `monthlyTrend`
  - array of month buckets with:
    - `month`
    - `label`
    - `orderCount`
    - `revenue`
- `fulfillmentMetrics`
  - `deliveredCount`
  - `shippedCount`
  - `cancelledCount`
  - `deliveryRate`
  - `cancellationRate`
- `recentOrders`
  - latest 5 to 8 orders with enough fields to link to detail pages

Recommended v1 behavior:
- include all six statuses even when one of them has `0` rows in the selected scope
- default the date range to the full available scope for now
- keep the contract read-only and purpose-built for dashboard rendering

### Backend service-layer changes

1. Add a dashboard query model that carries:
- caller user ID
- whether the request is admin-scoped

2. Extend `IOrdersService` with a method such as:
- `Task<Result<OrderDashboardResult>> GetDashboardAsync(GetOrderDashboardQuery query);`

3. Implement service logic in `OrdersService` that:
- delegates grouped data retrieval to the repository
- maps repository results into a stable dashboard result object
- derives calculated fields such as:
  - `averageOrderValue`
  - `activePipelineOrders`
  - percentages and rates

### Repository changes

Add repository support for dashboard aggregation rather than materializing all rows into memory.

Recommended repository additions:
- aggregate total order count
- aggregate total revenue
- aggregate total item quantity / units
- grouped counts by status
- grouped counts and revenue by month
- latest N orders for the recent activity list
- min/max created dates for the default displayed range

Implementation guidance:
- use EF Core grouped queries against the orders table
- preserve the same self-vs-admin scoping pattern used by `GetOrdersAsync` and `GetAllOrdersAsync`
- keep ordering deterministic (`CreatedAt DESC`) for recent orders
- avoid making the dashboard depend on paged list endpoints internally

### Controller changes

Add a new action to `OrdersController`:
- `GET /orders/dashboard`

Recommended controller behavior:
- resolve current user ID
- determine whether the caller is admin-scoped
- build and pass a dashboard query object
- return a dedicated response payload

Keep controller behavior aligned with the existing conventions already used in:
- `GetOrders(...)`
- `GetAllOrders(...)`
- `GetOrderById(...)`

## Recommended Frontend Approach

### Add a dedicated dashboard API method

Extend `src/web-admin/lib/orders-api.ts` with:
- `getDashboard(): Promise<OrderDashboardResponse>`

Do not make the dashboard page stitch together multiple existing list endpoints as the primary solution.

That fallback is acceptable for prototyping only, but the production path should use the dedicated summary endpoint.

### Add dashboard types

Extend `src/web-admin/types/orders.ts` with dashboard-specific contracts:
- `OrderDashboardResponse`
- `OrderDashboardSummary`
- `OrderDashboardStatusBucket`
- `OrderDashboardTrendPoint`
- `OrderDashboardFulfillment`
- `OrderDashboardRecentOrder`

Keep these separate from `OrderListResponse` so the page remains explicit about what data it consumes.

### Add a dedicated dashboard hook

Create a new hook, for example:
- `src/web-admin/hooks/use-dashboard.ts`

Responsibilities:
- call `ordersApi.getDashboard()`
- manage loading/error/refetch state
- expose a UI-friendly shape for the dashboard page

Recommended behavior:
- fetch only after auth state is resolved
- align with the same loading conventions used by `useOrders` and `useOrder`
- preserve a single refetch action so the page can expose a refresh button

### Replace the placeholder dashboard page

Replace the current content in `src/web-admin/app/(authenticated)/dashboard/page.tsx` with a real dashboard page composed of dedicated sections.

Recommended page sections:

1. Header / hero strip
- title: `Orders Overview`
- short subtitle based on scope:
  - `All orders` for admin/global view
  - `Your orders` for user-scoped view
- quick actions:
  - `View Orders`
  - `New Order` when the user has create permission
  - `Refresh`

2. KPI card row
- `Total Orders`
- `Revenue`
- `Average Order Value`
- `Units Sold`
- `Active Pipeline`

3. Graph row
- status distribution graph
- monthly trend graph

4. Secondary information row
- fulfillment metrics card
- recent orders card or table

5. Empty / error handling
- a meaningful empty state if the scoped user has no orders
- retry affordance on network failure

## Graphing Plan

The dashboard should include graphs as a first-class requirement.

### Recommended graphs

1. Status distribution graph
- Purpose: show workload and completion mix across the six order statuses
- Recommended chart type:
  - donut chart, or
  - vertical bar chart if you want simpler responsive behavior

Recommended data source:
- `statusBreakdown`

2. Monthly trend graph
- Purpose: show volume and business movement over time
- Recommended chart type:
  - dual-series line chart, or
  - combined bar + line chart

Recommended series:
- `orderCount`
- `revenue`

Recommended data source:
- `monthlyTrend`

### Charting library recommendation

Recommended:
- add a lightweight charting library such as `recharts`

Why:
- fastest path to polished responsive charts
- avoids hand-rolled SVG logic for tooltips, legends, and scaling
- sufficient for two charts without dragging in a large visualization stack

Fallback if dependency changes are intentionally avoided:
- build simple SVG/CSS charts in-house

Tradeoff:
- possible, but slower to make polished and more maintenance-heavy

Recommendation:
- use `recharts` for v1 unless the team has a strong preference to stay dependency-free

### Chart UX rules

- keep color mapping aligned with status badges used elsewhere in the Orders UI
- show readable tooltips with count and currency values
- ensure charts collapse well on mobile
- do not overload the dashboard with more than two primary graphs in v1

## Visual / UX Direction

The page should look intentionally designed, not like a generic admin placeholder.

Recommended design direction:
- a stronger top header band with subtle layered background / gradient
- clear visual hierarchy between hero, KPIs, charts, and recent activity
- dense but readable spacing that works in the existing sidebar shell
- skeleton states shaped like the final UI, not generic gray blocks
- status colors reused consistently across cards, charts, and badges

Stay within the current stack:
- Next.js App Router
- Tailwind CSS
- existing shadcn-style primitives (`Card`, `Badge`, `Button`, `Skeleton`)

Do not:
- introduce unrelated design-system rewrites
- turn the page into a generic analytics product UI disconnected from the rest of the app

## Navigation Changes

Small but important navigation updates should ship with the dashboard.

Recommended updates in `src/web-admin/components/app-sidebar.tsx`:

1. Make the brand link navigate to `/dashboard`
2. Keep Dashboard as the clear first primary destination
3. Remove the hardcoded `index === 1` default-open behavior so the active route determines which group is open
4. Keep Orders as the main secondary workflow area

Do not add extra dashboard sub-pages in v1 unless a concrete need appears during implementation.

## Testing Plan

### Backend tests

Add tests that cover:
- regular users only see their own dashboard aggregates
- admin users see global aggregates
- summary math is correct for counts, totals, and rates
- month grouping is stable and ordered
- the recent orders list is sorted descending by `CreatedAt`
- empty datasets return a valid zero-state payload

### Frontend tests

Add unit coverage for:
- dashboard value formatting
- any local mapping / chart preparation helpers
- scope-specific copy and action visibility

Add Playwright coverage for:
- authenticated `/` redirects to `/dashboard`
- dashboard renders real cards instead of placeholder blocks
- charts render for seeded data
- recent orders link into `/orders/[id]`
- `New Order` action is only enabled when the user has `orders.write` or `orders.manage`

## Recommended Implementation Sequence

1. Define the backend dashboard response/result/query models.
2. Add repository aggregation methods and implement the grouped queries.
3. Add `GetDashboardAsync(...)` to the service layer.
4. Add `GET /orders/dashboard` to the controller and cover it with backend tests.
5. Extend frontend types and `ordersApi.getDashboard()`.
6. Add `useDashboard()` and any small formatting helpers.
7. Replace the dashboard placeholder page with the new layout.
8. Add the chart components and responsive polish.
9. Update sidebar polish so the dashboard clearly feels like the app home page.
10. Add frontend test coverage.

## Risks / Notes

1. Client-side aggregation from paged endpoints would work with the current 150 seeded orders, but it is the wrong long-term architecture.
2. The new endpoint should be purpose-built for dashboard reads; avoid leaking raw internal query structure into the frontend contract.
3. If a charting library is added, keep the dependency focused and avoid turning this into a broader visualization refactor.
4. The dashboard must respect existing authorization boundaries. The page is not a separate “user dashboard”; it is the orders dashboard rendered according to the caller's allowed scope.

## Recommended Output of This Feature

When this feature is complete, the logged-in landing experience should be:
- `/` redirects to `/dashboard`
- `/dashboard` immediately shows a polished orders overview
- the page includes real KPIs, two charts, and recent order activity
- the data comes from a single dedicated Orders dashboard endpoint
- admins see all-order aggregates, while normal users see only their scoped order data
