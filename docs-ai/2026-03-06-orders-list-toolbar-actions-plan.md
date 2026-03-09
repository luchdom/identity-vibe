# Orders List Toolbar + Row Actions Plan

Date: 2026-03-06

Related:
- `docs-ai/DESIGN-orders-list-toolbar-actions-2026-03-06.md`
- `src/web-app/app/(dashboard)/orders/page.tsx`
- `src/web-app/app/(dashboard)/users/page.tsx`
- `src/web-app/tests/e2e/loading-ux-orders.spec.ts`
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`

## 1. Overview

### Problem statement

The current `src/web-app` Orders list page has three UX issues relative to the requested direction:

- it exposes a top-level `Refresh` action away from the list surface
- it uses a tab row with count badges for status filtering
- it duplicates row entry points by showing both an inline eye icon and a 3-dot overflow menu

This creates more visual layers than necessary for a list page and does not align with the nearby Users-page pattern where list controls are grouped directly above the table.

### Goals

- Remove the top `Refresh` button from the Orders page.
- Move list-changing controls into the panel area directly above the table.
- Replace status tabs and counts with a compact toolbar-style status filter.
- Remove the inline eye icon row action.
- Keep `View details` inside the 3-dot overflow menu.
- Preserve current list behavior, URL-driven filtering, loading UX, permissions, pagination, and cancel-order action behavior.
- Update docs where current guidance conflicts with the new Orders-page pattern.

### Non-goals

- No backend API changes.
- No new Orders edit flow, route, or dialog in this pass.
- No new search or sort behavior for Orders unless separately requested.
- No change to Orders detail page or new-order page behavior.

## 2. Assumptions & Constraints

### Known constraints

- The target frontend is `src/web-app`, not `src/web-admin`.
  - Reason: the checked-in `web-app` Orders page matches the requested UI elements exactly: top refresh, status tabs with counts, and an inline eye icon.
- The Orders list is currently URL-driven through `status`, `page`, and `pageSize`.
- `OrderStatus` includes `Draft`, but the current Orders page filter model omits it.
- The current Orders page computes status counts by making extra requests in `loadStatusCounts`.
- The page already follows the loading UX contract:
  - first-load vs refetch separation
  - inline retry
  - delayed slow-load hint
  - route transition feedback
- There is no existing checked-in Orders edit flow in `src/web-app`.

### Explicit assumptions

- The requested “remove the Refresh button on top” means remove that header action, not replace it with another header-level reload control.
- The status filter should become a normal list filter and does not need counts.
- Orders-page changes should not force a global removal of status tabs from other list pages such as Users.

### Clarification decisions

- 2026-03-06: Orders row actions will use `3 dots only`.
  - Remove the inline eye icon.
  - Keep `View details` in the overflow menu.
  - Do not add a new Orders edit action in this pass.
- 2026-03-06: Manual refresh stays available, but moves from the page header into the table toolbar.
- 2026-03-06: The new Orders status filter will include `Draft`.
- 2026-03-06: The new Orders status control will be a compact dropdown/select.
- 2026-03-06: Shared frontend docs should be broadened to approve both status tabs and toolbar status filters, depending on page needs.

## 3. Architecture / Approach

### Proposed solution

Refactor the Orders list page so it follows a simpler two-layer structure:

1. Page header:
- title/context
- `New order` primary CTA

2. Main list surface (`SurfacePanel`):
- compact toolbar with status filter and nearby reset affordance
- list feedback states
- table
- pagination

### UX approach for status

Recommended status control:
- use a compact `TextField select` or `Select`-backed toolbar control, matching the Users-page toolbar style
- default display text: `Status`
- menu options:
  - `All statuses`
  - `Draft`
  - `Confirmed`
  - `Processing`
  - `Shipped`
  - `Delivered`
  - `Cancelled`

Why this is the best fit:
- it keeps status in the same conceptual layer as other list filters
- it removes the second top-of-panel navigation strip
- it scales better than tabs now that counts are intentionally removed
- it handles the extra `Draft` state without crowding the layout
- it matches the current Users-page filter placement and sizing

### Row action approach

Refactor the table action column to use overflow-only actions for this pass:

- remove the inline eye icon
- keep the vertical 3-dot button
- keep `View details` as the first menu item
- keep `Cancel order` as a secondary/destructive menu item when allowed

This is the safest approach because:
- it exactly satisfies the requested removal of the eye icon
- it avoids adding an undefined Orders edit experience
- it reduces duplicate row affordances and focus targets

### Control flow impact

- `status` filter changes continue to update the URL
- `page` resets to `1` on status change and clear
- list refetches remain driven by the existing `loadOrders` function
- `loadStatusCounts` and the related cache path can be removed

## 4. API / Contracts

### Frontend URL/query contract

Keep:
- `status`
- `page`
- `pageSize`

Behavior:
- no selected status => remove `status` from the URL
- changing status => set/remove `status`, then set `page=1`
- clearing filters => remove `status`, set `page=1`

### Backend/API contract

No backend change required.

Existing Orders list requests already support:
- `GET /orders`
- `GET /orders/all`
- optional `status`
- paging via `page` and `pageSize`

### Error handling

Preserve:
- inline `Alert` with `Retry`
- snackbar feedback for mutations
- stale-request cancellation handling

### Authn/authz expectations

Keep:
- `New order` scope gating via `canCreateOrder`
- cancel behavior and current permission checks
- all existing auth and route-transition behavior

## 5. Data Model & Storage

No schema or storage changes.

Client-side state changes:
- remove `statusCounts`
- remove `loadStatusCounts`
- remove count-cache reads/writes tied to the Orders list page

## 6. Implementation Steps

### Step 1. Refactor Orders header actions

Files:
- `src/web-app/app/(dashboard)/orders/page.tsx`

Changes:
- remove the top `Refresh` button from the header action group
- keep `New order` as the only header CTA
- keep the heading/count text unless you want a later visual polish pass

### Step 2. Replace status tabs with a compact toolbar filter

Files:
- `src/web-app/app/(dashboard)/orders/page.tsx`

Changes:
- remove `Tabs`, `Tab`, and `AppLabel` usage from the Orders page
- add a toolbar block inside `SurfacePanel`, above alerts/progress/table
- add a compact status select with placeholder-style empty display
- add a nearby `Clear` button that resets the filter state
- include `Draft` in the new dropdown unless clarified otherwise

Recommended toolbar layout:
- left cluster:
  - status select
- right cluster:
  - `Clear`
  - `Refresh`
  - future list actions only if they directly affect the table

### Step 3. Remove status-count loading logic

Files:
- `src/web-app/app/(dashboard)/orders/page.tsx`
- optionally `src/web-app/lib/cache/query-keys.ts` if Orders status-count keys become fully unused after cleanup

Changes:
- remove `statusCounts` state
- remove `countsAbortRef` and `countsRequestIdRef`
- remove `loadStatusCounts`
- remove related effects and invalidation calls that only exist to support count badges

Note:
- check whether status-count cache keys are still used by the detail/new-order flows before deleting shared helpers

### Step 4. Simplify table row actions

Files:
- `src/web-app/app/(dashboard)/orders/page.tsx`

Changes:
- remove the inline eye `IconButton`
- keep the 3-dot `IconButton`
- keep `View details` in the overflow menu
- preserve prefetch/navigation behavior for `View details` through the menu action or move prefetch to row hover only

### Step 5. Reconcile empty-state copy and filter reset behavior

Files:
- `src/web-app/app/(dashboard)/orders/page.tsx`

Changes:
- update empty-state wording so it references the toolbar filter rather than top tabs
- ensure `Clear` returns the list to its default state cleanly
- ensure refetch progress still appears during filter changes

### Step 6. Update automated coverage

Files:
- `src/web-app/tests/e2e/loading-ux-orders.spec.ts`
- recommended new file: `src/web-app/tests/e2e/orders-page.spec.ts`

Coverage to add/update:
- Orders page does not render a `Refresh` button in the header
- Orders page does not render status tabs/count badges
- Orders page renders a compact status filter near the table
- selecting a status updates the URL and refetches the list
- clearing the filter removes `status` and resets pagination
- rows no longer show an inline eye icon
- row overflow menu still exposes `View details`

Recommended test split:
- keep `loading-ux-orders.spec.ts` focused on loading/refetch behavior
- add a dedicated page-structure/actions spec for the new toolbar and row-action expectations

### Step 7. Update docs

Files:
- `docs/DEVELOPMENT.md`
- `docs/FRONTEND.md`
- `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`

Recommended doc changes:
- adjust the global language that currently implies status tabs are always the required list-filter pattern
- document that both status tabs and compact toolbar filters are approved list-filter patterns
- document that compact toolbar filters are an approved alternative when:
  - counts are not needed
  - status should behave like a standard list filter
  - a denser table-first layout is preferred
- add an Orders-page note that status is now a toolbar filter and row actions are overflow-first

## 7. Testing Strategy

### Automated tests

- Update or add Playwright coverage in `src/web-app/tests/e2e`
- Validate URL query updates for status selection and clear
- Validate action menu behavior after removing the eye icon
- Validate no regression to refetch/loading indicators during filter-driven reloads

### Manual validation

1. Open `/orders`
2. Confirm the header shows `New order` but no top-level `Refresh`
3. Confirm the toolbar above the table shows status, `Clear`, and `Refresh`
4. Select each status and confirm URL + results update
5. Confirm `Clear` removes the status filter and resets page 1
6. Confirm toolbar `Refresh` reloads the current list state
7. Open a row menu and confirm `View details` is present
8. Confirm cancel-order behavior still works for cancellable rows
9. Confirm mobile layout stacks toolbar controls cleanly

### Edge cases

- no results for a selected status
- slow refetch after changing status
- disabled or unavailable cancel action
- page reset when clearing filters from a non-first page
- `Draft` availability if the backend returns draft orders

## 8. Observability / Debuggability

No new logging or metrics are required.

Preserve current debug-friendly behavior:
- inline load error with retry
- snackbar mutation feedback
- visible refetch progress

## 9. Rollout Plan

- Frontend-only rollout in `src/web-app`
- No feature flag required
- Safe to ship with docs and Playwright updates in the same pass
- Monitor via normal smoke/E2E coverage because behavior is UI-only

## 10. Risks & Mitigations

### Risk 1. Removing explicit refresh may reduce perceived control

Mitigation:
- keep retry on errors
- keep visible refetch progress during filter changes
- avoid hidden background failures

### Risk 2. Status dropdown may hide filter discoverability if styled too subtly

Mitigation:
- keep it first in the toolbar
- use placeholder text `Status`
- keep `Clear` nearby for visibility of filter state

### Risk 3. Docs currently conflict with the requested direction

Mitigation:
- update global guidance to treat status tabs as a default, not a hard rule
- add an explicit Orders-page exception/approved pattern note

### Risk 4. Removing count loading could affect other screens if shared helpers are deleted too aggressively

Mitigation:
- audit `ordersStatusCountsCacheKey` usage before cleanup
- only remove shared cache helpers if they are truly unused after the refactor

### Risk 5. Introducing an inline `Edit` action now would create scope creep

Mitigation:
- keep overflow-only actions in this pass
- treat edit as a separate feature if needed later

## 11. Open Questions

1. Should the new status filter include `Draft`?
- Resolved: yes, include `Draft`.

2. Do you want manual refresh removed entirely, or relocated into the toolbar as a compact secondary control?
 - Resolved: move it into the toolbar as a compact secondary control.

3. What exact status control should the toolbar use?
- Resolved: compact dropdown/select.

4. How broad should the docs update be?
- Resolved: broaden the shared frontend rule so tabs and toolbar status filters are both approved patterns.

## Sources Consulted

- `docs/AGENTS.MD`
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`
- `docs-ai/2026-03-04-web-app-users-list-minimal-alignment-plan.md`
- `src/web-app/app/(dashboard)/orders/page.tsx`
- `src/web-app/app/(dashboard)/users/page.tsx`
- `src/web-app/tests/e2e/loading-ux-orders.spec.ts`
- `src/web-app/types/orders.ts`
- `src/web-app/package.json`
- `docs-ai/DESIGN-orders-list-toolbar-actions-2026-03-06.md`
- MUI docs via MCP:
  - `https://llms.mui.com/material-ui/7.2.0/api/select.md`
  - `https://llms.mui.com/material-ui/7.2.0/api/text-field.md`
  - `https://llms.mui.com/material-ui/7.2.0/api/toggle-button-group.md`
