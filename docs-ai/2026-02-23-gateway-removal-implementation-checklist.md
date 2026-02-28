# Gateway Removal Implementation Checklist (Direct-to-API Cutover)

Date: 2026-02-23
Repo: `identity-vibe`
Goal: Remove `Gateway` (YARP) and make the web app call `AuthServer` and `Orders` directly.

## Confirmed Decisions (2026-02-23)

- [x] Use HTTP dev URLs/ports (not HTTPS) for cutover work.
- [x] Use direct browser/Next.js client calls to APIs (no Next.js proxy/rewrites).
- [x] Standardize user profile retrieval on `/connect/userinfo` (remove `/account/profile` fallback usage).
- [x] Keep existing auth DB (no forced reset); rely on seeder sync/updates.

## Usage

- Use this file as the execution checklist while implementing.
- Mark items complete as changes are made and verified.
- Keep notes under each section when a decision changes the original plan.

## 0. Cutover Targets (confirm before coding)

- [x] Confirm dev endpoints:
- [x] `AuthServer` = `http://localhost:5000`
- [x] `Orders` = `http://localhost:5003`
- [x] `web-admin` = `http://localhost:3000`
- [x] Confirm direct browser access to `Orders` is the intended development architecture.
- [x] Confirm no remaining requirement to preserve Gateway compatibility during implementation.
- [x] Normalize all dev URL defaults (scheme + ports) across code/config/docs:
- [x] frontend fallbacks
- [x] compose defaults
- [x] `AuthServer` issuer/examples/docs
- [x] `Orders` examples/docs

Notes:

- 

## 1. Frontend API Client Refactor (`src/web-admin`)

### 1.1 Split API clients in `src/web-admin/lib/api.ts`

- [x] Replace single `BASE_URL` (`NEXT_PUBLIC_API_BASE_URL`) usage with explicit service URLs.
- [x] Add `AUTH_BASE_URL` from `NEXT_PUBLIC_AUTH_URL` (fallback `http://localhost:5000`).
- [x] Add `ORDERS_BASE_URL` from `NEXT_PUBLIC_ORDERS_URL` (fallback `http://localhost:5003`).
- [x] Export separate axios instances (recommended: `authApi` and `ordersApi`).
- [x] Preserve correlation ID helpers and `authEvents` export.
- [x] Apply request interceptor to both clients:
- [x] Add `Authorization` header from `localStorage.accessToken`
- [x] Add `X-Correlation-ID`
- [x] Preserve dev request logging
- [x] Apply response interceptor to both clients:
- [x] Preserve dev response/error logging
- [x] Preserve session-expired behavior (`authEvents`)
- [x] Ensure 401 refresh retry flow still works and is not recursive.
- [x] Move refresh-token request target to `AuthServer` (`/connect/token` via `AUTH_BASE_URL`).
- [ ] Verify retry uses refreshed token on the original client request.

### 1.2 Orders client usage in `src/web-admin/lib/orders-api.ts`

- [x] Replace import of generic `api` with `ordersApi`.
- [x] Verify all methods still call `/orders/...` routes unchanged.
- [x] Verify admin and user list methods still hit `/orders/all` and `/orders`.

### 1.3 Auth client usage in `src/web-admin/lib/oauth2-auth.ts`

- [x] Keep authorize/token/logout endpoints on `NEXT_PUBLIC_AUTH_URL`.
- [x] Change `getUserInfo()` to use `authApi` for `/connect/userinfo`.
- [x] Remove `/account/profile` fallback and standardize profile retrieval on `/connect/userinfo`.
- [x] Confirm token exchange and refresh methods do not depend on removed Gateway base URL.
- [x] Confirm local storage/session storage behavior remains unchanged.

### 1.4 Auth context regression check in `src/web-admin/contexts/auth-context.tsx`

- [x] Verify imports from `@/lib/api` still compile after API client refactor.
- [ ] Verify session-expired modal flow still works.
- [ ] Verify startup auth initialization still calls working user-info endpoint.

Notes:

- 

## 2. Frontend Environment Variables (`src/web-admin` + compose injection)

### 2.1 Local frontend env file

