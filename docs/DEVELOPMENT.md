# Development Guide

## Services

- `AuthServer` -> `http://localhost:5000`
- `Orders` -> `http://localhost:5003`
- `web-admin` -> `http://localhost:3000`

## Docker (recommended)

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

Start only database services (for local `dotnet run` + `npm run dev` workflows):

```bash
docker compose up -d postgres
```

Notes:
- Docker Compose PostgreSQL now hosts both application databases: `AuthServer` and `Orders`.
- `Orders` is PostgreSQL-only (Docker and local `dotnet run`).

## Local Process Startup

```bash
cd src/backend/AuthServer && dotnet run
cd src/backend/Orders && dotnet run
cd src/web-admin && npm install && npm run dev
```

## Frontend Env (dev)

`src/web-admin/.env.local`

```env
NEXT_PUBLIC_AUTH_URL=http://localhost:5000
NEXT_PUBLIC_ORDERS_URL=http://localhost:5003
NEXT_PUBLIC_APP_BASE_URL=http://localhost:3000
```

## Frontend UI Rules Quick Checklist (`src/web-app`)

Use this checklist before implementing or reviewing a new list/admin screen.

1. Density and controls:
- compact/Minimal style by default
- toolbar controls are `small`, `outlined`, `36px` height
- subtle radius (`6px` to `8px`)

2. Toolbar behavior:
- placeholders only (no floating labels in toolbars)
- search input + filter icon button + optional clear
- avoid mixed control heights and mixed labeling patterns in the same toolbar

3. Page composition:
- compact header (title + breadcrumb + right action)
- use either:
  - status tabs with thin indicator + soft count badges when counts materially help scanning
  - a compact toolbar status dropdown/select when status behaves like a normal list filter
- compact table rows with sortable headers and rows-per-page

4. Actions and feedback:
- row actions: quick edit + 3-dot overflow
- overflow-only row actions are acceptable when no quick-edit flow exists yet
- destructive actions in overflow menus with confirmation when irreversible
- status/role badges should be soft labels

5. Forms and dialogs:
- use outlined MUI fields with floating labels in create/edit forms
- keep toolbar rules separate from form/dialog rules

6. UX validation:
- verify consistency with existing `web-app` patterns
- verify clarity/usability of labels and actions
- if a requirement is ambiguous or materially affects usability, ask the prompter for clarification before finalizing implementation

7. Loading UX baseline:
- every route transition must show immediate progress feedback
- data pages must distinguish first-load vs refetch and keep content mounted during refetch
- list/detail loads should provide inline retry alerts on failure
- superseded requests must be cancelable and stale responses must not override latest state
- show subtle delayed long-load hints (about 1200ms) for slow requests

See `docs/FRONTEND.md` for the full locked UI system rules.

## Frontend E2E (web-app)

Run Playwright loading UX checks:

```bash
cd src/web-app
npm run test:e2e
```

Notes:
- E2E config runs `web-app` on an isolated local port (`3101`).
- E2E mode uses `NEXT_PUBLIC_E2E_BYPASS_AUTH=true` and a local test auth flag to avoid external auth dependency during loading UX tests.

## Auth + Orders Smoke Test

1. Open `http://localhost:3000`
2. Authenticate via OAuth2 redirect to `AuthServer`
3. Verify user profile loads via `/connect/userinfo`
4. Verify orders list loads from `Orders`
5. Verify browser requests do not call `:5002` (no gateway)
6. Verify Orders status filtering works from in-page controls on `/orders` (URL query updates)
7. Verify `New Order` button behavior:
- enabled when user has `orders.write` or `orders.manage`
- disabled with explanatory message when user lacks create scope

## RBAC Development Notes

- Runtime source of truth for role -> permission mapping is the AuthServer DB table `AspNetRoleScopes`.
- `src/backend/AuthServer/appsettings.Scopes.json` remains the scope catalog and bootstrap defaults for role-scope assignments in fresh environments.
- `Orders` route auth is scope-based (`Authorization:Policies` in `src/backend/Orders/appsettings.json`).
- PKCE user login grants role-derived permission scopes automatically (project-specific behavior), while protocol scopes remain request-driven.
- `client_credentials` grants remain request/intersection-based.
- Role changes take effect for existing sessions on refresh/re-login/expiry (self-contained JWT behavior).
- Frontend page/feature access should be scope-based (`AuthGuard.requiredScopes`), while role names may still be displayed in UI (`roles` OIDC scope).

## Modeling & Config Validation Conventions

- Prefer `sealed record` + `init` for backend DTOs, commands, results, and response models.
- Keep framework-bound types as `class` (EF entities, controllers, services, middleware, `DbContext`).
- Use `required` to avoid hidden default strings in models when values must be provided.
- Do not rely on `required` for config safety: use runtime options validation (`IValidateOptions<T>`) and `ValidateOnStart()`.

Example pattern:

```csharp
public sealed record ClientSettings
{
    public required string ClientId { get; init; }
    public required string DisplayName { get; init; }
    public required List<string> AllowedScopes { get; init; }
}
```

