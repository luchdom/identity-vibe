# Sidebar Active Menu Highlighting Fix Plan

Date: 2026-02-25
Status: Planning (pre-implementation)
Related:
- `src/web-admin/components/app-sidebar.tsx`
- `src/web-admin/components/ui/sidebar.tsx`

## Problem Summary

The sidebar always highlights `All Orders` because the navigation config hardcodes:

- `url: "/orders"`
- `isActive: true`

and the render logic passes:

- `isActive={'isActive' in item ? item.isActive : false}`

There is no active-state calculation from the current pathname/search params.

## Root Cause

1. `AppSidebar` does not read route state (`usePathname`, `useSearchParams`).
2. Navigation items are treated as static config objects with a manual `isActive` flag.
3. `Orders` submenu items require query-aware matching (`/orders?status=0`, etc.), but matching logic does not exist.

## What should be fixed (recommended scope)

### 1. Replace hardcoded `isActive` flags with computed route matching
- Remove `isActive: true` from `All Orders` config.
- Compute active state at render time based on current route.

### 2. Make matching query-aware for orders filters
Recommended matching rules:
- `All Orders` (`/orders`) active when:
  - pathname is `/orders`
  - and `status` query param is missing
- `Pending Orders` active when:
  - pathname is `/orders`
  - and `status=0`
- `Shipped Orders` active when `status=3`
- `Cancelled Orders` active when `status=5`

### 3. Optionally open the correct sidebar group based on active child route
Current behavior uses `defaultOpen={index === 1}` (Orders group always initially open).
Recommended improvement:
- Open any group whose child route is active.
- Keep `Orders` open by default only when no route match exists (optional fallback).

### 4. Keep highlighting logic generic enough for non-orders routes
- Support exact path matching (`/dashboard`, `/users/roles`)
- Support query matching when provided
- Avoid overmatching unrelated routes (e.g., `/orders/123` should not accidentally look like `All Orders`, unless explicitly desired)

## Recommended implementation approach

### A. Add route-aware helpers in `AppSidebar`
Use Next.js navigation hooks:
- `usePathname()`
- `useSearchParams()`

Add helper(s), for example:
- `parseNavUrl(url)` -> pathname + query
- `isNavItemActive(itemUrl, currentPathname, currentSearchParams)`
- `groupHasActiveItem(groupItems, ...)`

### B. Extend nav item type shape (internal only)
Keep current config mostly unchanged, but support computed matching.
Optional small enhancement:
- add `matchMode?: 'exact' | 'prefix'`
- add `matchQuery?: boolean` (default true when URL has query string)

For this bug, simplest path is to parse and compare directly without introducing new config flags.

### C. Render `SidebarMenuSubButton` with computed `isActive`
Replace:
- `isActive={'isActive' in item ? item.isActive : false}`

With:
- `isActive={isNavItemActive(item.url)}`

### D. Improve parent `Collapsible` open behavior (recommended)
Replace static `defaultOpen={index === 1}` with route-aware default/open state:
- `defaultOpen={groupHasActiveItem(item.items) || index === 1}` (minimal)

If needed later, switch to controlled state to preserve manual open/close interaction across route changes.

## Edge cases to decide (open questions)

### Q1. Orders detail route behavior (`/orders/{id}`)
Should `All Orders` be highlighted when viewing an order detail page like `/orders/123`?

Options:
1. **No** (Recommended for clarity)
- Only highlight exact `/orders` list routes
- Detail pages show no selected order filter

2. **Yes**
- Treat `/orders/*` as part of `All Orders`
- Requires prefix matching for `All Orders`

Recommendation: **Option 1** (exact list route only)

Decision (user-confirmed):
- **Option 1**: Do not highlight `All Orders` on order detail routes like `/orders/123`.

### Q2. Extra query params on orders list
If the page later adds other query params (e.g., `/orders?status=0&page=2`), should the menu still highlight `Pending Orders`?

Options:
1. **Yes** (Recommended)
- Match only relevant query keys (`status`) and ignore unrelated keys

2. **No**
- Require exact query equality (fragile)

Recommendation: **Option 1**

## Tests to add/update (recommended)

### Playwright (`src/web-admin/tests/...`)
Add a sidebar navigation highlighting test (or extend existing orders page tests):

Cases:
1. `/orders` highlights `All Orders`
2. `/orders?status=0` highlights `Pending Orders` (not `All Orders`)
3. `/orders?status=3` highlights `Shipped Orders`
4. `/orders?status=5` highlights `Cancelled Orders`
5. `/users/roles` highlights `Roles & Permissions`

Assertions:
- active menu item has the active styling state/attribute from `SidebarMenuSubButton`
- only one item in the group is active at a time

### Optional unit/helper test (if route matcher is extracted)
If you extract matching into a pure helper (`src/web-admin/lib/nav-matching.ts`), add a small test for:
- exact path match
- query-aware status match
- ignored extra query params
- non-match on detail routes (if Q1 = No)

## File-by-file implementation checklist (proposed)

1. `src/web-admin/components/app-sidebar.tsx`
- add `usePathname` / `useSearchParams`
- add route-matching helpers
- remove hardcoded `isActive` usage
- compute child `isActive` dynamically
- compute group `defaultOpen` from active child (recommended)

2. `src/web-admin/tests/orders-page.spec.ts` (or new dedicated sidebar spec)
- add/extend active-menu assertions for status routes

3. `docs/FRONTEND.md` (optional)
- brief note that sidebar active state is route/query-driven (only if documenting nav conventions matters)

## Risk / impact

Low risk:
- Frontend-only visual behavior change
- No backend impact

Main implementation risk:
- Matching too broadly and highlighting multiple items (avoid by exact path + targeted query matching)

## Acceptance criteria

- `All Orders` is only highlighted on `/orders` with no `status` filter
- Filtered order routes highlight the matching submenu item
- `Roles & Permissions` highlights on `/users/roles`
- Sidebar does not hardcode any order submenu `isActive` values
- Existing sidebar expand/collapse behavior remains usable
