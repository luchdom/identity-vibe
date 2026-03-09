## Audit notes

- The clarified plan and design spec are aligned with the user request and with the checked-in `src/web-app` Orders page.
- No backend or API contract change is required; this is a frontend UX refactor in `src/web-app`.
- The main repo-level mismatch is documentation:
  - `docs/FRONTEND.md`
  - `docs/DEVELOPMENT.md`
  - `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`
  currently describe status tabs/counts as the default list pattern, while the clarified Orders direction intentionally uses a toolbar dropdown/select instead.
- There is no existing checked-in Orders edit route/dialog in `src/web-app`, so the accepted row-action model is overflow-only (`3 dots` menu only). This is confirmed scope, not a gap.
- Current automated coverage is concentrated in `src/web-app/tests/e2e/loading-ux-orders.spec.ts`; a dedicated Orders page structure/actions spec is still needed for this refactor.

## Backend

### Task 1. Confirm no backend changes are needed
- Goal:
  - Validate that the existing Orders endpoints and query contract already support the refactor without controller/service/repository changes.
- Files or modules likely touched:
  - No code changes expected.
  - Reference only:
    - `src/web-app/lib/orders-api.ts`
    - `src/web-app/types/orders.ts`
- Acceptance criteria:
  - Implementation proceeds without backend edits.
  - Existing `status`, `page`, and `pageSize` query usage remains valid.
  - No API contract or auth policy docs need backend-side amendments for this task.
- Test notes:
  - None beyond confirming existing mocked E2E contracts still represent the real frontend API usage.
- Dependencies:
  - Must be checked before frontend implementation starts deleting status-count logic.

## Frontend

### Task 2. Refactor the Orders page header and panel structure
- Goal:
  - Remove the top-level `Refresh` button from the page header and keep only `New order` as the header CTA.
- Files or modules likely touched:
  - `src/web-app/app/(dashboard)/orders/page.tsx`
- Acceptance criteria:
  - Header still shows page identity and `New order`.
  - The top header action row no longer contains `Refresh`.
  - Existing title/count text, route transition behavior, and `New order` permission gating still work.
- Test notes:
  - Add or update E2E assertion that no top-level `Refresh` button is rendered in the header.
- Dependencies:
  - Independent of row-action and docs work.

### Task 3. Replace status tabs/counts with a toolbar dropdown/select
- Goal:
  - Remove the status tab row and create a compact table-adjacent toolbar using the Users page as the nearest pattern.
- Files or modules likely touched:
  - `src/web-app/app/(dashboard)/orders/page.tsx`
  - Reference pattern:
    - `src/web-app/app/(dashboard)/users/page.tsx`
- Acceptance criteria:
  - `Tabs`, `Tab`, and top-of-panel status count badges are removed from Orders.
  - A compact status dropdown/select appears in the `SurfacePanel` directly above the table.
  - The status options are:
    - `All statuses`
    - `Draft`
    - `Confirmed`
    - `Processing`
    - `Shipped`
    - `Delivered`
    - `Cancelled`
  - The control uses toolbar-appropriate compact styling and placeholder-style empty display.
  - Mobile layout stacks cleanly without introducing a second filter surface.
- Test notes:
  - Add E2E assertions that status tabs/count badges are not rendered.
  - Add E2E assertions that the dropdown/select is present and keyboard reachable.
- Dependencies:
  - Depends on keeping the existing URL-driven list contract.

### Task 4. Wire the toolbar status, clear, and refresh behavior
- Goal:
  - Keep list controls adjacent to the table and ensure filter/reset/reload behavior remains predictable.
- Files or modules likely touched:
  - `src/web-app/app/(dashboard)/orders/page.tsx`
- Acceptance criteria:
  - Status changes update the `status` query parameter.
  - Status changes reset `page` to `1`.
  - `Clear` removes `status` and resets `page` to `1`.
  - `Refresh` is available in the toolbar and reloads the current list state without moving back to the header.
  - Existing first-load vs refetch behavior remains intact.
  - Existing inline retry-on-error remains available.
- Test notes:
  - Add E2E coverage for:
    - status selection updates URL
    - clear resets filter state
    - toolbar `Refresh` triggers refetch while keeping current content mounted
- Dependencies:
  - Depends on Task 3.

### Task 5. Remove Orders status-count loading logic and related page-only cache usage
- Goal:
  - Delete the extra page logic that exists only to render status tabs with counts.
- Files or modules likely touched:
  - `src/web-app/app/(dashboard)/orders/page.tsx`
  - Possibly `src/web-app/lib/cache/query-keys.ts` if helpers become unused after implementation audit
- Acceptance criteria:
  - `statusCounts` state is removed.
  - `countsAbortRef`, `countsRequestIdRef`, and `loadStatusCounts` are removed.
  - Effects/invalidation calls used only for tab counts are removed from the Orders page.
  - No extra count-fetching requests are made by the Orders list page after the refactor.
  - Shared cache helpers are only deleted if they are truly unused elsewhere.
- Test notes:
  - E2E/network assertions should verify the page no longer performs the count-only request pattern for each status.
  - Run lint/build checks to catch any newly unused imports or helpers.
