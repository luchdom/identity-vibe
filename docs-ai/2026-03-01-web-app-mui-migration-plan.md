# Web App MUI Migration Plan

Date: 2026-03-01
Status: Planning (pre-implementation)
Target app name: `web-app`
Current app kept in place: `src/web-admin`

Related current-project files:
- `src/web-admin/package.json`
- `src/web-admin/app/page.tsx`
- `src/web-admin/app/login/page.tsx`
- `src/web-admin/app/callback/page.tsx`
- `src/web-admin/app/(authenticated)/layout.tsx`
- `src/web-admin/app/(authenticated)/dashboard/page.tsx`
- `src/web-admin/app/(authenticated)/orders/page.tsx`
- `src/web-admin/app/(authenticated)/orders/new/page.tsx`
- `src/web-admin/app/(authenticated)/orders/[id]/page.tsx`
- `src/web-admin/app/(authenticated)/users/page.tsx`
- `src/web-admin/app/(authenticated)/users/roles/page.tsx`
- `src/web-admin/app/(authenticated)/users/activity/page.tsx`
- `src/web-admin/app/(authenticated)/admin/page.tsx`
- `src/web-admin/components/app-sidebar.tsx`
- `src/web-admin/contexts/auth-context.tsx`
- `src/web-admin/lib/oauth2-auth.ts`
- `src/web-admin/lib/orders-api.ts`
- `src/web-admin/lib/admin-users-api.ts`
- `src/web-admin/lib/admin-roles-api.ts`
- `src/web-admin/tests/orders-page.spec.ts`
- `src/web-admin/tests/order-detail-page.spec.ts`
- `src/web-admin/tests/new-order-page.spec.ts`
- `src/web-admin/tests/users-management-page.spec.ts`
- `src/web-admin/tests/roles-permissions-page.spec.ts`

Related docs:
- `docs/FRONTEND.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/DEPLOYMENT.md`

Related local template files:
- `C:/dev/templates/material-kit-react/package.json`
- `C:/dev/templates/material-kit-react/README.md`
- `C:/dev/templates/material-kit-react/src/layouts/dashboard/layout.tsx`
- `C:/dev/templates/material-kit-react/src/layouts/dashboard/nav.tsx`
- `C:/dev/templates/material-kit-react/src/layouts/nav-config-dashboard.tsx`
- `C:/dev/templates/material-kit-react/src/sections/auth/sign-in-view.tsx`
- `C:/dev/templates/material-kit-react/src/routes/sections.tsx`

External references reviewed:
- https://mui.com/store/items/minimal-dashboard/
- https://free.minimals.cc/
- https://minimals.cc/auth/amplify/sign-in?returnTo=%2Fdashboard
- https://llms.mui.com/material-ui/7.2.0/integrations/nextjs.md
- https://llms.mui.com/material-ui/7.2.0/integrations/routing.md
- https://llms.mui.com/material-ui/7.2.0/guides/testing.md

## Executive Summary

Build a new frontend in parallel at `src/web-app` using Next.js App Router + Material UI, while leaving `src/web-admin` intact until the new app reaches functional parity.

The local `material-kit-react` download is useful as a design/system reference and as a source of reusable theme/layout code, but it is not a direct starter for this migration:
- it is the free Vite React kit, not a Next.js project
- it uses `react-router-dom`, not Next App Router
- it ships a smaller page set than the full Minimal Dashboard product page

That means the migration should be a controlled port:
1. create a fresh Next.js app for `web-app`
2. integrate Material UI correctly for Next App Router
3. transplant the Minimal layout/theme patterns into Next
4. port business functionality from `web-admin`
5. cut traffic over only after parity and validation

## Key Findings From Research

### Current `web-admin` is already functionally meaningful

The current app is not just a shell. It already contains production-relevant flows:
- OAuth2 Authorization Code + PKCE login against `AuthServer`
- token refresh and session-expired handling
- protected authenticated shell with sidebar
- orders list with status filtering and scope-aware actions
- order detail page
- new-order creation flow
- users management
- roles and permissions management

This means the migration is mainly a UI-shell and component-system migration, not a greenfield product-definition exercise.

### Some current routes are placeholders or low-confidence

The current app also contains routes that should not be treated as parity blockers unless you explicitly want them:
- `/dashboard` is still placeholder content
- `/users/activity` is a placeholder
- `/admin` is a demo-like admin-only page
- sidebar links under settings and several admin subroutes do not have implemented pages

These should be classified as deferred or redesign candidates, not automatic v1 migration targets.

