# Orders Navigation + In-Page Filters + New Order (Implementation Checklist)

Date: 2026-02-25
Status: Tasked (pre-implementation)
Related:
- `ai-analysis/2026-02-25-orders-navigation-filters-new-order-plan.md`
- `src/web-admin/components/app-sidebar.tsx`
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx`
- `src/web-admin/lib/orders-api.ts`

## Confirmed product decisions

- Sidebar uses a single Orders menu entry (remove status submenu items).
- Orders menu should be highlighted for all `/orders*` routes (`/orders`, `/orders/new`, `/orders/{id}`).
- Orders list page uses in-page filters (URL-driven query params).
- `New Order` button is shown disabled (with message/tooltip) if user lacks `orders.write` and `orders.manage`.
- `/orders/new` is scope-gated by `orders.write` or `orders.manage`.
- Successful create redirects to `/orders` list with a success toast.

## Scope summary

Frontend-only primary feature work (web-admin), plus tests/docs updates.

Backend `Orders` API changes are **not required** for v1 because:
- `POST /orders` already exists
- endpoint is already protected by `orders.write`

## Implementation checklist

### A. Sidebar navigation cleanup and route-aware active highlighting

#### A1. Simplify Orders navigation menu structure
Files:
- `src/web-admin/components/app-sidebar.tsx`

Tasks:
- Remove Orders status submenu entries:
  - `Pending Orders`
  - `Shipped Orders`
  - `Cancelled Orders`
- Keep a single Orders child menu item (e.g. `All Orders` -> `/orders`)
- Remove hardcoded `isActive: true` from nav config

Acceptance checks:
- Sidebar no longer duplicates status filtering in navigation
- No hardcoded `isActive` flags remain for Orders

#### A2. Add route-aware active matching for sidebar submenu items
Files:
- `src/web-admin/components/app-sidebar.tsx`
- optional helper (recommended if extracted): `src/web-admin/lib/nav-matching.ts`

Tasks:
- Use Next.js routing hooks in sidebar:
  - `usePathname()`
  - `useSearchParams()` (optional if matcher stays path-only for v1)
- Compute submenu item `isActive` at render time
- Ensure exact/feature matching rules:
  - Orders menu item active for any `/orders*` route
  - Other menu items still use exact route matching by default (unless a prefix rule is explicitly needed)
- Keep matching deterministic (only one item active per group when possible)

Acceptance checks:
- Orders menu item highlights on:
  - `/orders`
  - `/orders?status=...`
  - `/orders/new`
  - `/orders/123`
- Non-orders routes do not incorrectly highlight Orders

#### A3. Improve sidebar group open behavior (recommended)
Files:
- `src/web-admin/components/app-sidebar.tsx`

Tasks:
- Replace static `defaultOpen={index === 1}` with route-aware default open:
  - open group if any child is active
  - optionally preserve existing fallback open behavior for Orders if no route matches

Acceptance checks:
- Navigating to `/orders*` expands Orders group automatically
- Navigating to `/users/roles` expands User Management group automatically (if route-aware logic is generic)

### B. Orders list page: in-page filter controls (URL-driven)

#### B1. Add status filter UI in Orders page header
Files:
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- optional new component: `src/web-admin/components/orders/orders-filters.tsx`

Tasks:
- Add status filter control(s) to the Orders page:
  - `All`
  - `Confirmed`
  - `Processing`
  - `Shipped`
  - `Delivered`
  - `Cancelled`
  - optional `Draft`
- Keep current heading/count behavior tied to selected status
- Make filters mobile-friendly (wrap or dropdown)

Recommended UI primitives:
- `Select` (existing) OR buttons/chips using `Button`

Acceptance checks:
- User can change status filter without sidebar navigation
- Selected filter is visible in the page UI

#### B2. Bind filters to URL query params
Files:
- `src/web-admin/app/(authenticated)/orders/page.tsx`

Tasks:
- Continue using `useSearchParams()` as source of truth
- Update query params when filter changes via `router.push(...)` or `router.replace(...)`
- Reset `page` query param to `1` on status filter change
- Preserve unrelated future params where sensible

Acceptance checks:
- Filter changes update the URL (`/orders?status=...`)
- Refreshing the page preserves selected filter
- Browser back/forward works with filter changes

### C. Orders page `New Order` CTA (scope-based UX gating)

#### C1. Add create permission helper usage
Files:
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- optional helper: `src/web-admin/lib/authz.ts`

Tasks:
- Compute `canCreateOrder` from auth scopes:
  - true if user has `orders.write` OR `orders.manage`
- Do not rely on `ordersApi.isAdmin()` for create permission
- Reuse `AuthGuard`/scope helper patterns already in repo

Acceptance checks:
- Create permission logic is explicitly scope-based

#### C2. Add `New Order` button with disabled state + explanation
Files:
- `src/web-admin/app/(authenticated)/orders/page.tsx`

Tasks:
- Add `New Order` button in header actions near `Refresh`
- If authorized:
  - enabled
  - navigates to `/orders/new`
- If unauthorized:
  - disabled
  - show message/tooltip/inline explanation (v1 acceptable if simple)

Acceptance checks:
- Users without create scopes see disabled CTA (not hidden)
- Users with create scopes can navigate to `/orders/new`

### D. New Order page (`/orders/new`) and create form

#### D1. Add new route page shell
Files (new):
- `src/web-admin/app/(authenticated)/orders/new/page.tsx`

Tasks:
- Add page shell with:
  - breadcrumbs (`Orders Management` -> `New Order`)
  - back link/button to `/orders`
  - page title + description
- Protect page with `AuthGuard` using scopes:
  - `requiredScopes={['orders.write', 'orders.manage']}`

Acceptance checks:
- Direct navigation to `/orders/new` requires create scopes
- Unauthorized users are redirected/blocked by `AuthGuard`

#### D2. Implement New Order form (v1)
Files (new recommended):
- `src/web-admin/components/orders/new-order-form.tsx`
- optional split components:
  - `src/web-admin/components/orders/order-customer-fields.tsx`
  - `src/web-admin/components/orders/order-items-editor.tsx`
  - `src/web-admin/components/orders/order-shipping-fields.tsx`

Tasks:
- Build form for `CreateOrderRequest` using existing types in `src/web-admin/types/orders.ts`
- Customer section:
  - required: `name`, `email`
  - optional: `phone`, address fields
- Items section:
  - dynamic rows
  - at least one item required
  - required fields: `productName`, `quantity`, `unitPrice`
  - optional: `sku`, `description`
- Optional shipping section (toggle on/off)
- Optional fields:
  - `paymentMethod`
  - `specialInstructions`
- Client validation (recommended `react-hook-form` + `zod`, but local state acceptable if faster)

Acceptance checks:
- Valid payload can be constructed from the UI
- Invalid form shows inline validation and blocks submit

#### D3. Submit create order and redirect to list
Files:
- `src/web-admin/components/orders/new-order-form.tsx`
- `src/web-admin/app/(authenticated)/orders/new/page.tsx`
- `src/web-admin/lib/orders-api.ts` (reuse existing `createOrder`)

Tasks:
- Submit via `ordersApi.createOrder(...)`
- Handle loading/error state
- On success:
  - show success toast
  - redirect to `/orders` (confirmed decision)
- Optional: preserve user context with a query param / flash message later (not required v1)

Acceptance checks:
- Successful submit calls existing Orders API and returns to `/orders`
- Failure shows useful error message and keeps form state

### E. Orders page / detail page navigation consistency

#### E1. Keep Orders feature routes consistent with sidebar highlighting
Files:
- `src/web-admin/components/app-sidebar.tsx`
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx`
- `src/web-admin/app/(authenticated)/orders/new/page.tsx`

