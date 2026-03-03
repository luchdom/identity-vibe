# Web App Phased Implementation Tasks

Date: 2026-03-03
Status: Ready for implementation
Primary baseline:
- `docs-ai/2026-03-03-web-app-consolidated-implementation-spec.md`

## Audit Summary

The consolidated implementation spec is coherent and can be executed without more product clarification.

I do not see any blocker that requires new decisions before implementation starts.

There are only two execution-level adjustments that should be made while implementing:

1. Treat OAuth client registration and Docker wiring as prerequisites for the early Docker switch.
2. Build the reusable WIP placeholder page pattern before switching Docker, so visible but unfinished routes do not fall into accidental 404s.

These are sequencing refinements, not scope changes.

## Execution Model

This plan is structured for gradual delivery.

Each phase has:
- objective
- scope
- concrete tasks
- exit criteria

Recommended rule:
- do not begin the next phase until the current phase has a working, manually testable result

## Phase 0: Preflight and Repository Preparation

### Objective

Prepare the repository and migration lane so `web-app` can be added cleanly.

### Scope

- no end-user functionality yet
- repo-level setup only

### Tasks

1. Verify the target path is unused or safe to create:
- `src/web-app`

2. Capture the current frontend baseline for comparison:
- current `web-admin` routes
- current auth env vars
- current Docker `client` service behavior

3. Identify all files that will later need Docker and docs updates:
- `docker-compose.yml`
- `docker-compose.dev.yml`
- `docker-compose.prod.yml`
- `docs/DEVELOPMENT.md`
- `docs/DEPLOYMENT.md`
- `docs/FRONTEND.md`
- `docs/ARCHITECTURE.md`

4. Add a short implementation tracker note or checklist section in the phase doc if you want progress tracking by hand.

### Exit Criteria

- implementation lane is clear
- no uncertainty remains about where `web-app` will be created

## Phase 1: Scaffold the Base Next App

### Objective

Create a clean standalone Next.js App Router project for `web-app`.

### Scope

- base project only
- app boots
- no real business features yet

### Tasks

1. Create `src/web-app`.

2. Initialize a fresh Next.js App Router project with:
- TypeScript
- ESLint
- app directory

3. Add the baseline files:
- `src/web-app/package.json`
- `src/web-app/tsconfig.json`
- `src/web-app/next.config.ts`
- `src/web-app/app/layout.tsx`
- `src/web-app/app/page.tsx`
- `src/web-app/app/globals.css`

4. Add runtime dependencies:
- `next`
- `react`
- `react-dom`
- `@mui/material`
- `@emotion/react`
- `@emotion/styled`
- `@mui/material-nextjs`
- `@iconify/react`
- `axios`
- `react-hook-form`
- `zod`
- `@hookform/resolvers`

5. Add any minimal utility dependencies needed for parity:
- `dayjs` if useful for date formatting

6. Ensure `npm run dev` boots the new app independently.

### Exit Criteria

- `src/web-app` runs on its own
- the project builds without feature code

## Phase 2: Establish the MUI Foundation

### Objective

Set up Material UI correctly for Next App Router before any shell or feature work.

### Scope

- provider stack
- theme
- light/dark mode foundation

### Tasks

1. In `src/web-app/app/layout.tsx`, integrate:
- `AppRouterCacheProvider` from `@mui/material-nextjs/v15-appRouter`
- `ThemeProvider`
- `CssBaseline`

2. Create the theme structure:
- `src/web-app/theme/index.ts`
- optional split files for palette, typography, component defaults if useful

3. Define a Minimal-aligned design baseline:
- palette
- surface colors
- spacing rhythm
- border radius
- typography scale

4. Implement theme mode support:
- light mode
- dark mode
- default from system preference
- persist manual choice in browser storage

5. Add a theme-mode context or utility if needed for toggling from the account menu.

6. Verify hydration stability:
- no SSR/client styling mismatch
- no flash beyond acceptable theme initialization behavior

### Exit Criteria

- a plain MUI page renders correctly
- theme mode can switch and persist

## Phase 3: Build the App Shell

### Objective

Create the persistent layout system that all protected pages will use.

### Scope

- sidebar
- mobile drawer
- top header
- content container

### Tasks

1. Create shell components:
- `components/app-shell/app-shell.tsx`
- `components/app-shell/sidebar.tsx`
- `components/app-shell/header.tsx`
- `components/app-shell/nav-config.ts`

