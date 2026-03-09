# TASKS - Roles Permissions UX UI - 2026-03-06

## Audit notes

- Alignment with repo conventions: This breakdown stays within `web-app` frontend scope and keeps existing API contracts unchanged, consistent with `docs/AGENTS.MD` guidance to preserve backend contracts during frontend evolution.
- Required steps previously missing: explicit responsive breakpoint behavior, deterministic UI hooks for unsaved state/action visibility, and e2e coverage for the new UX states.
- Risks covered:
  - Sticky panels/action bars can break inside nested scroll containers.
  - Selection styling changes can accidentally reduce keyboard focus visibility.
  - Responsive split changes can regress at mid-width breakpoints.
- Rollout/testing coverage: includes focused `web-app` Playwright updates against existing `loading-ux-admin.spec.ts` patterns and manual viewport checks.

## Task list

### Backend/.NET

#### Task B1 - Keep API contract unchanged for UX-only work
- Goal: Ensure the UX/UI change set is frontend-only and does not alter AuthServer role APIs or response shapes.
- Files/modules likely touched:
  - No backend files expected.
- Acceptance criteria:
  - No changes under `src/backend/`.
  - Existing calls in `src/web-app/lib/admin-roles-api.ts` remain unchanged in signature/usage.
- Test notes:
  - No backend test changes required for this scope.
- Dependencies (blocks/on):
  - On: none.
  - Blocks: all frontend tasks assume unchanged API shape.

### Frontend/Next

#### Task F1 - Rebalance desktop layout and constrain roles rail
- Goal: Reduce roles-list visual dominance and allocate more space to details/scope editing.
- Files/modules likely touched:
  - `src/web-app/app/(dashboard)/users/roles/page.tsx`
  - `src/web-app/components/admin/roles/roles-list.tsx` (container sizing hooks only if needed)
- Concrete changes:
  - Replace current desktop split (`lg: 4/8`) with a narrower left rail and wider detail area.
  - Keep stacked mobile layout (`xs`).
  - Add independent scroll behavior for roles list panel so long lists do not push detail content.
  - Keep refresh/error/loading behavior unchanged.
- Acceptance criteria:
  - On desktop, detail panel visibly has more horizontal room than current implementation.
  - Long role lists scroll inside the left panel without shifting detail panel position.
  - On mobile, list/details remain vertically stacked and usable.
- Test notes:
  - Manual viewport checks at mobile, tablet, laptop, desktop widths.
  - Verify no regression in existing loading/refetch indicators.
- Dependencies (blocks/on):
  - On: B1.
  - Blocks: F3, F4.

#### Task F2 - Replace hard-blue selected row with soft semantic selection
- Goal: Improve row selection clarity without hiding green/yellow status labels.
- Files/modules likely touched:
  - `src/web-app/components/admin/roles/roles-list.tsx`
  - `src/web-app/components/common/app-label.tsx` (only if chip contrast tuning is required)
- Concrete changes:
  - Remove full-row `primary.main` selected fill and typography color inheritance override.
  - Use a softer selected treatment (subtle tint, stronger border, optional accent edge).
  - Preserve semantic text/chip colors in selected state.
  - Keep keyboard/focus-visible state clearly distinguishable from selected state.
- Acceptance criteria:
  - Selected role is visually clear without full blue background fill.
  - `Active` (`success`) and `Disabled` (`warning`) labels remain readable in selected and unselected rows.
  - Hover, focus, and selected states remain distinct and accessible.
- Test notes:
  - Manual contrast/readability check in light and dark themes if available.
  - Add/assert stable selector for selected row to avoid brittle style-only assertions.
- Dependencies (blocks/on):
  - On: B1.
  - Blocks: F4, S1.

#### Task F3 - Improve action visibility and unsaved-change affordance
- Goal: Make pending edits obvious and keep primary actions discoverable while editing scopes.
- Files/modules likely touched:
  - `src/web-app/app/(dashboard)/users/roles/page.tsx`
  - `src/web-app/components/admin/roles/scope-editor.tsx` (if action container coupling is needed)
- Concrete changes:
  - Add explicit unsaved-changes indicator derived from existing `hasMetadataChanges` and `hasScopeChanges`.
  - Keep `Reset` and `Save permissions` controls persistently visible during long scope-list scroll (sticky action region in the right workspace).
  - Ensure disabled/enabled states continue to mirror draft-vs-persisted logic.
- Acceptance criteria:
  - Unsaved indicator appears after metadata/scope edits and clears after successful save/reset.
  - Save/reset controls remain visible while scrolling through permission groups.
  - Existing save feedback (`Snackbar`) still fires on success/error.
- Test notes:
  - Add e2e assertion for unsaved indicator toggling.
  - Add e2e assertion for action-region visibility after scrolling permissions content.
- Dependencies (blocks/on):
  - On: F1.
  - Blocks: S1.

#### Task F4 - Lock responsive behavior for mid-width layouts
- Goal: Avoid cramped dual-column UX between tablet and laptop sizes.
- Files/modules likely touched:
  - `src/web-app/app/(dashboard)/users/roles/page.tsx`
  - `src/web-app/components/admin/roles/roles-list.tsx` (if per-breakpoint density tweaks needed)
- Concrete changes:
  - Define explicit breakpoint behavior for:
    - `xs/sm`: stacked list then details.
    - `md` and constrained widths: avoid overly narrow detail pane.
    - large desktop: side-by-side with narrowed roles rail.
  - Ensure headings, form controls, and action row wrap cleanly at each breakpoint.
- Acceptance criteria:
  - No overlap/truncation of key controls at target breakpoints.
  - Role details form remains readable without horizontal crowding.
  - Role list remains scannable with status chip and meta text visible.
- Test notes:
  - Manual checks at representative widths (for example 390, 768, 1024, 1280, 1440).
- Dependencies (blocks/on):
  - On: F1, F2.
  - Blocks: S1.

### Shared/Infra

#### Task S1 - Update web-app e2e coverage for the UX states
- Goal: Protect the new UX behavior with stable end-to-end checks.
- Files/modules likely touched:
  - `src/web-app/tests/e2e/loading-ux-admin.spec.ts`
  - `src/web-app/app/(dashboard)/users/roles/page.tsx` (add `data-testid` hooks if needed)
  - `src/web-app/components/admin/roles/roles-list.tsx` (add deterministic test hook for selected row if needed)
- Concrete changes:
  - Extend roles tests to cover:
    - selected row state hook exists after role click.
    - unsaved indicator appears/disappears with edits/save or reset.
    - action region remains visible while permission list is scrolled.
  - Keep existing first-load/refetch behavior assertions intact.
- Acceptance criteria:
  - Updated e2e spec passes for roles loading + new UX states.
  - No regression in current loading/refetch tests.
- Test notes:
  - Run: `cd src/web-app && npx playwright test tests/e2e/loading-ux-admin.spec.ts`
- Dependencies (blocks/on):
  - On: F2, F3, F4.
  - Blocks: release readiness for this UX update.

## Sources consulted (paths)

- `docs/AGENTS.MD`
- `docs/TESTING.md`
- `docs-ai/2026-03-01-web-app-detailed-implementation-plan.md`
- `src/web-app/app/(dashboard)/users/roles/page.tsx`
- `src/web-app/components/admin/roles/roles-list.tsx`
- `src/web-app/components/admin/roles/scope-editor.tsx`
- `src/web-app/components/common/app-label.tsx`
- `src/web-app/tests/e2e/loading-ux-admin.spec.ts`
