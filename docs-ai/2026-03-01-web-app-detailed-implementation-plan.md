# Web App Detailed Implementation Plan

Date: 2026-03-01
Status: Planning (pre-implementation)
Target app: `src/web-app`
Current app retained during migration: `src/web-admin`

Related planning docs:
- `docs-ai/2026-03-01-web-app-mui-migration-plan.md`
- `docs-ai/2026-02-28-orders-dashboard-plan.md`

## Purpose

This document translates the migration strategy into an execution-ready implementation plan.

It is intentionally more prescriptive than the migration plan:
- it defines the recommended build order
- it identifies what to create first
- it describes what each phase should produce
- it adds acceptance criteria and sequencing rules

This is the recommended order to build `web-app` without breaking the current `web-admin`.

## Implementation Principles

Use these rules throughout the implementation:

1. Build `web-app` in parallel. Do not delete or heavily refactor `src/web-admin` during early phases.
2. Preserve existing backend contracts first. UI migration should not become a backend rewrite.
3. Establish MUI + Next foundations before porting any real feature.
4. Move shared behavior once, then reuse it across pages:
- auth
- API clients
- permission checks
- route guards
5. Keep the first navigation smaller than the current `web-admin` navigation.
6. Do not import the local Minimal Vite project wholesale. Port selected layout/theme patterns only.
7. Each phase should end with something that can be run and manually checked.

## Recommended Final v1 Route Scope

Build these routes in the first full migration pass:

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

Explicitly defer for now:
- `/users/activity`
- `/admin`
- settings pages that do not currently exist in a real state

## Recommended Folder Structure For `src/web-app`

Use a structure that separates shell, features, and shared services.

Recommended initial shape:

```text
src/web-app/
  app/
    (auth)/
      login/
      callback/
    (dashboard)/
      dashboard/
      orders/
      users/
      layout.tsx
    unauthorized/
    page.tsx
    layout.tsx
  components/
    app-shell/
    auth/
    common/
    orders/
    users/
    roles/
    dashboard/
  contexts/
    auth-context.tsx
  hooks/
    use-orders.ts
    use-order.ts
    use-users.ts
    use-roles.ts
  lib/
    api/
    auth/
    permissions/
    utils/
  theme/
    index.ts
    palette.ts
    typography.ts
    components.ts
  types/
    orders.ts
    admin-users.ts
    admin-roles.ts
```

You can refine names later, but starting with this separation will reduce churn.

## Execution Sequence

## Phase 0: Pre-Implementation Decisions

### Goal

Lock decisions that will otherwise cause rework later.

### Tasks

1. Confirm the `web-app` name and location:
- recommended: `src/web-app`

2. Confirm the version baseline:
- Next.js App Router
- React 19
- Material UI v7

3. Confirm the v1 route list:
- keep only the routes listed in this plan
- explicitly defer placeholder/demo routes

4. Confirm whether the dashboard must be real in v1:
- recommendation: yes, replace the placeholder concept with a real landing page

5. Confirm whether to keep `sonner` temporarily:
- recommendation: optional
- fastest path is to keep it at first, then replace with MUI Snackbar later if desired

### Deliverables

- a fixed v1 scope list
- agreement that this is a parallel app build, not an in-place component swap

### Definition of Done

- no unresolved question remains about which pages are in v1
- no unresolved question remains about where `web-app` will live

## Phase 1: Create the Base Project

### Goal

Create a clean Next.js application that runs independently before any feature work starts.

### Tasks

1. Create the new app directory:
- `src/web-app`

2. Initialize a fresh Next.js app with App Router.

Recommended baseline:
- TypeScript enabled
- ESLint enabled
- App Router enabled

3. Add a minimal root layout and home page so the app boots.

4. Add `.env.local.example` or equivalent local env documentation for `web-app`.

### Recommended package setup

Install the base runtime dependencies first:
- `next`
- `react`
- `react-dom`
- `@mui/material`
- `@emotion/react`
- `@emotion/styled`
- `@mui/material-nextjs`
- `axios`

Recommended supporting dependencies:
- `@mui/icons-material` if you want standard MUI icons
- `react-hook-form`
- `zod`
- `@hookform/resolvers`