2. Build the sidebar using MUI primitives:
- `Drawer`
- `List`
- `ListItemButton`
- `Box`
- `Divider`

3. Build the header with only v1-approved items:
- mobile nav trigger
- theme toggle
- account menu

4. Add the account menu with these actions only:
- `Profile`
- theme toggle
- `Logout`

5. Add active-route highlighting for:
- `Dashboard`
- `Orders`
- `Users`
- `Roles & Permissions`

6. Create the protected route group layout:
- `src/web-app/app/(dashboard)/layout.tsx`

7. Ensure the shell works on:
- desktop
- mobile

### Exit Criteria

- protected placeholder pages can render inside the shell
- navigation and layout are visually stable

## Phase 4: Add Core Route Skeletons and Placeholder Pattern

### Objective

Create the full route tree and the standard WIP placeholder used for unfinished pages.

### Scope

- route existence
- placeholder consistency
- no 404s for planned visible routes

### Tasks

1. Add the route skeletons:
- `app/page.tsx`
- `app/(auth)/login/page.tsx`
- `app/(auth)/callback/page.tsx`
- `app/unauthorized/page.tsx`
- `app/(dashboard)/dashboard/page.tsx`
- `app/(dashboard)/profile/page.tsx`
- `app/(dashboard)/orders/page.tsx`
- `app/(dashboard)/orders/new/page.tsx`
- `app/(dashboard)/orders/[id]/page.tsx`
- `app/(dashboard)/users/page.tsx`
- `app/(dashboard)/users/roles/page.tsx`

2. Create a shared placeholder state component:
- `components/common/page-placeholder.tsx`

3. Use a consistent placeholder message for unfinished routes:
- `Coming soon`
or
- `This page is still being migrated`

4. Ensure all visible core routes render a real page shell, even if content is placeholder-only.

5. Keep `/profile` out of the sidebar even though the route exists.

### Exit Criteria

- every core v1 route exists
- visible but unfinished pages do not 404

## Phase 5: Port Auth Core and Route Protection

### Objective

Move the current auth model into `web-app` with behavior preserved.

### Scope

- OAuth2 PKCE
- auth context
- route guards
- unauthorized handling

### Tasks

1. Port the OAuth2 PKCE service:
- `lib/auth/oauth2-auth.ts`

2. Preserve current behaviors:
- `authorize()`
- `handleCallback()`
- `refreshToken()`
- `logout()`
- access token storage
- refresh token storage
- granted scopes storage
- `userinfo` loading

3. Port the auth context:
- `contexts/auth-context.tsx`

4. Preserve auth initialization:
- load stored user/token
- refresh if needed
- session-expired behavior

5. Create the auth guard:
- `components/auth/auth-guard.tsx`

6. Support:
- unauthenticated redirect to `/login`
- forbidden redirect to `/unauthorized`
- scope-based protection

7. Apply final scope rules even to placeholder pages.

8. Ensure `/` follows the final behavior from day one:
- unauthenticated -> `/login`
- authenticated -> `/dashboard`

### Exit Criteria

- auth flow works end to end in the new app
- protected routes enforce access correctly

## Phase 6: Build the Polished Login, Callback, and Unauthorized UX

### Objective

Finish the public auth experience before exposing `web-app` as the default Docker frontend.

### Scope

- polished login UI
- callback handling UI
- unauthorized page UX

### Tasks

1. Build the Minimal-inspired login page:
- branded layout
- polished spacing and typography
- sign-in action button

2. Keep auth behavior project-specific:
- do not use Amplify/Auth0 template logic
- only use the existing AuthServer redirect flow

3. Build the callback page UX:
- loading state
- error state
- redirect on success

4. Build the unauthorized page UX:
- clear explanation
- link back to a valid route

5. Add logout behavior from the account menu.

6. If session-expired UI is needed now, add it here with MUI dialog patterns.

### Exit Criteria

- login is visually polished
- callback is stable
- unauthorized state is user-readable

## Phase 7: Infrastructure Readiness and Early Docker Switch

### Objective

Prepare the environment for the planned early switch of Docker `client` to `web-app`.

### Scope

- OAuth client registration prerequisites
- Docker wiring

### Tasks

1. Add or plan the new AuthServer OAuth client:
- client ID: `web-app`
- redirect URIs:
  - `http://localhost:3000/callback`
  - `http://localhost:3001/callback`

2. Ensure `web-app` is configured to use the existing env var names:
- `NEXT_PUBLIC_AUTH_URL`
- `NEXT_PUBLIC_ORDERS_URL`
- `NEXT_PUBLIC_APP_BASE_URL`

