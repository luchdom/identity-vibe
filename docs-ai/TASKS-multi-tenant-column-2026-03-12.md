## Audit notes

- The plan is aligned with the clarified requirements: global identity, tenant-local access management, token-scoped active tenant context, route-driven tenant selection, and `web-app` as the active frontend.
- The largest code-model mismatch is that current admin flows in `AuthServer` and current business isolation in `Orders` assume global user-role assignment and `UserId`-based ownership instead of tenant membership.
- The biggest implementation risk is partial rollout: token changes, `Orders` filtering, and `web-app` tenant routing must stay consistent or the system will appear multi-tenant in one layer and single-tenant in another.
- Invite acceptance is intentionally deferred, but the data model must still reserve a clean path for it now.

## Backend

### Task 1. Add tenant, membership, invite, and audit entities to AuthServer
- Goal:
  - Introduce the tenant domain model and persistence foundation in `AuthServer`.
- Files or modules likely touched:
  - `src/backend/AuthServer/Data/AppDbContext.cs`
  - `src/backend/AuthServer/Data/Entities/*`
  - `src/backend/AuthServer/Data/Configurations/*`
  - `src/backend/AuthServer/Data/Migrations/*`
- Acceptance criteria:
  - `Tenant`, `TenantMembership`, `TenantInvite`, and support audit entities exist.
  - Tenant slug is unique and immutable in the model/API contract.
  - Tenant lifecycle states are modeled as `Pending`, `Active`, `Suspended`, `Archived`.
  - Membership stores exactly one `RoleId`, plus `IsActive` and `IsDefault`.
  - DB constraints prevent duplicate memberships per user+tenant.
- Test notes:
  - Add migration smoke coverage or integration assertions for uniqueness and required relationships.
- Dependencies:
  - Blocks all downstream tenant-aware auth and API work.

### Task 2. Replace seed/sample identity data with tenant-aware roles and memberships
- Goal:
  - Reset development and test seed assumptions to the new tenant model.
- Files or modules likely touched:
  - `src/backend/AuthServer/Services/DatabaseSeeder.cs`
  - `src/backend/AuthServer/appsettings.Scopes.json`
  - `src/backend/AuthServer/appsettings.Clients.json`
  - test fixtures that rely on seeded users/roles
- Acceptance criteria:
  - Seed roles include `PlatformAdmin`, `TenantAdmin`, `TenantUser`, and `TenantSupport`.
  - Seed tenants and memberships exist.
  - Seed users reflect global identity plus tenant-local memberships.
  - Deprecated seed assumptions around direct global tenant access are removed.
- Test notes:
  - Update integration tests that currently expect `Admin`, `User`, or `Support`.
- Dependencies:
  - Depends on Task 1.

### Task 3. Implement tenant-scoped token issuance and switch flow in AuthServer
- Goal:
  - Make tenant context canonical in the JWT and add switch support.
- Files or modules likely touched:
  - `src/backend/AuthServer/Controllers/AuthorizationController.cs`
  - `src/backend/AuthServer/Services/*`
  - `src/backend/AuthServer/Models/*`
  - `src/backend/Shared/Extensions/ClaimsExtensions.cs`
  - `src/backend/Shared/Extensions/HttpContextExtensions.cs`
- Acceptance criteria:
  - Initial user session issues a token for the default active tenant.
  - Token contains only the active tenant context, not all memberships.
  - Tenant role and tenant-derived scopes are issued from the active membership role.
  - `POST` tenant-switch endpoint exchanges into a new tenant-scoped token set.
  - Suspended tenants and inactive memberships cannot receive new tenant-scoped tokens.
- Test notes:
  - Add AuthServer integration tests for default-tenant issuance, switch, inactive membership, and suspended tenant failures.
- Dependencies:
  - Depends on Tasks 1 and 2.

### Task 4. Add AuthServer identity, platform, and tenant-context APIs
- Goal:
  - Expose the APIs needed for memberships discovery, platform tenant management, support mode, and tenant-local administration.
- Files or modules likely touched:
  - `src/backend/AuthServer/Controllers/*`
  - `src/backend/AuthServer/Services/*`
  - `src/backend/AuthServer/Models/*`
  - `src/backend/AuthServer/Program.cs`
- Acceptance criteria:
  - `GET /api/me/tenants` returns all active switchable memberships for the signed-in identity.
  - Separate platform-admin tenant discovery/list APIs exist.
  - Self-serve tenant creation exists and provisions an active tenant with creator as `TenantAdmin`.
  - Tenant-context membership APIs support list, invite/create, role change, deactivate, and remove.
  - Tenant-admin operations do not expose global identity field mutation.
  - Last-active-tenant-admin guardrail is enforced.
- Test notes:
  - Add integration coverage for tenant-admin membership changes and self-serve tenant creation.
- Dependencies:
  - Depends on Tasks 1 through 3.

### Task 5. Implement direct support mode and impersonation in AuthServer
- Goal:
  - Support platform-admin tenant access in both direct and impersonated modes with auditability.
