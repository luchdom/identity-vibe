# RBAC Request Review + Open Questions (Pre-Implementation)

Date: 2026-02-24
Status: Analysis
Related:
- `ai-analysis/2026-02-24-rbac-role-scope-token-policy-plan.md`
- `ai-analysis/2026-02-24-rbac-role-scope-token-policy-implementation-checklist.md`

## Request summary (normalized)
Target model:
- Users have one or more Roles
- Roles map to Scopes (permissions)
- On PKCE login, token gets scopes based on the user's role assignment
- APIs (Orders etc.) authorize routes using scope-based policies
- Route access is validated against token scopes, not raw roles

Additional design decision from user:
- User wants permission scopes from role mapping included in token even if those permission scopes were not explicitly requested by the client.

## Does this make sense?
Yes, for a first-party/internal system this is coherent and implementable.

It is a valid RBAC-over-OAuth design if we keep these boundaries explicit:
- Roles = identity/grouping metadata (AuthServer concern)
- Scopes = API permissions (resource API concern)
- APIs authorize by scopes, not by role fallback

## What already matches the request (current code)
- `AuthServer` already has role->scope mapping config (`ScopeConfiguration:UserScopes:RoleScopes`)
- PKCE code flow already derives scopes from role mapping in `AuthorizationController.CreateUserPrincipalAsync(...)` (currently request-filtered)
- `Orders` endpoints already use named authorization policies (`orders.read`, `orders.write`, `orders.manage`)

## Main mismatches / gaps (must address)
1. Orders policy logic can authorize by Role OR Scope (bypasses intended scope-only route auth)
2. `HasScope()` in shared claims helpers is brittle for space-separated `scope` claims
3. Refresh token flow needs to be aligned to the new issuance rule (including upgrades/downgrades)
4. Client-allowed scope enforcement becomes more important now that AuthServer may add unrequested scopes
5. Existing access token lifetime/revocation behavior may not match expected RBAC responsiveness after role changes

## Important consequences of "grant all role scopes" (not request-limited)
1. OAuth least-privilege-by-request is intentionally relaxed
- This is okay for first-party apps, but should be documented.

2. OpenIddict request validation alone may be insufficient for added scopes
- OpenIddict validates requested scopes.
- If AuthServer manually adds extra scopes not in the request, we should explicitly ensure they are still allowed for the client.
- Recommendation: make client-allowed-scope intersection REQUIRED (not optional) in final scope assembly.

3. Refresh token semantics must be defined explicitly
- Should refresh grant only preserve previously granted permissions?
- Or also add newly gained role permissions after role changes?
- With your chosen model, consistency suggests refresh can add newly gained role-derived permission scopes.

4. Access token lifetime matters more
- Current non-production access tokens are long-lived (30 days in current `AuthServer` config).
- Role changes will not affect already-issued JWT access tokens until refresh/relogin/expiry.
- If you expect near-immediate RBAC changes, this needs a strategy decision.

## Open questions / optional choices to confirm before implementation

### Q1 (Important): Scope set finalization for user tokens
When AuthServer builds the final scope set for a PKCE user token, should it explicitly intersect with the client's allowed scopes (from OpenIddict client permissions), even for unrequested role-derived scopes?

Options:
- A. Yes, explicitly intersect final scopes with client-allowed scopes (Recommended)
  - Pros: prevents over-granting if role mapping contains scopes not intended for that client
  - Pros: safer because request validation no longer limits business scopes
  - Cons: slightly more code in `AuthorizationController`
- B. Rely only on OpenIddict request validation
  - Pros: less code
  - Cons: risky because we are adding scopes that may not have been in the request

Recommendation: A

Decision (user-confirmed):
- Use a custom variant:
  - For user authentication flows (PKCE), add all role-derived permission scopes to the token (do not intersect with requested business scopes, and do not add an extra client-allowed intersection step).
  - For `client_credentials`, keep intersection/validation behavior as-is.

Implication accepted:
- A PKCE client may receive role-derived permission scopes that were not requested and may exceed the client's configured scope request, by design.
- This should be documented clearly as an intentional first-party behavior.

### Q2 (Important): Refresh-token scope behavior after role changes
With your model, when a user's role changes and they use a refresh token, should the refreshed access token:
- A. Recompute and issue all current role-derived permission scopes (Recommended for consistency)
- B. Only keep the intersection of originally granted permission scopes and current role-derived scopes (current behavior, but inconsistent with your new login rule)
- C. Never add or remove permission scopes on refresh (not recommended)

Recommendation: A

Decision (user-confirmed):
- A. Recompute and issue all current role-derived permission scopes on refresh.

Implication accepted:
- Role upgrades can add new permission scopes on refresh.
- Role downgrades remove permission scopes on refresh.
- Refresh behavior is intentionally aligned with the PKCE user-login issuance model.

### Q3 (Important): RBAC responsiveness for already-issued access tokens
Do you want role/scope changes to take effect for already-issued access tokens immediately or only on refresh/relogin/expiry?

Options:
- A. Apply on refresh/relogin/expiry only (Recommended v1)
  - Pros: works with current self-contained JWT approach
  - Pros: low complexity
  - Cons: existing access tokens keep old permissions until they expire
- B. Near-immediate effect via short access token lifetime (Recommended if you want stronger responsiveness without major architecture changes)
  - Pros: still simple, improves responsiveness
  - Cons: more refresh traffic
- C. Immediate effect via reference tokens/introspection/revocation store (v2)
  - Pros: strongest control
  - Cons: higher complexity, runtime dependency changes

Recommendation: A for v1, optionally combined with shorter access token lifetime later

Decision (user-confirmed):
- A. Apply role/scope changes on refresh / re-login / expiry only (v1).

Implication accepted:
- Already-issued self-contained JWT access tokens keep previous permissions until expiry.
- RBAC changes become effective on next refresh, re-login, or token expiration.

### Q4 (Optional but useful): Role claims in access tokens
Should role claims remain in access tokens?

Options:
- A. Keep role claims in access tokens for compatibility/debugging, but APIs authorize by scopes (Recommended v1)
- B. Remove role claims from access tokens and expose roles only via `userinfo` when `roles` scope is granted (stricter)

Recommendation: A for now

Decision (user-confirmed):
- A. Keep role claims in access tokens for compatibility/debugging.

Constraint kept:
- Resource APIs (Orders) should still authorize routes by scopes, not role fallback.

### Q5 (Optional scope management direction): Role->scope mapping source
Do you want role->scope mapping to remain config-based (`appsettings.Scopes.json`) for now, or move to DB-managed/admin-editable mapping now?

Options:
- A. Keep config-based mapping for now (Recommended)
- B. Move to DB-managed mapping now

Recommendation: A (much lower scope/risk; current code already supports it)

## Additional implementation note (no decision needed yet)
- `Orders` should move to scope-only policy config, but the shared authorization extension can remain generic (`Role OR Scope`) as long as Orders config does not populate roles.
- If refactoring `AuthorizationController`, prefer a dedicated OpenIddict grant outcome type over generic API `Result<T>` for final controller mapping, because token endpoint responses are protocol actions (`SignIn` / `Forbid` with OpenIddict error payloads), not standard REST `Ok/ProblemDetails`.
- Refactor scope for current RBAC pass is confirmed:
  - `Token()` grant branches only
  - `Authorize()` extraction can be a follow-up if still needed

