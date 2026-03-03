# Web App Minimal Visual Alignment Implementation Tasks

Date: 2026-03-03

## Phase 1: Theme Rebuild

1. Rework the MUI theme under `src/web-app/theme/`.
2. Improve typography hierarchy and font choices.
3. Recalibrate radius, shadows, and background/paper contrast.
4. Add semantic layout and surface tokens.
5. Improve `app/globals.css` so the page background has more depth.

## Phase 2: Sidebar Restyle

1. Rework sidebar width, spacing, and top branding area.
2. Improve nav item alignment, hover states, and selected state treatment.
3. Remove visible scaffold/process copy from sidebar UI.
4. Reuse a temporary branding asset or shape mark.
5. Ensure desktop and mobile drawer styling match.

## Phase 3: Header Restyle

1. Rework header height and spacing.
2. Improve title/subtitle presentation.
3. Refine utility controls.
4. Improve avatar treatment and spacing.
5. Reduce flatness with cleaner surface styling.

## Phase 4: Shared Surface Primitives

1. Create `DashboardCard`, `MetricCard`, `SectionHeader`, `SurfacePanel`, `TableCardShell`, and `EmptyStateCard`.
2. Standardize padding, radius, and title/action layout.
3. Use these primitives for dashboard and future feature pages.

## Phase 5: Real Dashboard

1. Replace the placeholder in `app/(dashboard)/dashboard/page.tsx`.
2. Add a hero/welcome card with CTA and visual support.
3. Add KPI cards.
4. Add a quick actions block.
5. Add a recent orders section.
6. Add one table-style card and one list-style card.
7. Use realistic placeholder data first if live data is not wired yet.
8. Remove all visible phase/scaffold wording.

## Phase 6: Auth Screen Tightening

1. Refine auth shell proportions and spacing.
2. Replace migration/process wording with product-facing copy.
3. Improve brand block and CTA styling.
4. Keep callback states visually consistent with login.
5. Reduce unnecessary loading-screen flash on public pages.

## Phase 7: Asset And Favicon Cleanup

1. Add favicon/app icon assets.
2. Standardize temporary branding assets.
3. Ensure shell and auth assets are intentional and consistent.

## Phase 8: Visual QA

1. Compare live Minimal and local `web-app` with Playwright.
2. Review spacing, density, card depth, nav/header proportions, and typography hierarchy.
3. Do one dedicated polish pass before continuing deeper feature migration.

## Recommended Execution Order

1. Theme rebuild
2. Sidebar restyle
3. Header restyle
4. Shared surface primitives
5. Real dashboard
6. Auth screen tightening
7. Asset and favicon cleanup
8. Visual QA
