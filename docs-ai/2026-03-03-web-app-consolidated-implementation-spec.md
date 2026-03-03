# Web App Consolidated Implementation Spec

Date: 2026-03-03
Status: Finalized planning baseline
Target app: `src/web-app`
Current app during migration: `src/web-admin`

Supersedes as the execution baseline:
- `docs-ai/2026-03-01-web-app-mui-migration-plan.md`
- `docs-ai/2026-03-01-web-app-detailed-implementation-plan.md`

## Purpose

This document consolidates the migration plan plus all decisions made afterward.

It removes open choices and sets a fixed implementation baseline so work can start without further planning churn.

## Locked Product Decisions

### v1 Scope

`web-app` v1 includes:
- `/`
- `/login`
- `/callback`
- `/unauthorized`
- `/dashboard`
- `/profile`
- `/orders`
- `/orders/new`
- `/orders/[id]`
- `/users`
- `/users/roles`

Deferred from v1:
- `/users/activity`
- `/admin`
- settings pages and other non-implemented submenu routes

### New OAuth client

Use a new dedicated OAuth client for `web-app`.

Client ID:
- `web-app`

Development redirect URIs to register:
- `http://localhost:3000/callback`
- `http://localhost:3001/callback`

Callback path:
- `/callback`

### Dashboard v1

The dashboard must be real in v1, but intentionally minimal.

v1 dashboard includes:
- quick actions
- recent orders section
- basic counts

v1 dashboard excludes:
- charts
- heavier aggregation work
- new backend dashboard endpoints

Initial dashboard implementation rule:
- build the final layout first with static placeholder values
- wire live data afterward

### Notifications

Use Material UI `Snackbar` from the start.

### Table strategy

Use standard MUI `Table` components in v1.

Do not introduce MUI X `DataGrid` in the first migration pass.

### Design direction

Follow the Minimal template strongly for visual direction:
- layout
- spacing
- shell composition
- general look and feel

But adapt:
- routes
- content
- product naming
- auth behavior

### Forms

Keep:
- `react-hook-form`
- `zod`

### Assets

Reuse selected Minimal assets directly where useful.

Do not bulk-copy the template.

### Tailwind

`web-app` is a pure MUI frontend:
- no Tailwind in `src/web-app`
- use MUI theme
- use `sx`
- allow only small targeted CSS where needed

### Icons

Use:
- `@iconify/react`

This keeps icon usage aligned with the Minimal template.

### Data fetching

Use simple local hooks in v1:
- `useEffect`
- `useState`
- `useCallback`

Do not add:
- React Query
- SWR

### Backend changes

This is a frontend-first migration.

Rule:
- do not change backend contracts in v1 unless a real blocker appears
- the only planned backend-side change in scope is adding the new OAuth client

### TypeScript strictness

Use moderate TypeScript strictness for the initial build.

Practical meaning:
- do not start with an overly loose setup
- do not let strictness block migration velocity unnecessarily

## Locked UX Decisions

### Theme mode

Support both:
- light mode
- dark mode

Default behavior:
- follow system preference initially
- persist the user's manual choice after they toggle

### Branding

Use temporary Minimal-based branding in v1, but label the app as your product.

### Header

Keep the header minimal in v1.

Include:
- mobile nav trigger
- theme toggle
- account menu

Do not include in v1:
- global search
- notifications center UI
- language switcher
- workspace switcher

### Account menu

The account menu is limited to the practical personal actions.

Include:
- `Profile`
- theme toggle
- `Logout`

Do not add extra shortcuts in the account menu.

### Profile page

Route:
- `/profile`

Exposure:
- linked only from the account menu
- not shown in the sidebar

Access:
- any authenticated user

Data source in v1:
- existing auth context / stored authenticated user state
- existing granted scopes already loaded by the app

Profile contents in v1:
- name
- email
- roles
- active status
- created date
- granted scopes
- logout action

### Root route behavior

Use final behavior from day one:
- unauthenticated users go to `/login`
- authenticated users go to `/dashboard`

### Dashboard quick actions

Show these actions in v1:
- `View Orders`
- `New Order`
- `Manage Users`
- `Manage Roles`

## Locked Navigation and WIP Behavior