Optional immediate dependencies:
- `dayjs`
- `sonner` if preserving current toast behavior temporarily

### Detailed implementation steps

1. Create `src/web-app/package.json`.
2. Add the standard Next scripts:
- `dev`
- `build`
- `start`
- `lint`
3. Add `tsconfig.json`.
4. Add `next.config.ts`.
5. Create:
- `app/layout.tsx`
- `app/page.tsx`
- `app/globals.css`
6. Start the app and verify it renders.

### Deliverables

- `web-app` starts on its own
- the project compiles
- the project is isolated from `web-admin`

### Definition of Done

- `npm run dev` in `src/web-app` launches a working Next app
- there are no MUI imports yet beyond what is needed for the next phase

## Phase 2: Set Up Material UI Correctly For Next App Router

### Goal

Establish the MUI integration once, correctly, before building UI.

### Tasks

1. Wrap the app with MUI’s App Router cache provider.
2. Add the theme provider.
3. Add `CssBaseline`.
4. Decide on typography and base color tokens.
5. Verify SSR and hydration are stable.

### Detailed implementation steps

1. In `src/web-app/app/layout.tsx`:
- add `AppRouterCacheProvider` from `@mui/material-nextjs/v15-appRouter`
- wrap the body content with it

2. Create a dedicated theme file:
- recommended: `src/web-app/theme/index.ts`

3. In the theme:
- define primary, secondary, success, warning, error colors
- define shape radius rules
- define typography scale
- define component defaults for buttons, cards, text fields, tooltips, dialogs

4. In the root layout:
- wrap app content with `ThemeProvider`
- render `CssBaseline`

5. Add font configuration:
- use `next/font`
- prefer a consistent font family aligned with the Minimal visual style

6. Decide whether to enable MUI CSS variables:
- recommendation: enable early if you want long-term theming flexibility

### Important implementation note

Do not start copying Minimal layout code until this phase is stable. Otherwise layout bugs and theming bugs get mixed together.

### Deliverables

- working MUI provider stack
- theme file
- global typography and base surface styles

### Definition of Done

- a simple MUI page renders with correct styling
- no hydration warnings
- theme tokens are centralized, not hardcoded per page

## Phase 3: Bring Over The Minimal Visual Foundation

### Goal

Port the useful visual system from the local Minimal template without importing its routing model.

### Tasks

1. Review the local template sections that are worth reusing:
- theme direction
- dashboard shell proportions
- nav spacing
- auth layout style
- icons/assets if desired

2. Create an app-specific MUI shell that feels like Minimal but is built for Next.

3. Import only the assets you actually use.

### Detailed implementation steps

1. From the local template, use as references:
- `src/layouts/dashboard/layout.tsx`
- `src/layouts/dashboard/nav.tsx`
- `src/layouts/nav-config-dashboard.tsx`
- `src/sections/auth/sign-in-view.tsx`

2. Recreate equivalents in `web-app`:
- `components/app-shell/app-shell.tsx`
- `components/app-shell/sidebar.tsx`
- `components/app-shell/header.tsx`
- `components/app-shell/nav-config.ts`

3. Do not port:
- `react-router-dom`
- template route hooks
- demo nav items unrelated to this app

4. If you reuse icons or illustrations:
- copy only the specific files needed into `src/web-app/public/assets/...`

### Deliverables

- the visual language is established before feature pages are built

### Definition of Done

- a static page inside the new shell already looks recognizably like the target MUI design

## Phase 4: Build The Authenticated Shell And Sidebar

### Goal

Create the layout system first: sidebar, header, content area, and route grouping.

This is the point where the app begins to feel like the future product.

### Tasks

1. Create the authenticated route group.
2. Add the sidebar.
3. Add the header/app bar.
4. Add the content container layout.
5. Add a basic route config for the v1 pages.

### Detailed implementation steps

1. Create an authenticated layout file:
- `src/web-app/app/(dashboard)/layout.tsx`

2. Inside that layout:
- wrap children with the app shell component
- place the sidebar on desktop
- place a drawer-based nav on mobile
- place a top header with page-level actions/slots

