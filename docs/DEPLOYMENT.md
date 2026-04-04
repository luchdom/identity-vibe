# Deployment Guide

## Runtime Services

- `postgres`
- `authserver`
- `orders`
- `client` (legacy Docker frontend runtime name)

## Compose

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

## Deployment Notes

- Browser traffic goes directly to `AuthServer` and `Orders`
- Ensure CORS allows the active frontend origin
- Ensure `/callback` is registered for the active frontend client
- Keep legacy `web-admin` redirect URIs only while compatibility paths still need them

## Health Checks

```bash
curl http://localhost:5000/health
curl http://localhost:5003/health
```