### Sidebar visibility

Show all core v1 routes in the sidebar from the start:
- `Dashboard`
- `Orders`
- `Users`
- `Roles & Permissions`

Do not show deferred routes in the sidebar.

### WIP route handling

For unfinished but visible core pages:
- keep the route accessible
- keep auth and scope checks enforced
- render a simple placeholder page state

Allowed placeholder states:
- `Coming soon`
- `Not implemented yet`
- lightweight 404-style placeholder if appropriate

Practical rule:
- no more special UX branching for unfinished pages
- just use a simple placeholder page inside the normal protected app shell

### Permission enforcement on WIP pages

Even if a page is still a placeholder, enforce the final access rules from day one.

Examples:
- `/users` still requires `admin.users` or `admin.full`
- `/users/roles` still requires `admin.roles` or `admin.full`

## Locked Local Development and Docker Decisions

### Local ports

Standalone local `web-app`:
- `http://localhost:3001`

Local comparison `web-admin` after the Docker switch:
- `http://localhost:3002`

### Environment variable names

Keep the current naming convention in `web-app`:
- `NEXT_PUBLIC_AUTH_URL`
- `NEXT_PUBLIC_ORDERS_URL`
- `NEXT_PUBLIC_APP_BASE_URL`

### Side-by-side development

During migration:
- keep `web-app` and `web-admin` runnable side-by-side
- but the primary forward path is `web-app`

### Docker `client` strategy

Switch Docker `client` to `web-app` early.

Trigger for early switch:
- once `web-app` has the shell and login working

Docker `client` external URL stays:
- `http://localhost:3000`

After the Docker switch:
- `web-app` is the default Docker-served frontend
- `web-admin` remains local-only when needed on `http://localhost:3002`

## Locked Feature Delivery Order

After base shell and login are working, implement features in this order:

1. `Dashboard`
2. `Profile`
3. `Users`
4. `Roles & Permissions`
5. `Orders`

### Completion standard per feature

`Users` first completed version must be full parity:
- list
- search/filter
- create user
- edit user
- assign roles
- reset password
- enable/disable

`Roles & Permissions` first completed version must be full parity:
- roles list
- search
- create role
- metadata editing
- active toggle
- grouped scope editing
- save/reset flows

`Orders` first completed version must be full parity:
- orders list
- status filters
- refresh
- scope-aware new-order CTA
- order detail
- new-order flow

## Locked Architecture Decisions

### Bootstrap strategy

Create `web-app` as a fresh Next.js app from scratch.

Do not:
- clone `src/web-admin` as the starting point
- build a hybrid Tailwind + MUI app

### Shared logic strategy

Keep `web-app` self-contained for v1.

Rule:
- copy/adapt needed auth, API, and type logic into `web-app`
- do not extract a shared frontend package during the migration

### Auth model

Keep the current client-side auth model in v1:
- localStorage/sessionStorage-based token handling
- client-side auth context
- client-side route guards

Do not redesign into a server-first auth model during this migration.

## Recommended `web-app` Folder Baseline

Use this as the working baseline:

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
      profile/
      layout.tsx
    unauthorized/
    page.tsx
    layout.tsx
  components/
    app-shell/
    auth/
    common/
    dashboard/
    orders/
    users/
    roles/
    profile/
  contexts/
    auth-context.tsx
  hooks/
  lib/
    api/
    auth/
    permissions/
  theme/
  types/
```

## Final Implementation Sequence

This is the fixed execution order.

### Phase 1: Create `web-app`

Create a fresh Next.js App Router project in `src/web-app`.

Initial goal:
- app boots independently
- no feature work yet

### Phase 2: Set up MUI foundation

Implement:
- `@mui/material-nextjs` App Router integration
- theme provider
- `CssBaseline`
- light/dark mode support
- persisted theme selection

### Phase 3: Build the Minimal-inspired shell

Implement:
- sidebar
- mobile drawer
- header
- content container
- route structure

Initial sidebar should show:
- Dashboard
- Orders
- Users
- Roles & Permissions

### Phase 4: Build auth core

Port into `web-app`:
- OAuth2 PKCE service
- auth context
- token refresh behavior
- permission helpers
- route guards

### Phase 5: Build the polished login flow

Before Docker switch, the login page should already be visually polished.

Implement:
- Minimal-inspired login layout
- login button that triggers the existing auth redirect flow
- callback page
- unauthorized page
- account logout behavior

### Phase 6: Early Docker switch

Once the shell and login work:
- switch Docker `client` to `web-app`
- keep Docker URL at `http://localhost:3000`
- keep standalone local `web-app` at `http://localhost:3001`
- keep local `web-admin` comparison available at `http://localhost:3002` when needed