- Dependencies:
  - Depends on Task 3 because tabs/counts must be removed first.

### Task 6. Simplify Orders table row actions to overflow-only
- Goal:
  - Remove the inline eye icon and leave the 3-dot menu as the only row action control.
- Files or modules likely touched:
  - `src/web-app/app/(dashboard)/orders/page.tsx`
- Acceptance criteria:
  - No inline eye `IconButton` remains in the table action column.
  - The 3-dot menu remains present.
  - `View details` remains the first menu item.
  - `Cancel order` remains available when allowed and retains current safeguards.
  - Row accessibility labels remain specific and clear.
- Test notes:
  - Add E2E assertion that no inline view icon is rendered.
  - Add E2E assertion that opening the row menu still shows `View details`.
- Dependencies:
  - Independent from toolbar work, but can be shipped in the same page refactor PR.

### Task 7. Reconcile empty, loading, and refetch states after the layout change
- Goal:
  - Preserve current loading UX rules while adapting the surface structure from tabs to a toolbar.
- Files or modules likely touched:
  - `src/web-app/app/(dashboard)/orders/page.tsx`
- Acceptance criteria:
  - First-load spinner/hint still works.
  - Refetch progress still keeps current table content mounted.
  - Error alert and retry remain visible and correctly placed inside the panel.
  - Empty-state copy refers to the current toolbar/filter model rather than tabs.
- Test notes:
  - Keep `src/web-app/tests/e2e/loading-ux-orders.spec.ts` focused on loading/refetch behavior and update selectors/text only as needed.
- Dependencies:
  - Depends on Tasks 2 through 4 because the panel structure is changing.

### Task 8. Add dedicated Orders page E2E coverage for the new structure
- Goal:
  - Add page-level regression coverage for layout, filters, and row actions rather than relying only on loading-UX tests.
- Files or modules likely touched:
  - New file:
    - `src/web-app/tests/e2e/orders-page.spec.ts`
  - Existing file:
    - `src/web-app/tests/e2e/loading-ux-orders.spec.ts`
- Acceptance criteria:
  - The new spec covers:
    - no header-level `Refresh`
    - toolbar-level status dropdown/select
    - toolbar `Clear`
    - toolbar `Refresh`
    - no status tabs/count badges
    - no inline eye action
    - `View details` in the 3-dot menu
    - status URL updates and clear behavior
  - The existing loading-UX spec still passes after selector/behavior updates.
- Test notes:
  - Use the existing mocked Orders responses and auth helpers in `src/web-app/tests/e2e`.
  - Keep test responsibilities separated:
    - structure/actions in `orders-page.spec.ts`
    - loading/refetch in `loading-ux-orders.spec.ts`
- Dependencies:
  - Depends on Tasks 2 through 7.

## Shared or Infra

### Task 9. Update shared frontend docs to approve both status tabs and toolbar status filters
- Goal:
  - Bring repo docs in line with the clarified Orders implementation direction without making Orders look like a one-off exception.
- Files or modules likely touched:
  - `docs/FRONTEND.md`
  - `docs/DEVELOPMENT.md`
  - `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`
- Acceptance criteria:
  - Shared docs no longer imply that status tabs with counts are the only approved list-filter pattern.
  - Shared docs explicitly allow both:
    - status tabs when counts/status buckets add value
    - compact toolbar status filters when status behaves like a standard list filter
  - Orders-specific notes describe the implemented pattern:
    - toolbar dropdown/select for status
    - toolbar `Refresh`
    - overflow-only row actions
- Test notes:
  - Manual docs review only.
- Dependencies:
  - Should be completed in the same delivery pass as the UI change to avoid stale guidance.

### Task 10. Final verification and rollout check
- Goal:
  - Validate the completed refactor end-to-end before implementation is considered done.
- Files or modules likely touched:
  - No new files expected.
  - Verification against:
    - `src/web-app/app/(dashboard)/orders/page.tsx`
    - `src/web-app/tests/e2e/*.spec.ts`
    - updated docs files
- Acceptance criteria:
  - Lint/build/tests relevant to `src/web-app` pass, or any failures are explicitly documented.
  - Manual checks confirm:
    - header has no top `Refresh`
    - toolbar shows status, `Clear`, `Refresh`
    - row actions are `3 dots only`
    - `View details` remains accessible
    - no status-count requests occur
  - Docs and implementation describe the same pattern.
- Test notes:
  - Minimum recommended commands:
    - `cd src/web-app && npm run lint`
    - `cd src/web-app && npm run test:e2e`
- Dependencies:
  - Depends on all prior tasks.

## Sources consulted (paths)

- `docs/AGENTS.MD`
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- `docs-ai/2026-03-06-orders-list-toolbar-actions-plan.md`
- `docs-ai/DESIGN-orders-list-toolbar-actions-2026-03-06.md`
- `docs-ai/2026-03-04-web-app-ui-pattern-rules.md`
- `src/web-app/app/(dashboard)/orders/page.tsx`
- `src/web-app/app/(dashboard)/users/page.tsx`
- `src/web-app/tests/e2e/loading-ux-orders.spec.ts`
- `src/web-app/lib/orders-api.ts`
- `src/web-app/types/orders.ts`
