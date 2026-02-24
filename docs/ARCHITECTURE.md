# Architecture

## Overview

The system is composed of three runtime components in development:

- `web-admin` (Next.js UI)
- `AuthServer` (.NET 8, OpenIddict)
- `Orders` (.NET 8 API)

The browser calls `AuthServer` and `Orders` directly. There is no separate reverse-proxy service.

## Request Flow

1. User opens `web-admin` (`http://localhost:3000`)
2. Frontend initiates OAuth2 Authorization Code + PKCE against `AuthServer`
3. Frontend exchanges code for tokens on `AuthServer`
4. Frontend loads user profile from `GET /connect/userinfo`
5. Frontend calls `Orders` endpoints directly with bearer tokens

## Security Model

- `AuthServer` issues JWT access tokens.
- `Orders` validates tokens using `AuthServer` as authority.
- `Orders` policies enforce scopes/roles (`orders.read`, `orders.write`, `orders.manage`).
- Admin capabilities rely on the `Admin` role and/or admin scopes (`admin.full`, `admin.users`, `admin.roles`).

## Service Ports (dev)

- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- `web-admin`: `http://localhost:3000`

## Key Files

- `src/backend/AuthServer/Program.cs`
- `src/backend/AuthServer/appsettings.Clients.json`
- `src/backend/AuthServer/appsettings.Scopes.json`
- `src/backend/Orders/Program.cs`
- `src/web-admin/lib/api.ts`
- `src/web-admin/lib/oauth2-auth.ts`
- `src/web-admin/lib/orders-api.ts`