### The local Minimal template is not a drop-in Next starter

From `C:/dev/templates/material-kit-react/package.json` and README:
- stack is Vite + React 19
- MUI version is `^7.0.1`
- routing is `react-router-dom`
- the free version provides only a subset of pages

Implication:
- reuse the visual language, theme, layout primitives, icons, assets, spacing, and page structure ideas
- do not copy its routing architecture directly
- do not attempt a blind folder-by-folder import into Next

### The MUI Store page describes a broader target than the local free kit

From the MUI Store product page reviewed on 2026-03-01:
- the product advertises Next.js App Router support
- it lists MUI v7, React 19, private/public routes, light/dark mode, React Hook Form + Zod, responsive layout, multiple auth examples, and 70+ example pages

Implication:
- use the product page as the target capability/spec reference
- use the local free Vite kit as the concrete code/style reference
- do not assume the free local template includes all pro/store features

### MUI + Next integration has a specific required setup

Per current MUI docs for Next App Router:
- use `@mui/material-nextjs`
- wrap the app with `AppRouterCacheProvider`
- place `ThemeProvider` above the app content
- decide early whether to enable CSS theme variables

Implication:
- the MUI integration should be established first in `web-app`
- theme setup is foundation work, not something to bolt on after feature pages exist

## Recommended Migration Strategy

### High-level approach

Use a parallel-run migration.

Recommended structure:
- keep existing app at `src/web-admin`
- create new app at `src/web-app`
- point both at the same `AuthServer` and `Orders` APIs during the migration
- migrate feature-by-feature
- cut over only after acceptance

This avoids mixing two component systems in one frontend and avoids destabilizing the current admin while design and routing are in motion.

### Recommended platform decision

Use:
- Next.js App Router
- React 19
- Material UI v7
- TypeScript

Pragmatic version note:
- the existing repo frontend is already on Next 15.5.x
- MUI’s current Next integration docs explicitly support `@mui/material-nextjs/v15-appRouter`

Recommendation:
- start `web-app` on Next 15 App Router for the first migration pass to stay aligned with the existing repo and reduce version churn
- only consider Next 16 after parity if there is a clear reason

### Recommended architectural rule

Do not migrate business logic by rewriting everything at once.

Split the move into three layers:
1. shell migration
2. shared app services migration
3. feature-page migration

That keeps behavior stable while the UI system changes underneath.

## Functionality Inventory

This is the recommended parity inventory to use for planning the new `web-app`.

### Must migrate in `web-app` v1

#### Authentication and session

- Public login route
- OAuth2 Authorization Code + PKCE redirect flow to `AuthServer`
- callback handling
- access token + refresh token storage
- `userinfo` profile hydration
- refresh-on-401 behavior
- session-expired modal
- logout flow
- public/private route separation

Important product decision:
- keep the current custom AuthServer OAuth2 flow
- do not replace it with Amplify/Auth0/Firebase/Supabase just because the Minimal product has sample auth variants

Use the Minimal auth layout style, not its auth provider logic.

#### App shell

- responsive dashboard layout
- desktop sidebar + mobile drawer navigation
- top app bar/header
- authenticated route wrapper
- unauthorized page
- account menu
- breadcrumbs/page header pattern

Recommended shell source:
- port the Minimal dashboard layout ideas
- adapt nav data to this product’s actual routes only

#### Orders

- Orders list page (`/orders`)
- status filter controls (URL-driven)
- paginated table
- refresh action
- scope-aware `New Order` button
- order row action menu
- order detail page (`/orders/[id]`)
- create new order page (`/orders/new`)
- create-scope gating (`orders.write` or `orders.manage`)

Keep existing backend contracts unless there is a clear reason to improve them.

#### User administration

- Users list page (`/users`)
- search filter
- status filter
- role filter
- pagination
- create user dialog
- edit user dialog
- roles assignment dialog
- reset password dialog
- enable/disable user action
- self-disable guardrail

#### Roles and permissions

- Roles list page (`/users/roles`)
- available scopes loading
- role selection
- create role dialog
- metadata edit (description + active flag)
- scope-group editing
- save/reset workflow

### Should migrate, but can be simplified in v1

#### Dashboard

The current `/dashboard` is a placeholder, but `web-app` should not carry that placeholder forward.

Recommended v1:
- create a real dashboard landing page
- use Minimal’s overview-style layout
- populate it with this project’s real business data, not mock analytics

Recommended content:
- order counts by status
- recent orders
- users summary
- roles summary
- quick links to core admin pages

