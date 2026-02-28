# Roles & Permissions UI + AuthServer Plan Audit (Gaps / Open Questions)

Date: 2026-02-25
Status: Audit (pre-implementation)
Related:
- `ai-analysis/2026-02-25-roles-permissions-ui-authserver-plan.md`
- `ai-analysis/2026-02-25-roles-permissions-ui-authserver-implementation-checklist.md`

## Overall assessment
The plan is directionally correct and implementable.

Strong parts:
- Correctly identifies that editable role->scope assignments require DB-backed source of truth.
- Correctly keeps scope catalog metadata config/OpenIddict-seeded for v1.
- Correctly includes token issuance + refresh integration work (not just admin CRUD/UI).
- Correctly includes tests across backend and frontend.
- UI recommendation (grouped collapsible checklist by scope prefix) is a good fit for current shadcn primitives and scope count/shape.

## Main gaps / open decisions found during audit

### 1. Frontend auth currently relies on roles from `/connect/userinfo`, but frontend should move to scope-based page/feature guards (high impact)
Current observations:
- `OAuth2Auth.authorize()` in `src/web-admin/lib/oauth2-auth.ts` has the `scope` param commented out.
- `Userinfo()` in `AuthServer` only returns roles if `roles` scope is granted.
- `web-admin` route guards/admin UI currently rely on `user.roles`.

Implication:
- Admin UI access/visibility may be inconsistent or broken if `roles` scope is not requested/granted.

Decision needed:
- Should this feature pass switch frontend page/feature guards to scope-based checks and add a reliable frontend scope source?

Recommendation: Yes (include in scope of implementation)

Decision (user-confirmed):
- Yes. Switch frontend page/feature guards to scope-based checks and add a reliable frontend scope source.

Implementation impact:
- Add frontend scope persistence/parsing from token responses.
- Extend `AuthGuard` to support scope-based checks (`requiredScopes` / any-of).
- Use scope-based gating for `/users/roles` page and permission-management actions.
- Keep roles available in UI for display/debugging, not primary access gating.

### 2. `web-admin` client allowed scopes may not include `roles` (now optional, depending on UI role display needs)
Current observations:
- `src/backend/AuthServer/appsettings.Clients.json` for `web-admin` appears to include `openid`, `email`, `profile`, `offline_access`, custom scopes, but not necessarily `roles`.

Implication:
- If frontend still wants to display role names from `userinfo`, `roles` OIDC scope must be requested and client-allowed.
- If frontend uses scopes only for access checks and does not display roles, this becomes optional.

Decision needed:
- Do we still want frontend UI to display role names (and therefore request/allow OIDC `roles` scope), or de-emphasize roles in UI and use scopes for gating?

Recommendation: Yes

Decision (user-confirmed):
- Keep displaying role names in frontend UI.
- Therefore request/allow OIDC `roles` scope for frontend auth flow.

Implementation impact:
- Add explicit scope request in `src/web-admin/lib/oauth2-auth.ts` including `roles` (and standard OIDC scopes).
- Add `roles` to `web-admin` allowed scopes in `src/backend/AuthServer/appsettings.Clients.json`.

### 3. Soft-disabled roles and `/connect/userinfo` role list behavior (still relevant, but lower impact if UI gating switches to scopes)
Current observations:
- You chose soft-disable roles (`IsActive = false`).
- Plan already says disabled roles should not contribute scopes (good).
- Frontend `AuthGuard` is role-based today (`user.roles`), not scope-based.
- `AuthServer /connect/userinfo` currently returns role names via `userManager.GetRolesAsync(user)` and does not filter disabled roles.

Implication:
- If UI gating becomes scope-based, access mismatch risk is greatly reduced.
- But displaying disabled role names in `/connect/userinfo` may still be confusing in user/admin UI.

Decision needed:
Choose one v1 behavior:
- A. Filter disabled roles out of `/connect/userinfo` role list (Recommended)
- B. Keep userinfo roles unfiltered and accept UI/back-end mismatch for now
- C. Upgrade frontend guards to scope-based checks (larger frontend auth change)

Recommendation: A (small backend change, aligns UI with effective permissions better)

Decision (user-confirmed):
- A. Filter disabled roles out of `/connect/userinfo`.

Implementation impact:
- Update `AuthorizationController.Userinfo()` role retrieval to exclude roles where `AppRole.IsActive == false`.
- Add backend tests covering disabled-role filtering behavior.