- [x] Update `src/web-admin/.env.local`:
- [x] Add `NEXT_PUBLIC_ORDERS_URL=http://localhost:5003`
- [x] Keep `NEXT_PUBLIC_AUTH_URL=http://localhost:5000`
- [x] Remove `NEXT_PUBLIC_API_BASE_URL=http://localhost:5002`
- [x] Remove Gateway reference comments (if any)

### 2.2 Frontend env variable references

- [x] Repo search for `NEXT_PUBLIC_API_BASE_URL` and remove all usages.
- [x] Repo search for hardcoded `localhost:5002` in frontend runtime code and remove.
- [x] Add/confirm `NEXT_PUBLIC_ORDERS_URL` usage in all frontend runtime call paths.

Notes:

- 

## 3. Backend CORS Updates (Direct Browser Access)

### 3.0 CORS cleanup scope decision

- [x] Confirm implementation scope for CORS:
- [x] Minimal config-value updates only
- [ ] Config updates plus CORS policy code cleanup (recommended)

### 3.1 AuthServer CORS

- [x] Update `src/backend/AuthServer/appsettings.json` CORS defaults.
- [x] Update `src/backend/AuthServer/appsettings.Docker.json` CORS defaults.
- [x] Add/keep `http://localhost:3000` (and other dev web origins if needed).
- [x] Remove Gateway-specific origin assumptions (`http://localhost:5002`) from defaults/examples.
- [ ] Remove invalid/unsafe wildcard-style examples (`*`, `http://localhost:*`) if doing CORS policy cleanup.
- [ ] Verify no CORS regression for `connect/*` and `userinfo`.

### 3.2 Orders CORS

- [x] Update `src/backend/Orders/appsettings.json` CORS defaults.
- [x] Update `src/backend/Orders/appsettings.Development.json` CORS defaults.
- [x] Update `src/backend/Orders/appsettings.Docker.json` CORS defaults.
- [x] Add/keep `http://localhost:3000` as allowed origin.
- [x] Remove Gateway-specific origins:
- [x] `http://localhost:5002`
- [x] `http://gateway:8080`
- [ ] Remove invalid/unsafe wildcard-style examples (`*`, `http://localhost:*`) if doing CORS policy cleanup.
- [ ] Verify `Authorization` header and preflight requests are accepted.

Notes:

- 

## 4. Docker Compose / Runtime Topology Cleanup

### 4.1 Remove Gateway service from compose files

- [x] Update `docker-compose.yml`:
- [x] Delete `gateway` service block
- [x] Remove Gateway port mappings
- [x] Remove Gateway-specific comments
- [x] Update `depends_on` references
- [x] Update `docker-compose.dev.yml`:
- [x] Delete `gateway` service overlay block
- [x] Remove Gateway-specific dev comments
- [x] Update `docker-compose.prod.yml`:
- [x] Delete `gateway` service block
- [x] Remove Gateway port mappings and comments
- [x] Update `docker-compose.debug.yml`:
- [x] Remove Gateway env references/comments if present

### 4.2 Update compose env injection for client and services

- [x] In compose files, replace `NEXT_PUBLIC_API_BASE_URL` with `NEXT_PUBLIC_ORDERS_URL`.
- [x] Add `EXTERNAL_ORDERS_URL` usage for `client` in base/prod compose where applicable.
- [x] Remove `EXTERNAL_GATEWAY_URL` env usage.
- [x] Remove `INTERNAL_GATEWAY_URL` env usage.
- [x] Update `client.depends_on` to `authserver` + `orders` (or remove entirely if not needed).

Notes:

- 

## 5. Root Env Templates and Local Env Files

- [x] Update `.env` (actual local file, if used for local compose runs):
- [x] Add `EXTERNAL_ORDERS_URL`
- [x] Remove `EXTERNAL_GATEWAY_URL`
- [x] Remove `INTERNAL_GATEWAY_URL`
- [x] Update `.env.example`:
- [x] Add `EXTERNAL_ORDERS_URL`
- [x] Remove `EXTERNAL_GATEWAY_URL`
- [x] Remove `INTERNAL_GATEWAY_URL`
- [x] Update comments/documentation strings
- [x] Update `.env.development`:
- [x] Add `EXTERNAL_ORDERS_URL=http://localhost:5003`
- [x] Remove `EXTERNAL_GATEWAY_URL`
- [x] Remove `INTERNAL_GATEWAY_URL`
- [x] Update `.env.production`:
- [x] Replace `EXTERNAL_GATEWAY_URL` with `EXTERNAL_ORDERS_URL`
- [x] Remove `INTERNAL_GATEWAY_URL` comments
- [x] Update `.env.staging`:
- [x] Replace `EXTERNAL_GATEWAY_URL` with `EXTERNAL_ORDERS_URL`
- [x] Remove `INTERNAL_GATEWAY_URL` comments

