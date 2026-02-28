# Gateway Removal Pre-Implementation Review (Open Questions / Missing Items)

Date: 2026-02-23
Related checklist: `ai-analysis/2026-02-23-gateway-removal-implementation-checklist.md`

## Summary

The implementation checklist is solid, but there are a few important open questions and a small number of missing task items that should be decided before coding to avoid rework.

## Resolved Decisions (2026-02-23)

- [x] Use HTTP dev ports (`AuthServer` `5000`, `Orders` `5003`) for implementation.
- [x] Use direct browser/Next.js client calls to APIs (no Next.js proxy/rewrites).
- [x] Standardize user profile retrieval on `/connect/userinfo`.
- [x] Keep existing auth DB (no forced reset); use seeder sync and targeted cleanup.

## Open Questions (Need Confirmation)

### 1. Canonical dev URLs and HTTP/HTTPS consistency (important) [RESOLVED]

There are mixed defaults in the repo today:

- `AuthServer` frontend fallback uses `http://localhost:5000`
- some compose defaults use `https://localhost:5000`
- `AuthServer` `appsettings.json` issuer defaults include `https://localhost:5000`
- launch profiles expose `https://localhost:5001;http://localhost:5000`

Resolution:

- Standardize dev examples/defaults on HTTP:
- `AuthServer`: `http://localhost:5000`
- `Orders`: `http://localhost:5003`
- `web-admin`: `http://localhost:3000`

Why this matters:

- OIDC issuer / discovery / token validation mismatches can break auth and Orders token validation if URLs differ by scheme/port.

### 2. True browser-direct calls vs Next.js proxy/rewrite approach [RESOLVED]

Your request says "go directly to the apis without Gateway", which strongly suggests browser-direct calls. The current checklist assumes that.

Resolution:

- Use direct browser/Next.js client calls to `AuthServer` and `Orders` (cross-origin + CORS).

Why this matters:

- It changes CORS requirements, env vars, and frontend HTTP client architecture.

### 3. `Orders` public exposure policy (future-facing, but worth confirming)

Direct browser access means `Orders` becomes an externally reachable API in the target architecture.

Status:

- Not required to block implementation. Proceed with direct exposure for current dev architecture.
- Revisit when production architecture is defined.

Why this matters:

- It affects docs, security posture, and future deployment shape (separate public endpoint vs private service).

### 4. Auth profile source of truth (`/connect/userinfo` vs `/account/profile`) [RESOLVED]

I found frontend fallback calls to `/account/profile`, but I did not find an implemented API endpoint for `/account/profile` in `AuthServer` controllers (only docs/examples mention it).

Resolution:

- Standardize on `/connect/userinfo` and remove `/account/profile` fallback usage/references where appropriate.

Why this matters:

- Avoids keeping dead fallback logic and stale mocks/docs.

### 5. CORS policy strategy (values-only update vs code cleanup) [RESOLVED]

Current CORS config uses a combination of:

- `WithOrigins(allowedOrigins)`
- `SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)`

and some config values include wildcard-style entries like:

- `"http://localhost:*"`
- `"*"`

Resolution:

- Use minimal appsettings/config-value updates only for this change.
- Do not refactor CORS policy code in this task.

Why this matters:

- The current wildcard patterns are not a good long-term setup and may behave unexpectedly.

### 6. Local auth DB reset / reseed preference [RESOLVED]

Removing the `gateway` client and `gateway-bff` scope changes seeded auth configuration.

Resolution:

- Keep existing DB; rely on seeder sync / targeted updates.

Why this matters:

- Faster clean cutover vs preserving existing local test data.

### 7. Scope/client cleanup strictness [RESOLVED]

The checklist removes `gateway` client and `gateway-bff` scope. Good default.

Resolution:

- Remove `gateway` client and `gateway-bff` scope immediately (no temporary retention).

Why this matters:

- Full cleanup reduces confusion; temporary retention reduces rollback friction.

## Missing / Under-Specified Checklist Items

### A. Root `.env` file is not listed

The checklist covers `.env.example`, `.env.development`, `.env.production`, `.env.staging`, but not the actual root `.env` file (which exists in the repo).

Suggested addition:

- Update `.env` to remove Gateway vars and add `EXTERNAL_ORDERS_URL` (if you use it locally).

### B. Explicit URL normalization task is not called out

Suggested addition to checklist:

- Add a task to normalize all dev URL defaults (scheme + port) across:
- frontend env fallbacks
- compose env defaults
- `AuthServer` issuer config
- docs/examples

### C. 401 refresh interceptor recursion guard is listed, but client ID detail is not

In `src/web-admin/lib/api.ts`, the interceptor refresh call currently posts refresh token data without `client_id`. `OAuth2Auth.refreshToken()` includes `client_id`.

Suggested addition:

- Decide and standardize refresh token request payload for the interceptor (`client_id=web-admin` likely needed/safer).

### D. `.env` / compose references to `EXTERNAL_ORDERS_URL` consumer ownership

Not every backend service needs `EXTERNAL_ORDERS_URL`.

Suggested clarification:

- Only pass `EXTERNAL_ORDERS_URL` where needed (typically `client` and docs), unless you explicitly want full env symmetry.

### E. Validation should include "no requests to 5002" in browser devtools/network or logs

This is already implied, but worth making explicit in test steps.

Suggested addition:

- Confirm via browser Network tab (or log grep) that no runtime requests hit `localhost:5002`.

## Non-Blocking Inconsistencies (Can be cleaned while touching files)

- `AuthServer.http` examples appear stale/inconsistent:
- examples reference `gateway-bff` client id, while `appsettings.Clients.json` defines `gateway` (not `gateway-bff`)
- examples mention password flow, but server config does not appear to enable password flow
- `appsettings.Scopes.json` role mapping includes `admin.manage`, but the defined admin scopes appear to be `admin.full`, `admin.users`, `admin.roles`

These are not blockers for Gateway removal, but they are good cleanup opportunities while updating docs/examples.

## Updated Recommendation Before Implementation

No remaining blocking confirmations before implementation.