3. Create a nav config with only:
- Dashboard
- Orders
- Users
- Roles & Permissions

4. Ensure active route highlighting works with Next App Router path hooks.

5. Add responsive behavior:
- desktop persistent sidebar
- mobile collapsible drawer

### Recommended component list

- `Drawer`
- `AppBar`
- `Toolbar`
- `List`
- `ListItemButton`
- `ListItemIcon`
- `ListItemText`
- `IconButton`
- `Box`
- `Divider`

### Deliverables

- reusable shell for all protected pages

### Definition of Done

- a user can navigate between placeholder protected pages with a working sidebar
- active navigation state is correct
- the layout behaves acceptably on desktop and mobile

## Phase 5: Add Base Route Skeletons

### Goal

Create all v1 routes as lightweight shells before real logic is attached.

### Tasks

Create these pages:
- `app/page.tsx`
- `app/(auth)/login/page.tsx`
- `app/(auth)/callback/page.tsx`
- `app/unauthorized/page.tsx`
- `app/(dashboard)/dashboard/page.tsx`
- `app/(dashboard)/orders/page.tsx`
- `app/(dashboard)/orders/new/page.tsx`
- `app/(dashboard)/orders/[id]/page.tsx`
- `app/(dashboard)/users/page.tsx`
- `app/(dashboard)/users/roles/page.tsx`

### Detailed implementation steps

1. Add each route with:
- page title
- placeholder card
- route-specific heading

2. Ensure each route is reachable from the sidebar where applicable.

3. Keep page shells visually consistent:
- content width
- spacing
- title hierarchy
- section cards

### Deliverables

- full route tree exists

### Definition of Done

- every v1 route loads without 404s
- navigation can reach all protected pages

## Phase 6: Port Auth Core (Before Real Login UI)

### Goal

Move the current authentication behavior into the new app before styling the final login flow.

### Tasks

1. Port the PKCE service.
2. Port auth storage rules.
3. Port token refresh behavior.
4. Port profile loading.
5. Port the auth context/provider.

### Detailed implementation steps

1. Create:
- `src/web-app/lib/auth/oauth2-auth.ts`

2. Port behavior from `src/web-admin/lib/oauth2-auth.ts`:
- `authorize()`
- `handleCallback()`
- `refreshToken()`
- `logout()`
- access token storage
- refresh token storage
- granted scopes storage
- `userinfo` loading

3. Create:
- `src/web-app/contexts/auth-context.tsx`

4. Port behavior from `src/web-admin/contexts/auth-context.tsx`:
- initialize auth on app load
- refresh token on expired access token
- session-expired flow
- exposed `login`, `logout`, `authorize`

5. Add an app-level provider wrapper in `app/layout.tsx`.

### Important implementation rule

Do not redesign auth behavior in this phase. This is a port, not a replacement.

### Deliverables

- auth state can be initialized and consumed in the new app

### Definition of Done

- a test page can read auth state from context
- `authorize()` can start the redirect flow

## Phase 7: Add Route Guards And Public/Private Routing

### Goal

Protect routes with the same access logic used in `web-admin`.

### Tasks

1. Rebuild an auth guard for protected routes.
2. Rebuild scope-based protection.
3. Preserve redirect-after-login behavior.
4. Add an unauthorized screen.

### Detailed implementation steps

1. Create:
- `components/auth/auth-guard.tsx`

2. Port the current concepts:
- `requireAuth`
- `requiredScopes`
- loading state while auth initializes
- redirect unauthenticated users to `/login`
- redirect forbidden users to `/unauthorized`

3. Preserve `returnUrl` behavior:
- store intended URL before redirecting to auth
- return the user there after successful callback

4. Add an `Unauthorized` page with clear message and route back options.

### Deliverables

- route access rules work before feature logic is added

### Definition of Done

- protected routes redirect correctly
- scope-protected routes block correctly

## Phase 8: Build The Login, Callback, And Session UI

### Goal

Create the public auth experience using the new MUI look while preserving the current auth behavior.

### Tasks

