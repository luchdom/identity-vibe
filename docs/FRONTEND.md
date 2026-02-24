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

## Auth Flow

- OAuth2 Authorization Code + PKCE
- Token exchange on `AuthServer /connect/token`
- User profile via `AuthServer /connect/userinfo`

## Orders Flow

- UI calls `Orders` endpoints directly via `ordersApi`
- Bearer token is attached by shared interceptors

## Required Browser Env Vars

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
NEXT_PUBLIC_APP_BASE_URL=http://localhost:3000
```

