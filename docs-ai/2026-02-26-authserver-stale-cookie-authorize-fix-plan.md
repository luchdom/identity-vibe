# AuthServer Stale Cookie `Authorize()` Failure Fix Plan

Date: 2026-02-26
Status: Planned

## Symptom

Opening the login/authorize flow after resetting the Auth DB can fail with:

`System.InvalidOperationException: The user details cannot be retrieved.`

From:
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `Authorize()` when `userManager.GetUserAsync(result.Principal)` returns `null`

## Root Cause (likely / expected)

After a DB reset/reseed, the browser may still hold an old Identity application cookie.

- `HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme)` returns `Succeeded = true`
- but the cookie principal refers to a user ID that no longer exists in the reset DB
- `userManager.GetUserAsync(...)` returns `null`
- current code throws `InvalidOperationException` (500) instead of recovering

This is a stale-session recovery case, not a valid server error.

## Immediate workaround (manual)

- Clear site cookies for the app/AuthServer (or use a private window)
- Retry login

## Recommended fix (code)

Treat "authenticated cookie but user not found" as an invalid/stale session:

1. In `AuthorizationController.Authorize()`:
   - if `AuthenticateAsync(...)` succeeds but `GetUserAsync(...)` returns null:
     - sign out the Identity application cookie
     - challenge again to force a fresh login
     - preserve the original authorize request redirect URI
   - do **not** throw `InvalidOperationException`

2. Add warning logging for observability:
   - include client id if present
   - indicate stale cookie / missing user after auth cookie validation

3. (Optional hardening) Also handle inactive users similarly:
   - if `user.IsActive == false`, sign out + challenge
   - treat this as an invalid session for interactive login UX consistency

## Suggested implementation shape

In `Authorize()`:

- Keep the existing "not authenticated" challenge path
- Replace:
  - `throw new InvalidOperationException("The user details cannot be retrieved.")`
- With:
  - `await signInManager.SignOutAsync()` (or `HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme)`)
  - return `Challenge(...)` with the same `RedirectUri` pattern used above

Prefer extracting the challenge construction into a local helper to avoid duplication.

## Confirmed behavior decisions

- Missing user behind valid Identity cookie: `Sign out + challenge` (fresh login)
- Inactive user behind valid Identity cookie: `Sign out + challenge` (fresh login)

## Tests to add (recommended)

### AuthServer integration test

Add a regression test in:
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`

Scenario:
1. Start auth flow and log in normally
2. Delete the user from DB (or create a stale cookie principal)
3. Call `/connect/authorize` again with the existing cookie
4. Assert no 500
5. Assert response redirects/challenges to login (fresh auth)

If deleting the user after login is hard in the current fixture, a simpler route is:
- inject a fabricated cookie principal for `IdentityConstants.ApplicationScheme` (test host setup), but DB deletion is more realistic

### Optional unit/controller test

If controller unit tests exist (they currently do not), this path can be covered there. Integration coverage is more valuable.

## Why this fix is correct

- A missing user behind a valid cookie is a **session invalidation** case
- OAuth/OIDC UX should recover by re-authentication, not crash
- This can happen in dev (DB resets), test environments, and prod after user deletion/recovery scenarios

## Related follow-up (optional)

- Add a small troubleshooting note in docs:
  - stale cookies after DB reset may require relogin/clear cookies
- Not required if the code fix is implemented (it becomes self-healing)
