# Deployment Notes

This project exposes the frontend and APIs directly:

- `EXTERNAL_CLIENT_URL`
- `EXTERNAL_AUTH_URL`
- `EXTERNAL_ORDERS_URL`

## Required Env Vars

- `POSTGRES_PASSWORD`
- `EXTERNAL_AUTH_URL`
- `EXTERNAL_ORDERS_URL`
- `EXTERNAL_CLIENT_URL`

## Client Runtime Configuration

The `client` container receives:

- `NEXT_PUBLIC_AUTH_URL=${EXTERNAL_AUTH_URL}`
- `NEXT_PUBLIC_ORDERS_URL=${EXTERNAL_ORDERS_URL}`
- `NEXT_PUBLIC_APP_BASE_URL=${EXTERNAL_CLIENT_URL}`

## Common Issues

1. CORS failures: ensure `AuthServer` and `Orders` allow `EXTERNAL_CLIENT_URL`.
2. OIDC redirect mismatch: ensure `web-admin` redirect URI matches `EXTERNAL_CLIENT_URL/callback`.
3. Token validation failures in `Orders`: ensure `Authentication:Authority` points to the same AuthServer URL used for token issuance.

