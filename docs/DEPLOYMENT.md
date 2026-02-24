# Deployment Guide

## Runtime Services

- `postgres`
- `authserver`
- `orders`
- `client`

## Compose Commands

Development:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

Production-like:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
```

## Required Environment Variables

- `POSTGRES_PASSWORD`
- `EXTERNAL_AUTH_URL`
- `EXTERNAL_ORDERS_URL`
- `EXTERNAL_CLIENT_URL`

## Important Notes

- Browser traffic goes directly from the client app to `AuthServer` and `Orders`.
- Ensure both APIs allow the client origin via CORS.
- Ensure `web-admin` redirect URI (`/callback`) is registered for the `web-admin` OAuth client.

## Health Checks

```bash
curl http://localhost:5000/health
curl http://localhost:5003/health
```