3. Update Docker configuration so `client` can point to `web-app`.

4. Keep Docker `client` on:
- `http://localhost:3000`

5. Preserve local comparison behavior:
- standalone `web-app` at `http://localhost:3001`
- local `web-admin` when needed at `http://localhost:3002`

6. Switch Docker `client` only after:
- shell works
- login works
- placeholder routes exist for all visible core pages

### Exit Criteria

- Docker `client` serves `web-app`
- login still works at the Docker URL

## Phase 8: Build the Dashboard (Static First)

### Objective

Deliver a real dashboard page quickly, even before live data is wired.

### Scope

- real layout
- static initial values
- no charts

### Tasks

1. Replace the placeholder dashboard with:
- page header
- quick actions
- basic count cards
- recent orders section shell

2. Build components as needed:
- `components/dashboard/summary-cards.tsx`
- `components/dashboard/quick-actions.tsx`
- `components/dashboard/recent-orders.tsx`

3. Keep initial counts/recent items static placeholder values.

4. Ensure quick actions link correctly to:
- `/orders`
- `/orders/new`
- `/users`
- `/users/roles`

5. Respect scope rules for action availability where needed:
- `New Order` should still reflect real scope gating behavior once auth/permissions are available

### Exit Criteria

- `/dashboard` is a real page
- it is suitable as the authenticated landing route

## Phase 9: Build the Profile Page

### Objective

Deliver the account-view page linked from the account menu.

### Scope

- read-only user profile
- no extra backend dependency

### Tasks

1. Replace the placeholder `/profile` page with a real read-only implementation.

2. Render from existing auth state:
- name
- email
- roles
- active status
- created date
- granted scopes

3. Include a logout action on the page.

4. Ensure only authenticated users can access `/profile`.

5. Keep `/profile` exposed only through the account menu.

### Exit Criteria

- `/profile` is fully functional for v1

## Phase 10: Build Shared API Layer and Permission Utilities

### Objective

Port the non-UI client logic needed for real data-driven features.

### Scope

- axios clients
- interceptors
- permission helpers
- feature API modules

### Tasks

1. Port the shared axios setup:
- auth client
- orders client
- interceptors for bearer token, correlation ID, refresh handling

2. Port session-expired event behavior.

3. Port permission helpers:
- scope parsing
- admin checks
- orders permission checks

4. Port feature API modules:
- orders
- admin users
- admin roles

5. Port types into `web-app` as self-contained copies/adaptations.

### Exit Criteria

- feature pages can call existing backend APIs through shared local modules

## Phase 11: Build the Users Feature (Full Parity)

### Objective

Move `/users` from placeholder to full parity in a single feature block.

### Scope

- list
- filters
- create
- edit
- roles
- reset password
- enable/disable

### Tasks

1. Create the data hooks and types:
- `types/admin-users.ts`
- `hooks/use-users.ts`

2. Build the main page:
- header
- summary indicators
- filters
- table

3. Create small shared primitives if helpful:
- table wrapper
- dialog wrapper

4. Build user dialogs:
- create user
- edit user
- assign roles
- reset password

5. Implement enable/disable actions.

6. Preserve current guardrails:
- self-disable blocked
- inactive roles cannot be assigned

7. Preserve current required scopes:
- `admin.users`
- `admin.full`

### Exit Criteria

- `/users` is fully usable and no longer a placeholder

## Phase 12: Build the Roles & Permissions Feature (Full Parity)

### Objective

Move `/users/roles` from placeholder to full parity in a single feature block.

### Scope

- roles list
- search
- create role
- metadata editing
- active toggle
- grouped scope editing
- save/reset flows

### Tasks

1. Create the data hooks and types:
- `types/admin-roles.ts`
- `hooks/use-roles.ts`

2. Build the two-column layout:
- roles list on the left
- details and scope editor on the right

3. Implement:
- role selection
- search
- create role
- metadata save
- scope save
- reset changes

4. Preserve current semantics:
- role names immutable in v1
- soft-disable via `isActive`
- scope replacement is explicit

5. Preserve required scopes:
- `admin.roles`
- `admin.full`

### Exit Criteria

- `/users/roles` is fully usable and no longer a placeholder

## Phase 13: Build the Orders Feature (Full Parity Block)

### Objective

Implement the full Orders feature as one continuous delivery block.

### Scope

- list
- detail
- new order

### Tasks

