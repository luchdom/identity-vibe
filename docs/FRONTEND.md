# Frontend Integration Notes (`src/web-admin` and `src/web-app`)

## Active Frontends

- Legacy/admin baseline: `src/web-admin`
- Migration target and default for new UI work: `src/web-app`

For all new frontend implementation work, prefer `src/web-app` patterns unless a task explicitly targets `web-admin`.

## UX/UI Implementation Expectations

- Implementations must follow best UX/UI practices, not only visual parity.
- Every frontend change should be evaluated for:
  - clarity of actions and labels
  - consistency with existing page patterns
  - accessibility and readable density
  - predictable behavior for filters, forms, and table actions
- If requirements are ambiguous or a visual/interaction choice can meaningfully change usability, the implementing agent must ask the prompter for clarification before finalizing the UI.
- When in doubt, prefer consistency with the locked `src/web-app` UI System Rules in this document.

## Shared API/Auth Model

Both frontends follow the same backend integration model:

- OAuth2 Authorization Code + PKCE
- Token exchange via `AuthServer /connect/token`
- User profile via `AuthServer /connect/userinfo`
- Frontend explicitly requests OIDC scopes including `roles` for role-name display
- Feature/page access is scope-gated (`AuthGuard.requiredScopes`) using granted token scopes

Shared client behavior:

- Bearer token injection from `localStorage`
- Correlation ID header (`X-Correlation-ID`)
- 401 handling with refresh token flow against `AuthServer`
- Session-expired event emission (`authEvents`)
- Refresh responses persist granted scopes in `localStorage` (`grantedScopes`)

## Feature Routes And Gates (Web App)

- Orders (`/orders`, `/orders/new`, `/orders/[id]`)
  - API: `Orders` service via `ordersApi`
  - create gate: `orders.write` or `orders.manage` (or admin full)
- Users (`/users`)
  - API: `AuthServer /api/admin/users*` via `authApi`
  - gate: `admin.users` or `admin.full`
- Roles (`/users/roles`)
  - API: `AuthServer /api/admin/roles*` via `authApi`
  - gate: `admin.roles` or `admin.full`
- Profile (`/profile`)
  - read-only v1 page, available to authenticated users

## UI System Rules (Default For Future Implementations)

These rules are the project default for `src/web-app` list/admin screens.

### Density and Form Factor

- Default density is **compact / Minimal-style**
- If a choice is compact vs medium, default to compact
- Default control height in list/filter toolbars: **36px**
- Default radius: subtle (`6px` to `8px`)

### Control Styling

- Default variant for list/filter/toolbars: `outlined`
- Default size for list/filter/toolbars: `small`
- Toolbar inputs use **placeholders only** (no floating labels)
- Empty dropdown text should use placeholder tone (`text.secondary`)
- Selected dropdown text should use normal text tone

### List Page Header Pattern

- Compact header with:
  - page title
  - compact breadcrumb line
  - right-aligned primary action
- Avoid large subtitle blocks on list pages by default

### Primary Actions

- Default list-page primary action button:
  - `variant="contained"`
  - `color="inherit"`
- Keep list-page primary actions compact (`small`)

### Filter Toolbar Pattern

- Default pattern:
  - search input
  - filter icon button
  - optional clear button
- Prefer this over explicit `Apply` + `Clear` button pairs for list toolbars

### Tabs and Table Patterns

- Status tabs:
  - minimal tabs
  - thin indicator
  - soft compact count badges
- Table defaults:
  - compact rows
  - sortable headers where relevant
  - rows-per-page selector in list footer

### Row Actions

- Default row action pattern:
  - quick edit icon/button
  - 3-dot overflow menu for secondary actions
- Do not use many inline text action buttons in list rows

### Labels/Badges

- Use **soft labels** as default for status and role tags
- Avoid heavy outlined badges as the default status/tag presentation

### Form Dialogs (Create/Edit)

- Dialog/forms should use MUI outlined text fields with floating labels
- This rule applies to forms/dialogs, not to list toolbars

### Destructive Actions

- Keep destructive actions in overflow menus by default
- Use danger styling
- Require confirmation for irreversible operations

## Environment Variables

Required browser env vars:

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
NEXT_PUBLIC_APP_BASE_URL=http://localhost:3000
```

Notes:

- During side-by-side migration, `web-app` is commonly run on `http://localhost:3001` in standalone dev mode.
- Docker `client` may still serve the frontend on `http://localhost:3000`.
