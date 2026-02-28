# Frontend Integration Notes (`src/web-admin`)

## API Clients

Frontend HTTP clients are defined in `src/web-admin/lib/api.ts`:

- `authApi` -> `NEXT_PUBLIC_AUTH_URL`
- `ordersApi` -> `NEXT_PUBLIC_ORDERS_URL`

Shared behavior:

- Bearer token injection from `localStorage`
- Correlation ID header (`X-Correlation-ID`)
- 401 handling with refresh token flow against `AuthServer`
- Session-expired event emission (`authEvents`)
- Refresh token responses persist granted scopes in `localStorage` (`grantedScopes`) for scope-based UI guards

## Auth Flow

- OAuth2 Authorization Code + PKCE
- Token exchange on `AuthServer /connect/token`
- User profile via `AuthServer /connect/userinfo`
- Frontend explicitly requests OIDC scopes including `roles` for role-name display in UI
- Frontend page/feature access should use scope-based checks (`AuthGuard.requiredScopes`) backed by granted token scopes

## Orders Flow

- UI calls `Orders` endpoints directly via `ordersApi`
- Bearer token is attached by shared interceptors
- Admin-like UI behavior should prefer permission scopes (`orders.manage` / `admin.full`) over role-name checks
- Sidebar uses a single Orders navigation entry; status filtering is handled in-page on `/orders`
- Orders list filters are URL-driven (for example `?status=3`)
- `New Order` page route is `/orders/new` and is scope-gated by `orders.write` or `orders.manage`
- If the user lacks create scopes, the Orders page shows a disabled `New Order` button with an explanatory tooltip/message

## Roles & Permissions UI

- Route: `/users/roles`
- Backend API: `AuthServer /api/admin/roles*` via `authApi`
- Frontend gate: `admin.roles` or `admin.full`
- UI edits:
  - create role
  - soft-disable/enable role (`isActive`)
  - replace role permission scopes via grouped collapsible checklist

## Users Management UI

- Route: `/users`
- Backend API: `AuthServer /api/admin/users*` via `authApi`
- Frontend gate: `admin.users` or `admin.full`
- UI operations:
  - list/search/filter users
  - create user (requires at least one active role)
  - edit email/phone
  - replace roles (inactive roles blocked)
  - enable/disable user (self-disable blocked in UI/API)
  - reset password (token-based reset semantics server-side)
- `User Activity` placeholder route:
  - `/users/activity`
  - same scope gate (`admin.users` or `admin.full`)

## Required Browser Env Vars

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
NEXT_PUBLIC_APP_BASE_URL=http://localhost:3000
```