1. Build the login page.
2. Build the callback page.
3. Build the session-expired modal or equivalent.
4. Wire logout UI in the shell.

### Detailed implementation steps

1. Rebuild the login page with a Minimal-inspired layout:
- centered auth card
- branded hero/illustration area if desired
- sign-in action

2. Do not use the template’s Amplify logic.
- use your current `AuthServer` OAuth2 PKCE redirect only

3. The login page can be simple:
- a “Sign in” button
- optional helper text
- optional environment/debug hints in development only

4. Rebuild callback behavior:
- loading indicator while token exchange runs
- error state if callback fails
- redirect to stored return URL or `/dashboard`

5. Add session-expired handling:
- modal or dialog
- confirm action returns the user to login/logout path cleanly

6. Add account menu in the header/sidebar:
- user name/email
- logout action

### Deliverables

- the full auth loop works in the new UI

### Definition of Done

- login redirects out and returns successfully
- callback stores state correctly
- logout clears session state

## Phase 9: Port Shared API Clients And Permission Helpers

### Goal

Move all non-UI service behavior needed by the feature pages.

### Tasks

1. Rebuild shared axios clients.
2. Port request interceptors.
3. Port scope helper functions.
4. Port feature API modules.

### Detailed implementation steps

1. Create:
- `lib/api/client.ts`
- `lib/api/auth-api.ts`
- `lib/api/orders-api.ts`

2. Port behavior from `src/web-admin/lib/api.ts`:
- auth base URL
- orders base URL
- bearer token injection
- correlation ID header
- 401 handling
- refresh token retry flow
- session-expired event emission

3. Create:
- `lib/permissions/authz.ts`

4. Port current permission helpers:
- scope parsing
- `canViewAllOrders`
- admin checks
- feature-level checks used by UI buttons and guards

5. Port feature clients:
- orders client
- admin users client
- admin roles client

### Deliverables

- page logic can call backend APIs without duplicating transport logic

### Definition of Done

- all required frontend API modules compile in `web-app`
- interceptors are centralized and reused

## Phase 10: Implement The Orders List Page

### Goal

Migrate the highest-value business page first.

### Tasks

1. Port orders types.
2. Port orders hooks.
3. Rebuild orders list UI using MUI.
4. Preserve filtering and scope-based action behavior.

### Detailed implementation steps

1. Create:
- `types/orders.ts`
- `hooks/use-orders.ts`

2. Port the contracts from:
- `src/web-admin/types/orders.ts`
- `src/web-admin/lib/orders-api.ts`

3. Rebuild the page:
- `app/(dashboard)/orders/page.tsx`

4. Build the page in this order:

Step 10.1:
- show page header
- show loading state
- show error state

Step 10.2:
- render summary line (title + count)
- add refresh button

Step 10.3:
- add URL-driven status filters
- preserve querystring behavior

Step 10.4:
- add the orders table
- map current columns:
  - order number
  - status
  - customer
  - total
  - items
  - date
  - actions

Step 10.5:
- add row action menu
- keep at least:
  - view details

Step 10.6:
- add scope-aware `New Order` button
- enabled only when user has `orders.write` or `orders.manage`
- disabled with explanatory tooltip/message otherwise

### Recommended MUI components

- `Card`
- `Stack`
- `Button`
- `Tooltip`
- `Chip`
- `Table`
- `TableHead`
- `TableBody`
- `TableRow`
- `TableCell`
- `Menu`
- `MenuItem`
- `CircularProgress` or `Skeleton`

### Deliverables

- feature-equivalent orders list page

### Definition of Done

- filters update the URL
- orders load
- refresh works
- new-order button state matches scopes

## Phase 11: Implement The Order Detail Page

### Goal

Port the order detail view without changing the backend mapping behavior.

### Tasks

1. Port or reuse existing detail mapping logic.
2. Rebuild the page with MUI sections.
3. Preserve the current field interpretation.

### Detailed implementation steps

1. Build:
- `hooks/use-order.ts`
- `app/(dashboard)/orders/[id]/page.tsx`

2. Preserve mapping behavior from current `orders-api.ts`, especially:
- `fullName` -> display name
- `productSku` -> SKU
- `lineTotal` -> row total
- status history mapping
- shipping method fallback

