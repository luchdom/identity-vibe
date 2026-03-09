## Context and user goal

Refine the `src/web-app` Orders list so list-changing controls live next to the table, row actions are simpler, and status behaves like a normal filter instead of a top tab system with counts.

Primary user goal:
- Scan and narrow the orders list quickly, then open the correct order action without extra visual noise.

Secondary user goals:
- Keep `New order` prominent as a page-level action.
- Preserve current loading, error, pagination, and permission behavior.

Constraint:
- Interactive screen inspection was not performed because no running local app/session was used for this planning pass. This spec is based on checked-in code, tests, and docs.

## Current UX/UI issues found

- The page header includes a `Refresh` action that changes list contents but sits away from the list surface.
- Status is split into a dedicated `Tabs` row with count badges, which gives the page two separate control layers before the table.
- The Orders page does not match the nearby Users page pattern where filters live inside the card directly above the table.
- Each row currently shows both a quick view eye icon and a 3-dot overflow menu, even though `View details` is also already present in the overflow menu.
- The current status UI excludes `Draft`, even though `OrderStatus` supports it in `src/web-app/types/orders.ts`.
- Status counts require extra list requests through `loadStatusCounts`, adding UI weight and network work for information the user explicitly no longer wants.

## Recommended changes

1. Keep the page header focused on page identity and the primary action only.
- Keep `New order` in the header.
- Remove the top `Refresh` button from the header.
- Keep manual refresh available, but move it into the table-adjacent toolbar.

2. Replace the tab row with a compact filter toolbar inside the `SurfacePanel`, directly above the table.
- Put the status control in the same visual area as other list controls.
- Keep the toolbar compact and aligned with the Users page spacing pattern.

3. Use a compact single-select status filter instead of tabs.
- Preferred UX: `TextField select` with placeholder-style empty state and options:
  - `All statuses`
  - `Draft`
  - `Confirmed`
  - `Processing`
  - `Shipped`
  - `Delivered`
  - `Cancelled`
- Reason:
  - fits better when counts are removed
  - scales better than tabs when all statuses are shown
  - matches the established Users-page filter affordance
  - was explicitly chosen in clarification over segmented buttons/chips

4. Add nearby `Clear` and `Refresh` actions for list control.
- Reset `status` and `page` to the default list view.
- Keep both actions in the toolbar, not in the page header.

5. Simplify row actions.
- Remove the inline eye icon button from each row.
- Keep the 3-dot menu as the only row action control in this scope.
- Keep `View details` as the first menu item.
- Keep destructive or secondary actions like `Cancel order` inside the same menu.

6. Do not introduce an inline `Edit` action in this pass.
- No checked-in order edit route or edit dialog exists in `src/web-app`.
- Adding edit would expand scope beyond the requested list cleanup.
- If an edit flow is added later, the row can evolve to `Edit + overflow`, matching the documented default pattern.

## Component mapping

- Reuse `SurfacePanel` from `src/web-app/components/common/surface-panel.tsx` as the main list surface.
- Reuse the Users page toolbar pattern from `src/web-app/app/(dashboard)/users/page.tsx`.
- Reuse `TextField select` with `SelectProps.displayEmpty` and `renderValue` for the status filter.
- Reuse `Button` for compact `Clear`.
- Reuse `Menu` and `MenuItem` for row actions.
- Reuse `OrderStatusLabel` from `src/web-app/components/orders/order-status-label.tsx` for table status rendering and optionally for richer status menu items.
- Remove `Tabs`, `Tab`, `AppLabel`, and status-count specific UI from the Orders page.

## Layout, spacing, sizing, and responsive behavior

- Keep the header as a two-column layout:
  - left: page title and optional count text
  - right: `New order`
- Inside the `SurfacePanel`, use one toolbar row above the table.
- Preferred desktop toolbar structure:
  - left cluster: status select
  - right cluster: `Clear`, `Refresh`, and any future list filter controls
- On mobile:
  - stack controls vertically
  - keep all controls full width where needed
  - keep the table immediately below the toolbar with no extra filter band above it
- Keep control height at `36px`, matching current Users toolbar density.

## Color, typography, and visual hierarchy guidance

- Preserve the current compact `web-app` visual language.
- Remove the status-count badges from the top of the panel to reduce competing emphasis.
- Keep order status emphasis inside the table via `OrderStatusLabel`, not via a second navigation-like filter row.
- Keep action hierarchy clear:
  - `New order` remains the header CTA
  - filter reset stays secondary and close to the table
  - destructive order actions stay in the overflow menu

## Interaction and state behavior

- Status remains URL-driven through the `status` query parameter.
- Changing status should reset `page` to `1`.
- Clearing filters should remove `status` and reset `page` to `1`.
- Existing first-load vs refetch behavior should remain intact.
- Existing inline retry behavior should remain available on load errors.
- Removing tabs also removes the separate `loadStatusCounts` effect and the status-count cache dependency.
- Overflow menu behavior:
  - first item: `View details`
  - second item: `Cancel order` when allowed
  - preserve permission and cancellable-state guards

## Accessibility notes

- The status filter needs a clear accessible name, even if the visual trigger uses placeholder-style text.
- The 3-dot action button must keep a specific `aria-label` per row.
- Removing the eye icon reduces duplicate focus targets in each row.
- `Clear` should remain keyboard reachable before the table.
- Status selection and menu actions should remain fully keyboard operable through native MUI select/menu behavior.

## Acceptance criteria

- The Orders page no longer shows a top `Refresh` button.
- The Orders page keeps a manual `Refresh` action inside the table-adjacent toolbar.
- The page no longer renders status tabs or status count badges.
- Status is available as a compact table-adjacent filter control inside the main panel.
- The status filter supports resetting back to the unfiltered state.
- The table no longer shows an inline eye icon action.
- `View details` is available from the row overflow menu.
- No order-status count requests are made by the page after the refactor.
- Existing load, retry, pagination, and cancel-order flows still work.

## Sources consulted (paths/tools)

- `docs/AGENTS.MD`
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`
- `docs-ai/2026-03-04-web-app-users-list-minimal-alignment-plan.md`
- `src/web-app/app/(dashboard)/orders/page.tsx`
- `src/web-app/app/(dashboard)/users/page.tsx`
- `src/web-app/components/orders/order-status-label.tsx`
- `src/web-app/types/orders.ts`
- `src/web-app/tests/e2e/loading-ux-orders.spec.ts`
- MUI docs via MCP:
  - `https://llms.mui.com/material-ui/7.2.0/api/select.md`
  - `https://llms.mui.com/material-ui/7.2.0/api/text-field.md`
  - `https://llms.mui.com/material-ui/7.2.0/api/toggle-button-group.md`
