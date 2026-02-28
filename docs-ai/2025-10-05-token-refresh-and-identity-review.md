# Session Timeout Analysis & Identity Structure Review

_Date: 2025-10-05_

## Observed Behaviour
- Running the full stack (`docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d`) and exercising the admin UI with a Playwright script shows that the session redirects to `/login` ~3 minutes after landing on `/orders`.
- Console diagnostics captured by Playwright:
  - `Failed to refresh token: Error: No refresh token available`
  - `AuthGuard: Redirecting to login - not authenticated`
- `localStorage` contents after sign-in: `accessToken` is present, `refreshToken` is `null`.
- When the frontend later calls `/connect/userinfo`, the gateway returns a 401, the refresh path has no token to exchange, so `AuthGuard` forces a logout.

## Root Cause
- The authorization request assembled in `src/web-admin/lib/oauth2-auth.ts` omits the `scope` query parameter, so OpenIddict only issues an access token (no `offline_access`).
- The `AuthProvider` assumes a refresh token exists when `OAuth2Auth.refreshToken()` runs; when the token is missing it throws, resulting in an eager logout.

## Proposed Fix Plan
1. **Request correct scopes during authorization**  
   Reintroduce the `scope` parameter when building the `/connect/authorize` URL. Suggested set:
   ```
   openid profile email offline_access orders.read orders.write profile.read profile.write admin.full
   ```
   Ensure the list matches `appsettings.Clients.json` for the `web-admin` client.

2. **Handle missing refresh token defensively**  
   In `OAuth2Auth.refreshToken` (and/or `AuthProvider.refreshAccessToken`), detect when `refreshToken` is absent, clear the session, and call `authorize()` instead of immediately throwing. This allows older sessions (created before the scope fix) to recover gracefully.

3. **Regression safeguards**  
   - Store granted scopes alongside tokens so the UI can warn when `offline_access` is missing.
   - Add a Playwright smoke script (similar to the diagnostic run) that idles for >5 minutes and asserts the user remains on `/orders`.

4. **Follow-up hygiene**  
   - Check that backend gateway routes `/connect/userinfo` and `/connect/token` remain reachable (current issues were triggered by the forced logout, not routing itself).

## Identity Structure Review
- **Clients today**: `web-admin` (public, auth-code), `gateway` (confidential), `orders` (confidential), `mobile-app`, `postman`.
- **Service Identity** (machine-to-machine):
  - Use the existing `orders` / `gateway` confidential clients with the client-credentials grant.
  - Limit scopes to service-specific APIs (`orders.*`, `gateway-bff`).
  - Consider issuing long-lived client secrets via Azure Key Vault / AWS Secrets Manager rather than configuration files for production.

- **User Identity** (interactive):
  - `web-admin` should request user-centric scopes (`openid`, `profile`, `email`, `offline_access`, UI-specific API scopes).
  - Roles already seed `Admin` and `User`. Continue storing them in the identity token for UI role checks.
  - Gate user-only routes via role-based policies in the gateway (e.g., `Admin` can reach `/orders/manage`, `User` limited to self-service endpoints).

### Improvement Ideas
1. **Scope layering**  
   - Define separate scope bundles: `user.read`, `user.manage`, `orders.read`, `orders.manage` and tie them to gateway routes.
   - For client-credential flows, map scopes to service principal permissions rather than human roles.

2. **Token lifetime strategy**  
   - Development environment currently sets access tokens to 30 days. Consider shortening and relying on refresh tokens (now that they will exist) to align behaviour with production.

3. **PKCE & Consent**  
   - `web-admin` already enforces PKCE (`RequirePkce: true`). Keep consent disabled for first-party apps; enable for third parties if needed.

4. **Documentation**  
   - Update `docs/DEPLOYMENT.md` / `docs/DEVELOPMENT.md` withOpa mal the scope list and explain the difference between service identities (client credentials) and user identities (authorization code + refresh token).

## Summary
- Yes, we need to request the proper scopes—specifically `offline_access`—during the PKCE authorization step or the admin UI cannot refresh tokens.
- Hardening the refresh workflow to reinitiate the auth flow when no refresh token is present will improve resilience.
- The existing identity structure already supports separate service and user identities; tightening scopes and documenting expectations will clarify the model.