3. Build the UI in sections:

Step 11.1:
- page header with order number and status

Step 11.2:
- summary card:
  - order number
  - status
  - created/updated dates

Step 11.3:
- customer card

Step 11.4:
- items table/card

Step 11.5:
- totals card

Step 11.6:
- shipping card

Step 11.7:
- status history/timeline card

### Deliverables

- equivalent order detail rendering

### Definition of Done

- the current backend payload renders correctly in the new UI
- no critical field regressions exist

## Phase 12: Implement The New Order Page

### Goal

Port the order creation flow with parity on validation and redirect behavior.

### Tasks

1. Rebuild the form in MUI.
2. Preserve validation rules.
3. Preserve submit and redirect behavior.

### Detailed implementation steps

1. Build:
- `components/orders/new-order-form.tsx`
- `app/(dashboard)/orders/new/page.tsx`

2. Guard access using scopes:
- `orders.write`
- `orders.manage`

3. Rebuild the form sections:
- customer information
- order items
- quantities
- prices

4. Preserve form validations:
- required customer name
- required customer email
- valid numeric fields

5. On submit:
- call the existing create order API
- redirect back to `/orders`
- show success feedback

### Deliverables

- working order create flow

### Definition of Done

- users with the right scope can create an order
- users without the right scope are blocked
- successful submission returns to the orders list

## Phase 13: Implement The Users Management Page

### Goal

Port the user administration area with full functional parity.

### Tasks

1. Port users types and API client.
2. Rebuild the list page.
3. Rebuild the create/edit/roles/reset dialogs.
4. Preserve all guardrails.

### Detailed implementation steps

1. Create:
- `types/admin-users.ts`
- `hooks/use-users.ts`
- `components/users/users-table.tsx`
- `components/users/create-user-dialog.tsx`
- `components/users/edit-user-dialog.tsx`
- `components/users/user-roles-dialog.tsx`
- `components/users/reset-password-dialog.tsx`
- `app/(dashboard)/users/page.tsx`

2. Build the page in this order:

Step 13.1:
- page header and summary badges

Step 13.2:
- filters:
  - search
  - status
  - role
  - clear/apply

Step 13.3:
- table with actions

Step 13.4:
- create user dialog

Step 13.5:
- edit user dialog

Step 13.6:
- roles dialog

Step 13.7:
- reset password dialog

Step 13.8:
- enable/disable action

3. Preserve guardrails:
- self-disable blocked
- inactive roles cannot be assigned
- backend errors surface cleanly

### Recommended MUI components

- `Dialog`
- `TextField`
- `Checkbox`
- `FormControlLabel`
- `Select`
- `Chip`
- `Table`
- `Alert`

### Deliverables

- full users management parity

### Definition of Done

- user list/filter/create/edit/status flows all work

## Phase 14: Implement The Roles & Permissions Page

### Goal

Port the roles and permissions administration area with a clearer MUI layout.

### Tasks

1. Port roles types and API client.
2. Rebuild the two-column page.
3. Preserve metadata save and scope save flows.

### Detailed implementation steps

1. Create:
- `types/admin-roles.ts`
- `hooks/use-roles.ts`
- `components/roles/roles-list.tsx`
- `components/roles/role-details-panel.tsx`
- `components/roles/role-scope-editor.tsx`
- `components/roles/create-role-dialog.tsx`
- `app/(dashboard)/users/roles/page.tsx`

2. Build the page in this order:

Step 14.1:
- page header
- refresh button
- create role button

Step 14.2:
- left column roles list with search

Step 14.3:
- right column role details panel

Step 14.4:
- right column scope editor grouped by scope group

Step 14.5:
- save metadata flow

Step 14.6:
- save scopes flow

Step 14.7:
- reset changes flow

3. Preserve current semantics:
- role names immutable in v1
- soft-disable role via `isActive`
- scope replacement is explicit

### Deliverables

- full roles and permissions parity

### Definition of Done

- a role can be selected, edited, and saved
- scope updates persist correctly

