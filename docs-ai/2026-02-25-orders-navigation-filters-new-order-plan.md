# Orders Navigation Simplification + In-Page Filters + New Order Page Plan

Date: 2026-02-25
Status: Planning (pre-implementation)
Related:
- `src/web-admin/components/app-sidebar.tsx`
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx`
- `src/web-admin/lib/orders-api.ts`
- `src/web-admin/components/auth/AuthGuard.tsx`
- `src/backend/Orders/Controllers/OrdersController.cs`

## Summary of Requested Changes

1. Simplify sidebar navigation for Orders:
- remove status-specific submenu entries (`Pending`, `Shipped`, `Cancelled`)
- keep a single Orders page/menu entry

2. Move order status selection/filtering into the Orders list page UI:
- a single `/orders` page with filters (status, later more filters if needed)

3. Add a `New Order` action and dedicated page:
- route: `GET /orders/new` (frontend page)
- action enabled only when user has `orders.write` or `orders.manage` in granted scopes
- page posts to existing `POST /orders` backend endpoint

## Current Codebase Findings

### Frontend sidebar
- `src/web-admin/components/app-sidebar.tsx` currently has a submenu under `Orders Management` with hardcoded status links.
- `All Orders` is incorrectly hardcoded with `isActive: true`.
- Sidebar active state is not route-aware yet.

### Orders list page (already a strong base)
- `src/web-admin/app/(authenticated)/orders/page.tsx` already reads `status` and `page` from URL query params via `useSearchParams()`.
- It already renders different headings based on `status`.
- It already calls `useOrdersWithStatus(status, page, pageSize)`.

Implication:
- The page is already close to the requested “single page with filters” model.
- Main work is UI controls + navigation cleanup + create flow.

### Backend Orders API
- `POST /orders` already exists and is protected by `[Authorize(Policy = "orders.write")]`.
- `GET /orders` and `GET /orders/all` already support `status` query filtering.

Implication:
- No backend Orders API changes are required for v1 create/list-filter support.
- Frontend scope gating is UX; backend remains authoritative.

## Recommended UX / Architecture Approach

### 1. Sidebar: single Orders entry (no status submenu)
Recommended change:
- Replace the status submenu items with just one child item:
  - `All Orders` -> `/orders`
- (Optional) rename `Orders Management` parent to `Orders` for cleaner IA.

Why:
- Avoid duplicate navigation states when filter UI exists in-page.
- Keeps deep-linking via query params (`/orders?status=3`) while avoiding menu clutter.

### 2. Orders page filters should remain URL-driven
Recommended behavior:
- Keep filter state mirrored in query params (`status`, `page`, later `sort`, search)
- UI controls update URL -> page reacts via existing `useSearchParams`

Why:
- Deep links remain shareable/bookmarkable
- Browser back/forward works naturally
- Sidebar active matching becomes simpler (only `/orders` list entry)

### 3. New Order flow with dedicated page (`/orders/new`)
Recommended behavior:
- Add a `New Order` button on `/orders`
- Button visible/active only if user has `orders.write` or `orders.manage`
- Route to `/orders/new`
- New page contains a create form and submits via `ordersApi.createOrder(...)`
- On success, redirect to `/orders/{id}` (recommended) or `/orders`

Why dedicated page (vs modal):
- Create order payload is non-trivial (`customer`, `items[]`, optional shipping/payment)
- Better for validation, future draft-save, and mobile layout

## Detailed Implementation Plan

### A. Sidebar navigation simplification and active-state fix (frontend)
Files:
- `src/web-admin/components/app-sidebar.tsx`

Tasks:
1. Remove hardcoded `isActive: true` from `All Orders` nav item.
2. Remove status submenu entries (`Pending`, `Shipped`, `Cancelled`) from sidebar config.
3. Implement route-aware active state (pathname + query aware as needed), but with simplified matching since Orders is now one item.
4. Ensure `/orders` is highlighted for the Orders menu item.
5. Confirm `/orders/new` and `/orders/{id}` behavior for menu highlight (see open questions below).

Recommended route matching rules (initial):
- `/orders` list routes (including query params) => highlight Orders menu item
- `/orders/new` => also highlight Orders menu item (same feature area)
- `/orders/{id}` => also highlight Orders menu item (same feature area)

Note:
- This supersedes the previous status-submenu active matching problem for most cases.

### B. Orders list page: add filter controls in-page
Files:
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- optional new component: `src/web-admin/components/orders/orders-filters.tsx`

Tasks:
1. Add status filter UI (select/dropdown or tabs/chips) inside the page header.
2. Bind filter UI to query param `status` using `useRouter()` + `useSearchParams()`.
3. Reset `page=1` when status filter changes.
4. Keep refresh behavior intact.
5. Keep headings/counts based on current status filter.
6. Optionally add a clear filter action (`All`).

Recommended UI (v1):
- `Select` or segmented buttons/chips for status
- Options: `All`, `Confirmed`, `Processing`, `Shipped`, `Delivered`, `Cancelled` (and optionally `Draft`)

### C. Orders list page: add `New Order` CTA (scope-gated)
Files:
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- `src/web-admin/contexts/auth-context.tsx` (already exposes `scopes`)
- optional helper: `src/web-admin/lib/authz.ts`

Tasks:
1. Compute `canCreateOrder = scopes includes orders.write or orders.manage`.
2. Add `New Order` button in page header actions.
3. If allowed, clicking navigates to `/orders/new`.
4. If not allowed:
   - preferred v1: hide button entirely (or disable with tooltip; see open question).

Note:
- Backend `POST /orders` still enforces `orders.write`, so this is UX gating only.

### D. Add New Order page (`/orders/new`) with create form
Files (new):
- `src/web-admin/app/(authenticated)/orders/new/page.tsx`
- `src/web-admin/components/orders/new-order-form.tsx` (recommended)
- optional helper components:
  - `src/web-admin/components/orders/order-customer-fields.tsx`
  - `src/web-admin/components/orders/order-items-editor.tsx`
  - `src/web-admin/components/orders/order-shipping-fields.tsx`

Tasks:
1. Add route page shell with breadcrumbs + back to orders list.
2. Protect page with `AuthGuard requiredScopes={['orders.write', 'orders.manage']}`.
3. Build v1 form for `CreateOrderRequest`:
   - Customer (name, email required; phone optional)
   - Items[] (at least one item; productName, quantity, unitPrice required; sku/description optional)
   - Shipping optional (toggle section)
   - Payment method optional
   - Special instructions optional
4. Client-side validation (recommended `zod` + `react-hook-form`, but local state acceptable if faster).
5. Submit via `ordersApi.createOrder`.
6. Loading/error/success states.
7. On success, redirect to `/orders/{id}` and show toast.

Recommended v1 constraints:
- Keep form simple and explicit (no dynamic product search/autocomplete yet)
- Manual item rows with add/remove buttons
- No draft autosave in v1

### E. Shared frontend auth/scope helpers (optional cleanup, recommended)
Files:
- `src/web-admin/components/auth/AuthGuard.tsx`
- optional `src/web-admin/lib/authz.ts`

Tasks:
1. Add/reuse helper `hasAnyScopes(scopes, ['orders.write', 'orders.manage'])` in a shared place.
2. Use the same helper for:
   - Orders list page create CTA
-  New order page route guard
   - Future action buttons

Note:
- `AuthGuard` already supports `requiredScopes`, so this may be minimal.

### F. Tests

#### F1. Update sidebar/orders page Playwright tests
Files:
- `src/web-admin/tests/orders-page.spec.ts`

Tasks:
1. Remove assertions that depend on status submenu entries in sidebar (`Pending Orders`, etc.) if any are added later.
2. Add assertions for:
   - Orders page shows in-page status filter UI
   - `New Order` button visible when `grantedScopes` contains `orders.write` or `orders.manage`
3. Add negative case (optional in same spec or separate):
   - `New Order` button hidden/disabled when user lacks both scopes

#### F2. Add Playwright test for new order page (mocked API) (recommended)
Files (new):
- `src/web-admin/tests/new-order-page.spec.ts`

Cases:
1. Authorized user can open `/orders/new`
2. Form validation blocks submit with missing required fields
3. Successful submit calls `POST /orders` mock and redirects to `/orders/{id}`
4. Unauthorized user is redirected/blocked by `AuthGuard` (scope-based)

#### F3. Optional helper tests
If extracting filter/query helpers or form mappers, add unit tests for:
- status query parsing/serialization
- create-order payload mapping

### G. Docs updates (recommended)
Files:
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- optionally `README.md`

Tasks:
1. Document Orders navigation simplification (single Orders menu item).
2. Document list filters are URL-driven via query params.
3. Document `/orders/new` route and required scopes (`orders.write` or `orders.manage`).
4. Note backend `POST /orders` remains authoritative for authorization.

## Recommended implementation order

1. Sidebar nav simplification + route-aware active highlighting
2. Orders list in-page status filter UI (URL-driven)
3. `New Order` CTA (scope-gated) on list page
4. `/orders/new` page + form submit flow
5. Playwright updates/new tests
6. Docs updates

## Risks / Notes

- `ordersApi.isAdmin()` currently mixes roles + scopes; new create permission should use explicit scopes (`orders.write`/`orders.manage`) not admin heuristic.
- Form scope can expand quickly; keep v1 create page intentionally simple.
- If sidebar active matching is made generic, avoid hardcoding one-off logic tied only to Orders.

## Open Questions (to confirm before implementation)

### Q1. Sidebar highlight for Orders feature routes
Should the single Orders menu item be highlighted on all Orders feature routes (`/orders`, `/orders/new`, `/orders/{id}`)?

Options:
1. **Yes (Recommended)**
- Treat all `/orders*` routes as the Orders feature area
- Best UX consistency

2. **Only `/orders` list route**
- More strict
- Detail/create pages show no active Orders menu item

Recommendation: **Option 1**

Decision (user-confirmed):
- **Option 2**: Redirect to `/orders` list with a success toast after successful order creation.

Decision (user-confirmed):
- **Option 2**: Show a disabled `New Order` button with a message/tooltip when the user lacks `orders.write`/`orders.manage`.

Decision (user-confirmed):
- **Option 1**: Highlight the single Orders menu item for all Orders feature routes (`/orders*`), including list, create, and detail pages.

### Q2. `New Order` button when user lacks permission
How should the list page handle users without `orders.write`/`orders.manage`?

Options:
1. **Hide the button (Recommended v1)**
- Cleaner UI
- Fewer disabled-action questions

2. **Show disabled button + tooltip/message**
- More discoverable permissions model
- Slightly more UI work

Recommendation: **Option 1**

### Q3. New order submit redirect target
After successful order creation, where should we redirect?

Options:
1. **Go to order detail page `/orders/{id}` (Recommended)**
- Immediate confirmation and next actions

2. **Go back to `/orders` list with success toast**
- Simpler flow

Recommendation: **Option 1**

## Acceptance Criteria

- Sidebar no longer contains status submenu items for Orders
- Orders menu highlight is route-driven (not hardcoded)
- Orders list page has in-page status filter controls bound to URL query params
- `New Order` button appears only for users with `orders.write` or `orders.manage`
- `/orders/new` is scope-protected and can create an order via existing API
- Successful create redirects as designed
- Playwright tests cover updated orders nav/list behavior and new-order flow