```csharp
builder.Services.AddSingleton<IValidateOptions<ClientConfiguration>, ClientConfigurationValidator>();
builder.Services.AddOptions<ClientConfiguration>()
    .Bind(builder.Configuration)
    .ValidateOnStart();
```

## Roles & Permissions Admin UI

- URL: `http://localhost:3000/users/roles`
- Required frontend/backend permission scope: `admin.roles` or `admin.full`
- Backend endpoints (AuthServer):
  - `GET /api/admin/roles`
  - `GET /api/admin/roles/available-scopes`
  - `GET /api/admin/roles/{roleId}`
 - `POST /api/admin/roles`
 - `PUT /api/admin/roles/{roleId}` (metadata + soft-disable)
 - `PUT /api/admin/roles/{roleId}/scopes` (replace role assignments)

## Users Admin UI

- URL: `http://localhost:3000/users`
- Required frontend/backend permission scope: `admin.users` or `admin.full`
- Backend endpoints (AuthServer):
  - `GET /api/admin/users`
  - `GET /api/admin/users/available-roles`
  - `GET /api/admin/users/{userId}`
  - `POST /api/admin/users`
  - `PUT /api/admin/users/{userId}`
  - `PUT /api/admin/users/{userId}/roles`
  - `PUT /api/admin/users/{userId}/status`
  - `PUT /api/admin/users/{userId}/password`
- Guardrails:
  - self-disable is blocked
  - self-removal of `admin.users`/`admin.full` access is blocked
  - inactive roles cannot be assigned through user-management endpoints

## Orders UI Notes

- Sidebar navigation uses a single Orders entry; status filters are in the `/orders` page UI.
- Orders feature routes (`/orders`, `/orders/new`, `/orders/{id}`) should all highlight the Orders menu item.
- Creating a new order uses `/orders/new` and redirects back to `/orders` with a success toast.
- Orders list status uses a toolbar dropdown/select, not status tabs with counts.
- Orders list keeps manual `Refresh` in the table-adjacent toolbar, not the page header.
- Orders list rows currently use overflow-only actions (`3 dots`) with `View details` inside the menu.

## Orders Sample JSON Seeding

- `Orders` seeds a committed demo dataset from `src/backend/Orders/Data/Seed/orders.sample.v1.json`.
- Seeding mode is `IfEmpty`: the import runs only when the `Orders` table is empty.
- Sample orders use synthetic `UserId` values (for admin/all-orders dashboards and demo data), so normal user `/orders` views may still be empty until the user creates orders.
- The old hardcoded sample order seeding path has been replaced by this JSON import.

### Reseed Orders data (local dev)

To re-import the JSON dataset in the PostgreSQL-only setup, reset the local compose Postgres volume and restart services:

```bash
docker compose down -v
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders
```

Note:
- `down -v` resets both `AuthServer` and `Orders` databases for the compose project.

## AuthServer Database Migrations

- AuthServer startup now applies EF migrations (`MigrateAsync`) instead of `EnsureCreated()`.
- Existing local databases created via `EnsureCreated()` may need to be dropped/recreated once to align with migrations.
- Fresh dev/integration environments are seeded automatically on startup (roles, role-scope defaults, users, OpenIddict scopes/clients).

One-time reset (recommended in dev if you hit `42P07: relation "AspNetRoles" already exists` after the migration transition):

```bash
docker compose down -v
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver
```

Note:
- `down -v` removes Docker volumes (including Postgres data for this compose project).
- This is appropriate for local development resets, but it deletes local DB data.

## RBAC Smoke Test (manual)

1. Login as `user@example.com` and verify normal orders screens work but admin orders routes/features do not.
2. Login as `admin@example.com` and verify `orders.manage` capabilities (all orders/admin endpoints) work.
3. Change a user's role in Auth DB (or admin tool), then refresh token/login again and verify permissions change accordingly.
4. Update role scopes in `/users/roles`, then refresh token/login and verify the permission changes are reflected in APIs.

## Backend Integration Tests

AuthServer auth-flow integration tests require Docker (Testcontainers).

Run all AuthServer integration tests:

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj
```

Run only auth flow tests:

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj --filter "FullyQualifiedName~AuthenticationFlowTests"
```

See `docs/TESTING.md` for patterns and best practices used in this repo.

## Troubleshooting

1. CORS errors:
- Check `Cors:AllowedOrigins` in `AuthServer` and `Orders` appsettings.

2. Token validation errors in `Orders`:
- Check `Authentication:Authority` points to the active `AuthServer` URL.

3. Stale frontend endpoints:
- Verify `NEXT_PUBLIC_AUTH_URL` and `NEXT_PUBLIC_ORDERS_URL`.

4. `Npgsql.PostgresException ... 42P07: relation "AspNetRoles" already exists` on AuthServer startup:
- This usually means the DB was created previously with `EnsureCreated()` but has no EF migration history.
- In local dev, reset the compose Postgres volume and restart:

```bash
docker compose down -v
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver
```

## Cleanup Rule During Refactors

- If you confirm code is unused after a refactor (and no tests/docs/runtime paths rely on it), remove it in the same change rather than leaving deprecated layers behind.