If a dedicated backend dashboard endpoint is added later, that can be phase 2. For the migration itself, the UI shell should at least stop being placeholder content.

### Migrate later or explicitly defer

These exist today but should be treated as optional unless you decide otherwise:
- `/users/activity` (currently placeholder)
- `/admin` (demo-style admin page)
- all sidebar links for settings pages without implemented routes:
  - `/settings`
  - `/settings/oauth`
  - `/settings/api-keys`
  - `/settings/audit`
  - `/admin/settings`
  - `/admin/diagnostics`

Recommendation:
- keep them out of `web-app` v1 navigation until there is a real implementation plan
- do not recreate empty pages just to match a menu

## Recommended Information Architecture For `web-app`

Keep the route tree narrower than the current sidebar.

Recommended v1 route set:
- `/`
- `/login`
- `/callback`
- `/unauthorized`
- `/dashboard`
- `/orders`
- `/orders/new`
- `/orders/[id]`
- `/users`
- `/users/roles`

Optional v1.1:
- `/users/activity`

Recommended nav groups:
- Overview
  - Dashboard
- Management
  - Orders
  - Users
  - Roles & Permissions

Do not carry forward fake or future-state navigation items into the first release of `web-app`.

## UI/Component Mapping Plan

Map current shadcn/Radix usage to MUI primitives, then rebuild feature pages around those MUI components.

### Core component mapping

- `Button` -> MUI `Button`, `IconButton`
- `Card` -> MUI `Card`, `CardHeader`, `CardContent`, `CardActions`
- `Badge` -> MUI `Chip` or `Box` with themed status styling
- `Dialog` -> MUI `Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`
- `Tooltip` -> MUI `Tooltip`
- `Dropdown menu` -> MUI `Menu`, `MenuItem`
- `Input` -> MUI `TextField`
- `Select` -> MUI `Select`, `MenuItem`, or `TextField` with `select`
- `Table` -> MUI `Table` family
- `Skeleton` -> MUI `Skeleton`
- `Breadcrumb` -> MUI `Breadcrumbs`
- `Sidebar` -> custom composition using `Drawer`, `List`, `ListItemButton`, `Toolbar`, `Box`
- `Toast` (`sonner`) -> MUI `Snackbar` + `Alert`, or keep `sonner` temporarily if you want faster parity

### Recommendation on data grids

Do not introduce MUI X `DataGrid` in the first migration pass unless the team explicitly wants it.

Reason:
- the current tables are functional but not large/complex enough to require a grid abstraction
- MUI `Table` is easier to control during a one-for-one migration
- it avoids extra licensing/behavior changes during parity work

You can revisit `DataGrid` later if sorting, column pinning, virtualization, or inline editing become a real need.

## Shared Logic Migration Plan

These areas should be ported first, with behavior preserved:

### Auth and API services

Port or extract from `web-admin`:
- `oauth2-auth.ts`
- shared axios clients/interceptors
- auth context/provider
- scope parsing and authorization helpers

Recommended structure in `web-app`:
- `src/web-app/lib/auth/`
- `src/web-app/lib/api/`
- `src/web-app/contexts/`
- `src/web-app/lib/permissions/`

Goal:
- business behavior stays the same
- only the UI wrapper changes

### Route guards

Rebuild current `AuthGuard` concepts with MUI-friendly loading and unauthorized states:
- require auth
- require scopes
- support public routes (`/login`)
- preserve redirect-to-return-url logic

### Domain API modules

Port and keep stable:
- orders API client
- users admin API client
- roles admin API client

This should be a near-copy with only path and import cleanup, not a functional redesign.

## Detailed Build Plan

## Phase 0: Scope freeze and parity matrix

Before coding, define the migration target explicitly.

Outputs:
- route inventory
- feature parity checklist
- explicit defer list
- acceptance criteria per page

Decision gate:
- confirm whether placeholder pages are in or out
- confirm whether the dashboard becomes real in v1

## Phase 1: Create the new `web-app` foundation

Create a fresh Next.js app at `src/web-app`.

Foundation tasks:
- initialize Next.js App Router app
- add TypeScript
- install MUI core dependencies
- install `@mui/material-nextjs`
- configure `AppRouterCacheProvider`
- add shared theme setup
- choose font strategy
- wire `CssBaseline`
- define theme tokens (palette, typography, spacing, component defaults)

Recommendation:
- create a small app-specific theme inspired by Minimal
- do not attempt to mirror the template theme file-by-file until the app is running

## Phase 2: Port the Minimal shell into Next