Notes:

- 

## 6. AuthServer Client/Scope/Seeder Cleanup (`src/backend/AuthServer`)

### 6.1 OAuth clients in `appsettings.Clients.json`

- [x] Decision: remove `gateway` client immediately (no temporary retention).
- [x] Remove `gateway` client entry (confidential client).
- [x] Remove `gateway-bff` from `web-admin` `AllowedScopes` if unused.
- [x] Re-check `web-admin` redirect URIs remain correct.
- [x] Re-check `orders` service client remains intact.

### 6.2 Scopes in `appsettings.Scopes.json`

- [x] Decision: remove `gateway-bff` scope immediately (no temporary retention).
- [x] Remove `gateway-bff` scope if it is Gateway-only.
- [x] Verify no role/default scope mappings reference removed scope.
- [x] Fix inconsistent role scope references (e.g. `admin.manage`) to match defined scopes (`admin.full`, `admin.users`, `admin.roles`) or intended replacement.
- [x] Verify no docs/examples rely on removed scope names.

### 6.3 Seeder token replacement cleanup in `Services/DatabaseSeeder.cs`

- [x] Remove `${EXTERNAL_GATEWAY_URL}` replacement.
- [x] Remove `${INTERNAL_GATEWAY_URL}` replacement.
- [x] Keep auth/client/orders replacements intact.
- [ ] Confirm code compiles after removal.

### 6.4 Auth data reseed/reset (dev only)

- [x] Decide reseed approach for local dev auth DB:
- [x] Reuse existing DB and let seeder sync
- [ ] Reset DB for clean client/scope state
- [ ] Verify seeded clients/scopes match updated config after startup.

Notes:

- 

## 7. Remove Gateway Project and Solution Entry

### 7.1 Solution cleanup

- [x] Update `ArchZ.sln` to remove `Gateway` project entry.
- [x] Remove Gateway project build configuration mappings from `ArchZ.sln`.

### 7.2 Delete project files

- [x] Delete `src/backend/Gateway/` folder.
- [ ] Confirm removal includes:
- [x] `src/backend/Gateway/Program.cs`
- [x] `src/backend/Gateway/Gateway.csproj`
- [x] `src/backend/Gateway/appsettings.json`
- [x] `src/backend/Gateway/appsettings.Development.json`
- [x] `src/backend/Gateway/appsettings.Docker.json`
- [x] `src/backend/Gateway/Dockerfile`
- [x] `src/backend/Gateway/Dockerfile.dev`
- [x] `src/backend/Gateway/Gateway.http`
- [x] `src/backend/Gateway/Properties/launchSettings.json`

Notes:

- 

## 8. Tests and HTTP Example Files

### 8.1 Playwright test updates

- [x] Update `src/web-admin/tests/orders-page.spec.ts`.
- [x] Remove hardcoded `localhost:5002` matcher.
- [x] Match direct Orders requests (`localhost:5003`) or use `**/orders/**`.
- [ ] Keep auth mocks aligned with final frontend behavior (`/connect/userinfo`, optional `/account/profile`).
- [ ] Run/verify test(s) after frontend refactor.

### 8.2 HTTP examples

- [x] Update `src/backend/AuthServer/AuthServer.http`:
- [x] Remove/rename Gateway-BFF examples and comments
- [x] Replace `gateway-bff` client examples with valid current direct-flow examples
- [x] Remove/replace password-flow examples if password flow is not enabled in server config
- [x] Update `src/backend/Orders/Orders.http`:
- [x] Remove Gateway examples/comments (`localhost:5002`, `gateway:8080`)
- [x] Keep direct Orders examples only
- [x] Delete `src/backend/Gateway/Gateway.http` with Gateway project removal.

Notes:

- 

## 9. Documentation Rewrite (Architecture + Setup)

### 9.1 Core docs

- [x] Update `README.md`
- [x] Update `CLAUDE.md`
- [x] Update `README-DEPLOYMENT.md`
- [x] Update `DOCKER.md`