- Files or modules likely touched:
  - `src/backend/AuthServer/Controllers/*`
  - `src/backend/AuthServer/Services/*`
  - `src/backend/AuthServer/Models/*`
  - audit entities/configurations from Task 1
- Acceptance criteria:
  - Platform admin can start direct tenant mode.
  - Platform admin can impersonate any active tenant user.
  - Impersonation requires a reason.
  - Direct-mode token grants full tenant access and carries actor/support metadata.
  - Impersonation token represents the effective user and actor metadata.
  - All support-mode actions are audit logged.
- Test notes:
  - Add integration tests for direct mode, impersonation, required-reason validation, and audit record creation.
- Dependencies:
  - Depends on Tasks 1 through 4.

### Task 6. Make Orders schema tenant-aware by column
- Goal:
  - Add `TenantId` to tenant-owned data and fix uniqueness/indexing.
- Files or modules likely touched:
  - `src/backend/Orders/Data/Entities/Order.cs`
  - `src/backend/Orders/Data/Entities/Customer.cs`
  - `src/backend/Orders/Data/Entities/IdempotencyRecord.cs`
  - `src/backend/Orders/Data/Configurations/OrderConfiguration.cs`
  - `src/backend/Orders/Data/Configurations/CustomerConfiguration.cs`
  - `src/backend/Orders/Data/Configurations/IdempotencyRecordConfiguration.cs`
  - `src/backend/Orders/Data/Migrations/*`
- Acceptance criteria:
  - `Orders`, `Customers`, and `IdempotencyRecords` have `TenantId`.
  - Customer email uniqueness is per tenant.
  - Order number uniqueness is per tenant.
  - Idempotency uniqueness includes tenant context.
- Test notes:
  - Add DB/integration assertions covering per-tenant uniqueness and migration behavior.
- Dependencies:
  - Can begin after tenant domain shape is stable from Task 1.

### Task 7. Refactor Orders repository/service/controller logic to enforce tenant isolation
- Goal:
  - Make `Orders` read/write behavior tenant-scoped and no longer depend on `UserId` ownership assumptions for visibility.
- Files or modules likely touched:
  - `src/backend/Orders/Repositories/OrdersRepository.cs`
  - `src/backend/Orders/Services/OrdersService.cs`
  - `src/backend/Orders/Controllers/OrdersController.cs`
  - shared claims/context helpers if needed
- Acceptance criteria:
  - All order queries are filtered by active tenant.
  - Tenant members can see all tenant orders, per clarified behavior.
  - Create/update/idempotency writes stamp the active tenant id.
  - No cross-tenant access is possible with a valid token for another tenant.
- Test notes:
  - Add integration tests for tenant-wide visibility and cross-tenant denial.
- Dependencies:
  - Depends on Tasks 3 and 6.

## Frontend

### Task 8. Reshape web-app auth state around tenant-scoped sessions
- Goal:
  - Extend `web-app` session handling to understand default tenant, memberships list, and switched tenant tokens.
- Files or modules likely touched:
  - `src/web-app/contexts/auth-context.tsx`
  - `src/web-app/lib/auth/oauth2-auth.ts`
  - `src/web-app/lib/api.ts`
  - `src/web-app/lib/auth/*`
- Acceptance criteria:
  - Auth state includes active tenant summary and accessible memberships.
  - After login, app lands on the default tenant dashboard.
  - Memberships are loaded from `AuthServer`.
  - Tenant switch replaces stored token set cleanly.
- Test notes:
  - Add e2e coverage for login landing and switch behavior.
- Dependencies:
  - Depends on Tasks 3 and 4.

### Task 9. Split web-app routes into global and tenant-context areas
- Goal:
  - Make route structure reflect the tenant model cleanly.
- Files or modules likely touched:
  - `src/web-app/app/*`
  - likely new route groups for global and tenant-context surfaces
  - shared navigation/app-shell components
- Acceptance criteria:
  - Tenant work lives under `/t/{tenantSlug}/...`.
  - Platform/global pages live outside tenant routes.
  - Visiting an accessible tenant route auto-switches tenant context.
  - Invalid or inaccessible tenant routes fail safely.
- Test notes:
  - Add e2e coverage for route-based auto-switch and unauthorized tenant access.
- Dependencies:
  - Depends on Task 8.

### Task 10. Add tenant switcher and support-mode UI affordances
- Goal:
  - Make tenant context visible and operable in the active frontend.
- Files or modules likely touched:
  - `src/web-app/components/app-shell/*`
  - `src/web-app/app/*`
  - support/impersonation components
- Acceptance criteria:
  - User can switch among active memberships.
  - Direct support mode and impersonation mode show a strong persistent banner.
  - Banner includes a clearly visible exit action.
  - Direct mode and impersonation mode are visually distinct from normal tenant sessions.
- Test notes:
  - Add e2e coverage for switcher and support banner behavior.
- Dependencies:
  - Depends on Tasks 5, 8, and 9.

### Task 11. Move tenant-local user management into web-app tenant routes
- Goal:
  - Re-home membership management from the old global-user model to tenant-local admin flows in `web-app`.