### 4. AuthServer migration transition scope is large (high risk / scheduling)
Current observations:
- AuthServer still uses `EnsureCreated()` and has no real migrations baseline in `Data/Migrations`.
- Plan includes switching to `MigrateAsync()` (good long-term), but this is a substantial schema-transition change.

Implication:
- Implementation may expand significantly (baseline migration generation, dev DB reset path, test impacts).
- It is the highest-risk infrastructure step in this feature.

Decision needed:
Choose v1 scope:
- A. Include migration transition now (Recommended if you want future-safe DB evolution)
- B. Keep `EnsureCreated()` for this feature and add `AppRoleScope` model only (works in fresh DB/tests, defer migration transition)

Recommendation: A if you want the clean foundation; B if you want to ship UI/admin capability faster with lower infra churn

Decision (user-confirmed):
- A. Include migration transition now (`EnsureCreated()` -> `MigrateAsync()` + real migrations).

Implementation impact:
- Treat schema migration transition as in-scope, not optional.
- Document local AuthServer DB reset/recreate guidance for developers moving from `EnsureCreated()` DBs.

### 5. Role identifier in API routes (medium impact)
Current observations:
- Plan notes `roleId` is preferred, but not locked.

Decision needed:
- A. Use `roleId` in update/detail routes (Recommended)
- B. Use role `name` in routes (human-readable, but rename issues)

Recommendation: A

Decision (user-confirmed):
- A. Use `roleId` in admin API routes.

### 6. Scope catalog source for validation/listing (medium impact)
Current observations:
- Plan proposes validating against config/OpenIddict-backed scope catalog but exact source is not fixed.

Decision needed:
- A. Validate/list against `ScopeConfigurationService` (config scope catalog) only (Recommended v1)
- B. Validate/list against OpenIddict scope store (`IOpenIddictScopeManager`) only
- C. Combine and assert consistency (more robust, more code)

Recommendation: A for v1 (since scope catalog remains config-seeded source of truth)

Decision (user-confirmed):
- A. Validate/list available scopes using `ScopeConfigurationService` (config scope catalog) for v1.

### 7. Role name rules and rename support (medium impact)
Current observations:
- Plan includes create role + metadata edit + soft-disable, but role rename behavior is not defined.
- Renaming roles can be messy because user-role assignments and any external references depend on role name semantics.

Decision needed:
- A. Do not support role rename in v1 (Recommended)
- B. Support role rename (higher risk and more tests)

Recommendation: A

Decision (user-confirmed):
- A. Do not support role rename in v1.

Implementation impact:
- `UpdateRole` endpoint should allow metadata (`description`, `isActive`) updates only (no `name` changes).
- Frontend UI should not present rename controls in v1.

### 8. Frontend data-fetching approach is implicit (low/medium impact)
Current observations:
- Project currently uses axios + local state, no React Query.
- Plan recommends local state (good), but not explicitly locked.

Decision needed:
- A. Use local state + `authApi` module only (Recommended v1)
- B. Introduce React Query/SWR for this page (larger change)

Recommendation: A

Decision (user-confirmed):
- A. Use local state + `authApi` module only for v1 (no React Query/SWR introduction).

### 9. Admin roles/permissions API authorization policy (medium impact)
Current observations:
- Roles & permissions admin APIs should be protected by permission scopes.
- `admin.full` is already treated as a broad admin permission in other parts of the system.
- Requiring only `admin.roles` may block super-admin style operators unnecessarily.

Decision needed:
- A. Allow `admin.roles` OR `admin.full` for roles/permissions admin endpoints (Recommended)
- B. Require `admin.roles` only

Recommendation: A (keeps `admin.full` meaningful as a super-admin permission while preserving scope-based authorization)

Decision (user-confirmed):
- A. Allow `admin.roles` OR `admin.full` for roles/permissions admin endpoints.

Implementation impact:
- AuthServer admin roles policy should accept either scope.
- Frontend `/users/roles` page scope gate should also accept either scope for UX consistency.

## Missing tasks to add to checklist (recommended)
1. Add frontend scope-aware access helpers/guards (`requiredScopes`) and page-to-scope mapping
2. Add a reliable frontend scope source (token response `scope` persistence recommended)
3. Decide whether OIDC `roles` scope remains requested/allowed for UI display of role names
4. Define and test `/connect/userinfo` role filtering behavior for disabled roles (if chosen)
5. Clarify role rename support (v1 no-rename) in API DTOs and docs

## Audit conclusion
The plan is strong and the previously open decisions have now been resolved. Implementation can proceed with the locked v1 scope and policy choices.
