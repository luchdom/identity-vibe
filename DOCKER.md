# Docker Notes

## Services and Ports (host)

- `postgres`: `5432`
- `authserver`: `5000` (HTTP), `5001` (HTTPS in prod compose)
- `orders`: `5003` (HTTP), `5005` (HTTPS in prod compose)
- `client`: `3000`

## Development

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

## Production-like Compose

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
```

## Environment Variables Used by Client

- `NEXT_PUBLIC_AUTH_URL`
- `NEXT_PUBLIC_ORDERS_URL`
- `NEXT_PUBLIC_APP_BASE_URL`

## Health Checks

```bash
curl http://localhost:5000/health
curl http://localhost:5003/health
```