Tasks:
- Ensure breadcrumbs/links consistently use `/orders`
- Ensure sidebar active matcher treats `/orders/new` and `/orders/{id}` as Orders feature routes

Acceptance checks:
- Orders menu stays highlighted across list/detail/new pages

### F. Frontend tests (Playwright)

#### F1. Update Orders page Playwright tests for new navigation/filter UX
Files:
- `src/web-admin/tests/orders-page.spec.ts`

Tasks:
- Update sidebar assertions to match single Orders menu item (remove submenu-status assumptions)
- Add assertions for in-page status filter UI
- Add assertions for `New Order` button:
  - enabled when `grantedScopes` includes `orders.write` or `orders.manage`
  - disabled with message/tooltip when missing scopes (can be a separate test fixture)

Acceptance checks:
- Orders page tests reflect new UX and pass with mocks

#### F2. Add Playwright test for `/orders/new` create flow (mocked API)
Files (new):
- `src/web-admin/tests/new-order-page.spec.ts`

Tasks:
- Mock auth userinfo + token scopes (`grantedScopes`)
- Mock `POST /orders`
- Cover:
  - authorized access to `/orders/new`
  - validation blocks submit when required fields missing
  - successful submit redirects to `/orders` and shows success toast
  - unauthorized user cannot use the page (redirect or access denied)