## Phase 15: Replace The Dashboard Placeholder With A Real `web-app` Dashboard

### Goal

Do not carry the existing placeholder dashboard forward.

### Tasks

1. Build a real landing page using the new shell.
2. Use current project data, not template mocks.
3. Keep the first version simple but useful.

### Recommended v1 dashboard content

- total orders
- active pipeline orders
- total users
- active users
- total roles
- recent orders
- quick actions:
  - view orders
  - create order
  - manage users
  - manage roles

### Detailed implementation steps

1. Create:
- `components/dashboard/summary-cards.tsx`
- `components/dashboard/recent-orders.tsx`
- `components/dashboard/quick-actions.tsx`
- `app/(dashboard)/dashboard/page.tsx`

2. First make it static with real layout and placeholder data wiring.

3. Then wire real data from existing endpoints.

4. If needed, use a temporary composition of existing endpoints rather than blocking on a new backend dashboard endpoint.

5. If a new backend dashboard endpoint is later added, the UI can be refactored to consume it in a separate task.

### Deliverables

- the dashboard is no longer a placeholder concept in the new app

### Definition of Done

- `/dashboard` is a meaningful landing page and a valid home route after login

## Phase 16: Add Global UX Polish

### Goal

Make the new app feel complete and consistent before formal testing.

### Tasks

1. Normalize loading states.
2. Normalize error states.
3. Normalize empty states.
4. Add success/error notifications consistently.
5. Audit responsive behavior.

### Detailed implementation steps

1. Create reusable page-state components:
- loading section
- error alert panel
- empty-state card

2. Ensure each page follows the same pattern:
- loading
- error
- content
- empty

3. Add consistent page headers:
- title
- subtitle
- actions on the right

4. Ensure dialogs, cards, and table spacing are consistent across features.

### Deliverables

- coherent UX system across all pages

### Definition of Done

- feature pages no longer look like individually styled prototypes

## Phase 17: Port And Expand Test Coverage

### Goal

Validate parity through behavior-focused tests.

### Tasks

1. Recreate critical Playwright scenarios for `web-app`.
2. Add helper/unit tests for complex mapping and guard logic.
3. Validate responsive basics manually.

### Detailed implementation steps

1. Add Playwright tests for:
- login page renders
- protected routes redirect
- callback flow success/failure
- orders page loads
- orders filtering updates querystring
- new order creation path
- order detail render
- users management create/disable
- roles permissions update flow

2. Keep tests userspace-focused:
- query visible text
- query roles
- query test IDs only where necessary
- avoid tying tests to MUI internals

3. Add unit tests where it matters:
- order detail mapping
- scope parsing
- permission checks
- route-guard behavior

### Deliverables

- enough automated coverage to compare `web-admin` and `web-app` behavior

### Definition of Done

- critical flows are covered and passing

## Phase 18: Update Docs For The New Frontend

### Goal

Make the repository documentation reflect the migration reality.

### Tasks

1. Add a dedicated `web-app` doc.
2. Update architecture docs.
3. Update development and deployment docs.

### Detailed implementation steps

1. Add:
- `docs/WEB-APP.md`

Recommended contents:
- app purpose
- setup instructions
- env vars
- route structure
- auth flow
- component conventions

2. Update `docs/ARCHITECTURE.md`:
- mention temporary dual frontend state
- mention target migration to `web-app`

3. Update `docs/DEVELOPMENT.md`:
- add `src/web-app` startup instructions
- add local env examples

4. Update `docs/FRONTEND.md`:
- split stable current app vs target app notes

5. Update `docs/DEPLOYMENT.md`:
- document how `web-app` fits into compose/deployment

### Deliverables

- implementation docs match actual repo state

### Definition of Done

- a new contributor can discover how to run and understand `web-app`

## Phase 19: Cutover Preparation

### Goal

Prepare the repository and deployment configuration for switching the active frontend.

### Tasks

1. Decide how both frontends will coexist in development.
2. Decide which frontend the `client` service points to.
3. Prepare rollback strategy.

### Detailed implementation steps

1. During the transition:
- keep `web-admin` runnable
- keep `web-app` runnable