### 9.2 `docs/` set

- [x] Update `docs/AGENTS.MD`
- [x] Update `docs/ARCHITECTURE.md`
- [x] Update `docs/DEVELOPMENT.md`
- [x] Update `docs/DEPLOYMENT.md`
- [x] Update `docs/FRONTEND.md`
- [x] Update `docs/LOGGING_IMPLEMENTATION.md`

### 9.3 Documentation content changes required

- [x] Remove statements that require all traffic to pass through Gateway.
- [x] Remove YARP-specific configuration/routing guidance.
- [x] Replace Gateway endpoint examples (`5002`) with direct `AuthServer` / `Orders`.
- [x] Update startup instructions to run only `authserver`, `orders`, and `client`.
- [x] Update diagrams/architecture text to direct client-to-service model.
- [x] Update troubleshooting sections that mention Gateway/YARP.

Notes:

- 

## 10. Repo-Wide Cleanup Sweep

- [x] Search and remove remaining references to:
- [x] `Gateway`
- [x] `gateway`
- [x] `YARP`
- [x] `5002`
- [x] `EXTERNAL_GATEWAY_URL`
- [x] `INTERNAL_GATEWAY_URL`
- [x] `gateway-bff`
- [x] `/account/profile` (remove stale profile endpoint references if standardizing on `/connect/userinfo`)
- [x] `admin.manage` (resolve stale scope references/examples)
- [x] Confirm remaining matches are intentional (ideally none).

Notes:

- 

## 11. Validation Checklist (Run After Implementation)

### 11.1 Local runtime / manual smoke

- [ ] Start `AuthServer`, `Orders`, and `web-admin` (no Gateway).
- [ ] Confirm login redirect/PKCE flow works end-to-end.
- [ ] Confirm `connect/userinfo` works directly from browser.
- [ ] Confirm orders list loads.
- [ ] Confirm order details page loads.
- [ ] Confirm create/update/cancel order flows work (as applicable).
- [ ] Confirm admin-only actions still enforce permissions.
- [ ] Confirm via browser Network tab that no runtime requests hit `localhost:5002`.

### 11.2 Token handling / auth robustness

- [ ] Force/trigger a 401 and verify refresh-token flow hits `AuthServer` directly.
- [ ] Confirm refresh-token request payload is standardized (including `client_id` if required by final implementation).
- [ ] Confirm session-expired modal still appears when refresh fails.
- [ ] Confirm correlation ID still appears in outgoing requests.

### 11.3 Docker validation

- [ ] `docker compose up` works without Gateway service.
- [ ] Client container resolves correct `NEXT_PUBLIC_AUTH_URL`.
- [ ] Client container resolves correct `NEXT_PUBLIC_ORDERS_URL`.
- [ ] AuthServer and Orders reachable from browser in Docker setup.

### 11.4 Build/solution validation

- [ ] `ArchZ.sln` opens/builds without `Gateway`.
- [ ] Backend builds (`AuthServer`, `Orders`, `Shared`) succeed.
- [ ] Frontend build/dev server runs successfully.

Notes:

- 

## 12. Completion Criteria

- [ ] No runtime traffic from `web-admin` goes to `http://localhost:5002`.
- [ ] Gateway project removed from repo and solution.
- [x] Compose and env files have no Gateway service/env references.
- [x] AuthServer clients/scopes no longer include Gateway-only entries.
- [x] Docs no longer describe Gateway as required architecture.
- [x] Repo-wide search confirms no stray Gateway/YARP references (or all remaining are intentional and documented).

## Implementation Log (fill in during work)

- [x] Task execution started
- [x] Frontend refactor completed
- [x] Backend CORS updates completed
- [x] Compose/env cleanup completed
- [x] Auth seeding cleanup completed
- [x] Gateway project deleted
- [x] Tests/examples/docs updated
- [ ] Final validation completed

Change notes:

- Remaining `src/backend/Gateway/` artifacts were deleted after the running `Gateway.exe` process exited.
- Frontend `next build` compiles and type-checks after the refactor, but the build command still fails on pre-existing ESLint issues unrelated to Gateway removal.
- Backend build verification with default outputs was blocked earlier by active local process file locks. A later alternate-output solution build attempt failed with duplicate assembly attribute errors (verification issue not caused by Gateway removal changes).