Acceptance checks:
- New order UX flow is regression-tested without live backend

### G. Docs updates

#### G1. Update frontend/dev docs for Orders UX changes
Files:
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- optional `README.md`

Tasks:
- Document single Orders navigation entry (filters moved into page)
- Document `/orders/new` route and required scopes (`orders.write` or `orders.manage`)
- Mention disabled create CTA behavior when scope is missing
- Note list filters are URL-driven via query params

Acceptance checks:
- Docs reflect implemented navigation and route behavior

### H. Validation

#### H1. Frontend compile/type validation
Tasks:
- Run `npx tsc --noEmit` in `src/web-admin`
- Run `npm run build` if/when pre-existing lint issues are handled (optional in this pass)

Acceptance checks:
- New changes are type-safe

#### H2. Manual UI smoke validation
Tasks:
- Verify sidebar shows single Orders entry
- Verify Orders menu highlight on:
  - `/orders`
  - `/orders?status=3`
  - `/orders/new`
  - `/orders/{id}`
- Verify in-page status filters update URL and data
- Verify `New Order` button disabled for no-create-scope user and enabled for create-capable user
- Verify successful create redirects to `/orders` with success toast

Acceptance checks:
- UX behavior matches confirmed product decisions

## Recommended implementation order

1. A (sidebar simplification + active matching)
2. B (in-page status filters)
3. C (scope-gated `New Order` CTA)
4. D (new order page + create form + redirect)
5. F (Playwright updates/new tests)
6. G (docs)
7. H (validation)

## Notes / guardrails

- Do not reintroduce role-based create gating; use scopes (`orders.write` / `orders.manage`) only.
- Backend `POST /orders` authorization already exists and remains authoritative.
- Keep status filter URL-driven; do not move it to local-only state.
- Keep v1 create form simple and explicit; avoid scope creep into product search/autocomplete.

## Final Validation Checklist (done criteria)

- [x] Sidebar Orders submenu statuses removed
- [x] Sidebar active highlighting is route-driven (no hardcoded `isActive`)
- [x] Orders menu highlights for all `/orders*` routes
- [x] Orders page has in-page status filter controls bound to URL
- [x] `New Order` button is scope-gated and disabled with explanation when unauthorized
- [x] `/orders/new` page exists and is scope-protected
- [x] Successful order create redirects to `/orders` with success toast
- [ ] Orders page Playwright tests updated and passing
- [ ] New order page Playwright test added and passing
- [x] Docs updated

## Implementation Log (to update while coding)

- 2026-02-25: Simplified sidebar Orders navigation to a single submenu item (`/orders`) and removed hardcoded status submenu links.
- 2026-02-25: Implemented route-aware sidebar active highlighting (Orders highlighted across `/orders`, `/orders/new`, `/orders/{id}`) in `src/web-admin/components/app-sidebar.tsx`.
- 2026-02-25: Added in-page URL-driven status filters to `src/web-admin/app/(authenticated)/orders/page.tsx`.
- 2026-02-25: Added scope-gated `New Order` CTA to Orders page with disabled state + tooltip when missing `orders.write` / `orders.manage`.
- 2026-02-25: Added `/orders/new` page and `NewOrderForm` with create flow using existing `ordersApi.createOrder`, redirecting to `/orders?created=1` and showing success toast.
- 2026-02-25: Added Orders page success-toast handling for `created=1` query param and URL cleanup.
- 2026-02-25: Updated Playwright Orders page tests for status filter UI, create-CTA behavior, and sidebar active-state assertions.
- 2026-02-25: Added Playwright spec `src/web-admin/tests/new-order-page.spec.ts` covering validation, create redirect, unauthorized access, and sidebar highlight.
- 2026-02-25: Updated `docs/FRONTEND.md` and `docs/DEVELOPMENT.md` for the new Orders navigation/filter/create flow.
- Validation run: `npx tsc --noEmit` in `src/web-admin` -> Passed.
- Not run in this session: Playwright specs (`orders-page.spec.ts`, `new-order-page.spec.ts`) and manual UI smoke validation.