2. If Docker is used:
- add a separate `web-app` service or switch the `client` target only when ready

3. Review redirect URI registration:
- ensure the new app URL is registered with the `web-admin` OAuth client or create a new client if required

4. Do not remove `web-admin` until:
- parity is confirmed
- deployment is validated
- rollback is documented

### Deliverables

- low-risk cutover path

### Definition of Done

- switching to `web-app` does not leave the team without a quick rollback

## Suggested Granular Task List (Step-By-Step)

This is the practical build order if implemented as a ticket sequence.

1. Create `src/web-app` Next.js project.
2. Install MUI runtime dependencies and configure root layout providers.
3. Add theme files and define the initial design tokens.
4. Create the app shell:
- sidebar
- header
- content container
5. Add the v1 route skeletons.
6. Port the auth provider and OAuth2 PKCE service.
7. Add route guards and unauthorized handling.
8. Build the login page UI.
9. Build the callback page UI and logic.
10. Add the account menu and logout action in the shell.
11. Port shared axios clients and permission helpers.
12. Port orders types and hooks.
13. Build the orders list page.
14. Build the order detail page.
15. Build the new-order page.
16. Port users API/types and build the users page.
17. Build the create/edit/roles/reset dialogs for users.
18. Port roles API/types and build the roles page.
19. Build the role scope editor.
20. Replace the dashboard with a real landing page.
21. Standardize loading/error/empty states.
22. Add Playwright coverage for core flows.
23. Update docs.
24. Prepare cutover/deployment changes.

## Suggested Acceptance Checklist

Use this as the final parity checklist before cutover.

### Shell and routing

- `web-app` boots successfully
- authenticated routes use the MUI shell
- sidebar works on desktop and mobile
- active nav state is correct

### Authentication

- login initiates OAuth2 redirect
- callback completes and stores tokens
- `userinfo` loads
- token refresh works
- logout works
- unauthorized route works

### Orders

- orders list loads
- status filter updates URL
- new order CTA respects scopes
- order detail renders correctly
- order creation works and redirects

### Users

- list/search/filter works
- create user works
- edit user works
- update roles works
- reset password works
- enable/disable works with self-disable protection

### Roles

- roles list loads
- role details load
- create role works
- metadata updates work
- scope updates work

### Dashboard

- `/dashboard` is real and useful
- it is the default post-login landing page

## Implementation Risks To Watch During Coding

### Risk 1: Mixing visual migration with logic rewrites

If a page is redesigned and its auth/API logic is refactored at the same time, debugging becomes slower.

Recommended mitigation:
- port behavior first
- improve structure only after the page works

### Risk 2: Over-copying the Minimal template

The local template contains demo architecture, mock data, and route assumptions that do not belong in this project.

Recommended mitigation:
- port only layout and styling patterns
- rebuild routing and app state specifically for Next App Router and this domain

### Risk 3: Leaving fake nav in place

If placeholder nav items are added before real pages, the new app will feel incomplete.

Recommended mitigation:
- only add routes that actually exist in v1

### Risk 4: Hidden auth regressions

UI work can obscure session or refresh bugs.

Recommended mitigation:
- validate auth loop early, before feature pages

## Recommended First Three Implementation Sessions

If you want a practical start sequence, use this:

### Session 1

- Create `src/web-app`
- Set up Next + MUI providers
- Add theme
- Build the shell and sidebar
- Add route skeletons

Target outcome:
- the app runs and navigation works

### Session 2

- Port auth service
- Port auth context
- Add route guards
- Build login and callback pages
- Add logout/account menu

Target outcome:
- full login flow works

### Session 3

- Port shared API layer
- Build orders list page
- Build order detail page
- Start new-order page

Target outcome:
- first real business feature is live in the new app

## Final Recommendation

The most effective implementation order is:

1. foundation
2. sidebar/shell
3. auth
4. login/callback
5. shared API
6. orders
7. users
8. roles
9. dashboard
10. tests
11. docs
12. cutover

That sequence gives you a visible, testable application early, keeps risk concentrated in small layers, and lets you migrate the highest-value functionality first without destabilizing the current `web-admin`.
