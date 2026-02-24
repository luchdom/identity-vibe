# Project Notes

## Services (development)

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- `web-admin`: `http://localhost:3000`

## Architecture Rules

- Frontend calls `AuthServer` and `Orders` directly (no proxy/BFF).
- OAuth2/OIDC user auth uses Authorization Code + PKCE.
- User profile retrieval uses `/connect/userinfo`.
- Orders endpoints are protected by scope/role policies.

## Local Run

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

## Key Config Files

- `src/web-admin/lib/api.ts`: frontend HTTP clients (`authApi`, `ordersApi`)
- `src/backend/AuthServer/appsettings.Clients.json`: OAuth clients
- `src/backend/AuthServer/appsettings.Scopes.json`: scopes + role scope mapping
- `src/backend/Orders/appsettings*.json`: Orders API config/CORS