### Phase 7: Build `Dashboard`

Implement the actual dashboard route with:
- final page shell
- quick actions
- basic counts
- recent orders section

Initial data can be static placeholders.

Do not add charts in v1.

### Phase 8: Build `Profile`

Implement a read-only profile page using existing auth state.

Include:
- identity details
- roles
- granted scopes
- logout action

### Phase 9: Build `Users`

Move `/users` from placeholder to full parity implementation.

Until complete:
- use the simple placeholder page state
- keep the final permission checks active

### Phase 10: Build `Roles & Permissions`

Move `/users/roles` from placeholder to full parity implementation.

Until complete:
- use the simple placeholder page state
- keep the final permission checks active

### Phase 11: Build `Orders`

Implement the entire Orders feature as one continuous block:
- `/orders`
- `/orders/[id]`
- `/orders/new`

Do not split this into partial completion phases.

### Phase 12: Replace static dashboard data with live data

Once core feature pages exist, wire the dashboard to live existing endpoints where appropriate.

Still no backend dashboard endpoint in v1 unless a blocker forces it.

### Phase 13: Add reusable small primitives

Implement lightweight shared primitives only where they help reduce repetition.

Approved shared abstractions:
- small shared table primitives
- small shared form/dialog primitives

Do not build a heavy framework up front.

### Phase 14: Add test coverage

Create a separate Playwright test set for `web-app`.

Keep existing `web-admin` tests intact.

### Phase 15: Update docs and deployment notes

Update the repo docs to reflect:
- `web-app` as the active frontend target
- early Docker switch behavior
- local ports
- new OAuth client

## Feature Implementation Notes

### Dashboard implementation rule

First version:
- static counts
- static recent items if needed
- real layout and interactions

Second pass:
- wire live data

### Placeholder page rule

For unfinished visible pages, do not over-design the placeholder.

Use one consistent card or section that clearly states:
- `Coming soon`
or
- `This page is still being migrated`

That is enough.

### Orders implementation rule

When Orders starts, finish all of it before moving to final polish.

### Profile implementation rule

Do not introduce extra backend calls unless they are necessary.

Use the already loaded auth state first.

## Non-Goals for v1

Do not add these in the initial migration unless forced by a blocker:
- backend dashboard endpoint
- MUI X DataGrid
- React Query / SWR
- shared frontend package across apps
- Tailwind in `web-app`
- server-first auth redesign
- full settings area
- admin demo routes
- charts on the dashboard

## Acceptance Baseline

This is the minimum bar for the consolidated plan to be considered complete.

### Platform and shell

- `web-app` runs as a standalone Next app
- MUI is integrated correctly with App Router
- light/dark mode works
- theme choice persists
- sidebar and mobile nav work

### Auth

- login works
- callback works
- new `web-app` OAuth client is used
- logout works
- unauthorized handling works

### Routing

- `/` routes correctly from day one
- all core v1 routes exist
- unfinished routes show a simple placeholder page
- final scope checks are enforced even on placeholders

### Features

- dashboard is real (minimal but present)
- profile is real
- users reaches full parity
- roles reaches full parity
- orders reaches full parity

### Delivery model

- Docker `client` points to `web-app` early
- `web-admin` remains available locally when needed
- `web-app` remains self-contained

## Final Summary

The execution baseline is now fixed:

- build a fresh `src/web-app`
- pure MUI, strongly Minimal-inspired
- new OAuth client `web-app`
- early Docker switch after shell + login
- dashboard and profile first
- then users, roles, and orders
- use simple placeholder pages for unfinished visible routes
- no more planning variations are required unless a real implementation blocker appears