1. Port orders types and hooks:
- `types/orders.ts`
- `hooks/use-orders.ts`
- `hooks/use-order.ts`

2. Build `/orders`:
- header
- loading/error states
- refresh
- status filters
- table
- row actions
- scope-aware `New Order` CTA

3. Build `/orders/[id]`:
- summary
- customer
- items
- totals
- shipping
- status history

4. Build `/orders/new`:
- form
- validation
- create request
- redirect back to `/orders`

5. Preserve current scope behavior:
- `orders.write`
- `orders.manage`
- `orders.read`

6. Preserve current backend contract mapping.

### Exit Criteria

- Orders reaches full parity and no longer contains placeholder routes

## Phase 14: Replace Dashboard Placeholder Data with Live Data

### Objective

Upgrade the dashboard from static presentation to live data using existing endpoints.

### Scope

- no new backend dashboard endpoint
- use current APIs only

### Tasks

1. Replace static counts with real values derived from existing endpoints.

2. Replace static recent orders with real recent orders if feasible through current APIs.

3. Keep the dashboard intentionally minimal:
- still no charts
- still no new backend aggregation endpoint unless blocked

4. Ensure dashboard remains fast and not overloaded with cross-page logic.

### Exit Criteria

- dashboard uses real data while staying within existing backend contracts

## Phase 15: Shared UX Polish and Reusable Primitives

### Objective

Normalize the app experience once all core features exist.

### Scope

- loading states
- error states
- empty states
- lightweight shared abstractions

### Tasks

1. Standardize:
- loading sections
- error panels
- empty-state cards

2. Add or refine:
- small shared table primitives
- small shared form/dialog primitives

3. Ensure MUI `Snackbar` feedback is applied consistently where needed.

4. Check spacing, density, and responsiveness across:
- dashboard
- profile
- users
- roles
- orders

### Exit Criteria

- the app feels consistent across all implemented pages

## Phase 16: Testing and Validation

### Objective

Validate the migrated app with a dedicated test lane.

### Scope

- `web-app` Playwright tests
- critical path verification

### Tasks

1. Add a separate Playwright test set for `web-app`.

2. Keep existing `web-admin` tests unchanged.

3. Add coverage for:
- login
- callback
- route protection
- dashboard visibility
- profile rendering
- users full flow
- roles full flow
- orders full flow

4. Add small helper/unit tests where they reduce risk:
- permission helpers
- mapping logic
- route guard decisions

### Exit Criteria

- critical paths are covered and passing for `web-app`

## Phase 17: Docs and Deployment Alignment

### Objective

Bring repository docs and deployment notes in line with the actual migration state.

### Scope

- docs updates
- deployment clarity

### Tasks

1. Add or update frontend docs for `web-app`.

2. Update:
- `docs/ARCHITECTURE.md`
- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`
- `docs/DEPLOYMENT.md`

3. Document:
- `web-app` as the active target frontend
- Docker `client` early-switch behavior
- local ports:
  - `3000`
  - `3001`
  - `3002`
- new OAuth client expectations

### Exit Criteria

- the repo documentation matches the real implementation and dev workflow

## Recommended Gradual Rollout Checkpoints

Use these as natural “pause and validate” points.

### Checkpoint A

After Phase 3:
- app boots
- shell exists
- route skeletons are close

### Checkpoint B

After Phase 6:
- auth and login are stable
- Docker switch is safe

### Checkpoint C

After Phase 9:
- dashboard and profile are real
- the app is credible as the default shell

### Checkpoint D

After Phase 12:
- users and roles are complete
- admin management parity is largely established

### Checkpoint E

After Phase 13:
- orders parity is complete
- all core v1 features are implemented

### Checkpoint F

After Phase 17:
- app is tested
- docs are aligned
- migration baseline is complete

## Recommended Commit or Delivery Boundaries

If you want clean incremental delivery, the practical boundaries are:

1. Base scaffold + MUI foundation
2. Shell + route skeletons + placeholder pattern
3. Auth + login + callback + unauthorized
4. Docker switch prep + Docker switch
5. Dashboard + Profile
6. Shared API layer
7. Users
8. Roles & Permissions
9. Orders
10. Dashboard live data
11. UX polish
12. Tests + docs

## Final Recommendation

The plan is sound. The only meaningful audit correction is to treat the early Docker switch as gated by infrastructure readiness and a finished placeholder pattern, not just by “shell + login” in isolation.

With that adjustment, this phased plan is safe for gradual implementation and should let you ship visible progress early without breaking the migration path.