- Files or modules likely touched:
  - `src/web-app/app/*`
  - current users/roles-management pages in `src/web-app`
  - frontend API clients for admin/member management
- Acceptance criteria:
  - Tenant admin can list members in their tenant.
  - Tenant admin can invite/add, role-change, deactivate, and remove members in their tenant.
  - Tenant admin cannot edit global identity fields.
  - Global role catalog management remains platform-only.
- Test notes:
  - Add e2e coverage for tenant-local membership management and last-admin guardrail.
- Dependencies:
  - Depends on Tasks 4, 8, and 9.

### Task 12. Add platform admin global screens in web-app
- Goal:
  - Provide platform routes for tenant discovery, lifecycle management, and support actions.
- Files or modules likely touched:
  - `src/web-app/app/*`
  - platform-specific components
  - frontend API clients for platform APIs
- Acceptance criteria:
  - Platform admin can discover tenants globally.
  - Platform admin can create/manage tenants.
  - Platform admin can enter direct mode or impersonation mode from platform UI.
  - Self-serve tenant creation path remains separate from platform admin controls.
- Test notes:
  - Add e2e coverage for platform-admin-only route guards and support-mode entry points.
- Dependencies:
  - Depends on Tasks 4, 5, 8, and 9.

## Shared or Infra

### Task 13. Add backend test coverage for tenant claims, tenant auth, and tenant isolation
- Goal:
  - Extend test suites so the new model is enforced and debuggable.
- Files or modules likely touched:
  - `tests/backend/AuthServer.IntegrationTests/*`
  - `tests/backend/Shared.UnitTests/*`
  - new or extended Orders integration test project/files
- Acceptance criteria:
  - AuthServer integration tests cover default tenant issuance, switch, support mode, impersonation, suspension, and membership guardrails.
  - Shared unit tests cover tenant/support/impersonation claims helpers.
  - Orders integration coverage proves cross-tenant isolation and tenant-wide visibility.
- Test notes:
  - Add new test project to `ArchZ.sln` if required.
- Dependencies:
  - Depends on Tasks 3 through 7.

### Task 14. Replace sample/dev data and fixtures across the repo
- Goal:
  - Remove obsolete single-tenant/global-role sample assumptions from test and local environments.
- Files or modules likely touched:
  - seed JSON/data under `src/backend/Orders/Data/Seed/*`
  - integration fixtures
  - docker/init or local setup docs if needed
- Acceptance criteria:
  - Sample orders/customers are tenant-stamped.
  - Auth test fixtures use tenant-aware users and memberships.
  - Local startup still produces a usable dev environment under the new model.
- Test notes:
  - Re-run integration suites after reseeding.
- Dependencies:
  - Depends on Tasks 1, 2, and 6.

### Task 15. Update repo docs and remove deprecated guidance conflicts
- Goal:
  - Align repo docs with the new tenant model and active frontend target.
- Files or modules likely touched:
  - `README.md`
  - `docs/ARCHITECTURE.md`
  - `docs/DEVELOPMENT.md`
  - `docs/TESTING.md`
  - `docs/AGENTS.MD`
- Acceptance criteria:
  - Docs describe global identity plus tenant-local access.
  - Docs describe tenant-scoped tokens and switch flow.
  - Docs describe `web-app` as the implementation target and `web-admin` as deprecated where relevant.
  - Docs explain support-mode and impersonation audit expectations.
- Test notes:
  - Manual doc review only.
- Dependencies:
  - Should land after the implementation model is stable enough to document concretely.

## Sources consulted (paths)

- `docs/AGENTS.MD`
- `docs/ARCHITECTURE.md`
- `docs-ai/2026-03-12-multi-tenant-column-plan.md`
- `docs-ai/2026-03-12-multi-tenant-column-clarification-decisions.md`
- `src/backend/AuthServer/Controllers/AuthorizationController.cs`
- `src/backend/AuthServer/Data/AppDbContext.cs`
- `src/backend/AuthServer/Data/Entities/AppUser.cs`
- `src/backend/AuthServer/Services/UserAdminService.cs`
- `src/backend/Orders/Data/Entities/Order.cs`
- `src/backend/Orders/Data/Entities/Customer.cs`
- `src/backend/Orders/Data/Entities/IdempotencyRecord.cs`
- `src/backend/Orders/Data/Configurations/OrderConfiguration.cs`
- `src/backend/Orders/Data/Configurations/CustomerConfiguration.cs`
- `src/backend/Orders/Data/Configurations/IdempotencyRecordConfiguration.cs`
- `src/backend/Orders/Repositories/OrdersRepository.cs`
- `src/backend/Orders/Services/OrdersService.cs`
- `src/backend/Orders/Controllers/OrdersController.cs`
- `src/backend/Shared/Extensions/ClaimsExtensions.cs`
- `src/backend/Shared/Extensions/HttpContextExtensions.cs`
- `src/web-app/contexts/auth-context.tsx`
- `src/web-app/lib/auth/oauth2-auth.ts`
- `tests/backend/AuthServer.IntegrationTests/Flows/AuthenticationFlowTests.cs`