Recreate the layout system in Next, borrowing structure from the local template:
- responsive sidebar
- mobile drawer
- app header
- search slot
- notifications slot
- account menu
- content container rules

Key adaptation work:
- replace `react-router-dom` navigation with Next navigation
- replace the template’s route hooks with Next App Router hooks
- remove template demo items not relevant to this product

Deliverable:
- a working authenticated shell that feels like Minimal, but with this project’s navigation

## Phase 3: Port auth and session management

Move the current auth stack into `web-app`.

Tasks:
- port PKCE flow logic
- port callback handling
- port refresh token behavior
- port session expiry behavior
- rebuild login page using Minimal-inspired auth visuals
- implement public/private route wrappers

Acceptance criteria:
- login from `web-app` reaches `AuthServer`
- callback returns to the intended protected route
- token refresh still works
- unauthorized and expired-session states are clear

## Phase 4: Build real route skeletons

Create the route tree and basic pages first:
- `/dashboard`
- `/orders`
- `/orders/new`
- `/orders/[id]`
- `/users`
- `/users/roles`

Each page should initially render:
- page title
- breadcrumb/header
- loading state
- basic auth/scope guard

This reduces risk before detailed UI migration begins.

## Phase 5: Migrate the Orders feature set

Recommended sequence:
1. orders list
2. order detail
3. new order

Orders list tasks:
- move list data fetch
- rebuild filter controls with MUI
- rebuild row action menu
- preserve URL-driven status filter
- preserve scope-aware new-order CTA behavior

Order detail tasks:
- port data contract mapping
- rebuild sections as MUI cards
- render customer, items, shipping, totals, and status history cleanly

New order tasks:
- keep validation rules
- keep submission behavior
- keep redirect back to `/orders`

Recommended implementation rule:
- preserve existing backend payload shapes exactly during migration

## Phase 6: Migrate Users management

Tasks:
- port list loading
- port search/status/role filters
- rebuild table
- rebuild dialogs for create/edit/roles/reset password
- preserve user-management guardrails

UI recommendation:
- use MUI dialogs and form controls
- keep the feature dense and admin-oriented, not over-designed

## Phase 7: Migrate Roles & Permissions

Tasks:
- port roles list
- port available-scopes load
- rebuild scope editor using MUI cards, checkboxes, dividers, and grouped sections
- preserve save/reset workflow
- preserve soft-disable behavior

UX recommendation:
- this page is a good candidate for a two-column layout similar to the current implementation:
  - role list on the left
  - details + scope editor on the right

## Phase 8: Build the new dashboard

Since the current dashboard is placeholder, use the migration to fix that.

Recommended v1 dashboard contents:
- summary cards for orders/users/roles
- recent orders list
- quick-action shortcuts
- optional basic chart if there is enough time and reliable data

This page should use the Minimal overview design language, but it should show real project data, not the template’s mock business metrics.

## Phase 9: Testing and parity validation

Testing should follow current MUI guidance:
- test by DOM behavior and user actions
- do not bind tests to MUI internals
- avoid snapshot-heavy tests

Recommended test layers:
- unit tests for mapping/helpers
- Playwright for critical flows

Critical end-to-end checks:
- login
- callback
- protected route access
- orders list/filter
- new order create flow
- order detail render
- user create/disable
- role selection/update

## Phase 10: Cutover and deprecation

Only after parity:
- decide whether `web-app` replaces `web-admin` in compose and deployment
- update redirect URIs if the public URL changes
- switch deployment/service naming
- keep `web-admin` available briefly as rollback

Do not delete `src/web-admin` until the cutover has passed acceptance.

## Recommended Deliverables By Work Package

### Work package 1: Foundation

- `src/web-app` app scaffold
- MUI + Next integration
- theme
- shell

### Work package 2: Shared services

- auth provider
- route guards
- API clients
- permission helpers

### Work package 3: Feature pages

- dashboard
- orders
- users
- roles

### Work package 4: Validation and cutover

- tests
- docs updates
- deployment updates
- final parity sign-off

## What To Reuse vs. What To Replace

### Reuse with minimal behavior change

- OAuth2 PKCE flow
- token refresh logic
- API endpoints and contracts
- permission model and scope names
- page-level feature boundaries
- Playwright scenario coverage concepts

### Replace

- shadcn/Radix UI primitives
- Tailwind-first page composition
- current sidebar implementation
- current login view
- current placeholder dashboard

### Reuse selectively from the Minimal template

- layout proportions
- drawer/header/nav patterns
- theme direction
- spacing and card composition
- auth page structure
- icon and asset approach

### Do not copy directly

- `react-router-dom` routing setup
- mock data pages unrelated to this project
- demo routes that do not map to real product features
- auth-provider-specific example logic from the pro demo

## Risks and Constraints

### Risk 1: Treating the free Vite template as a full Next starter

This is the biggest planning risk.

Reality:
- the local code is not the same artifact as the advertised Next.js package on the MUI Store page

Mitigation:
- build a fresh Next app
- port selected template layers intentionally

### Risk 2: Rebuilding UI and behavior at the same time

If layout changes and auth/API logic changes together, bugs will multiply.

Mitigation:
- preserve service-layer behavior first
- change presentation second

### Risk 3: Migrating placeholder or fake pages by accident

Current nav contains items without real backed functionality.

Mitigation:
- keep the v1 nav smaller
- only migrate implemented business features first

### Risk 4: Overreaching into “template feature chasing”

The Minimal product offers many features that are not needed for this app.

Mitigation:
- treat the product page as a reference for UX and platform capabilities
- not as a requirement to adopt every advertised module

## Recommended Docs Changes

These are proposed documentation changes to support the migration. They do not need to happen before coding, but they should be part of the plan.

### Add a new frontend doc for the new app

Add:
- `docs/WEB-APP.md`

Purpose:
- canonical guide for `src/web-app`
- MUI/Next setup
- auth flow notes
- route structure
- theme and component conventions

### Update `docs/ARCHITECTURE.md`

Update to document the migration period explicitly:
- there are temporarily two frontend apps
- both frontends call `AuthServer` and `Orders`
- eventual target frontend is `web-app`

Also update key-files references to include `src/web-app` once it exists.

### Update `docs/DEVELOPMENT.md`

Add:
- `src/web-app` startup instructions
- `src/web-app/.env.local` example
- local development guidance for running old and new frontends side by side

Clarify:
- which app is the current default
- which app is under migration

### Update `docs/FRONTEND.md`

Expand it from “web-admin integration notes” into a dual-frontend or migration-aware frontend guide.

Recommended new sections:
- current stable frontend: `web-admin`
- target frontend: `web-app`
- shared auth/API behavior
- MUI-specific integration notes for Next App Router
- component migration mapping

### Update `docs/DEPLOYMENT.md`

During migration:
- document whether compose exposes `web-admin`, `web-app`, or both
- document environment variables for the new frontend
- document redirect URI expectations for the new app

At cutover:
- rename `client` references if needed
- make the default deployed frontend explicit

### Optional tracking doc

Add:
- `docs/WEB-APP-MIGRATION.md`

Purpose:
- parity checklist
- deferred items
- cutover criteria

This is worth adding if the migration will span multiple sessions or branches.

## Proposed Initial Backlog For `web-app`

Use this as the first implementation backlog.

### Epic 1: Foundation

- Create `src/web-app`
- Install MUI + Next integration packages
- Add theme and layout providers
- Build responsive shell

### Epic 2: Shared app services

- Port auth provider
- Port OAuth2 PKCE service
- Port API clients
- Add route guards

### Epic 3: Orders

- Orders list
- Order detail
- New order

### Epic 4: Admin management

- Users
- Roles & permissions

### Epic 5: Dashboard

- Replace placeholder dashboard with real `web-app` landing page

### Epic 6: Validation

- Port/adapt Playwright coverage
- Run parity review
- Prepare cutover

## Recommended Order For Real Execution

1. Define the parity scope and defer list.
2. Scaffold `src/web-app` with proper MUI + Next App Router integration.
3. Port the Minimal-inspired shell and theme.
4. Port auth/session logic unchanged in behavior.
5. Bring up route skeletons with guards.
6. Migrate Orders fully.
7. Migrate Users fully.
8. Migrate Roles & Permissions fully.
9. Replace the placeholder dashboard with a real one.
10. Update docs and deployment wiring.
11. Run side-by-side validation before any cutover.

## Final Recommendation

Treat this as a parallel frontend replacement, not a component-swap refactor inside `web-admin`.

The cleanest path is:
- new Next App Router app in `src/web-app`
- MUI foundation set up correctly from the start
- Minimal template used as a visual/layout source, not as a direct starter
- existing auth/API behavior preserved
- implemented features migrated first
- placeholder pages and fake nav intentionally deferred

That gives you a cleaner codebase, a lower-risk migration, and a better chance of actually landing on a maintainable MUI-based admin instead of a half-converted hybrid.
