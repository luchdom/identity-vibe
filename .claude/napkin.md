# Napkin Runbook

## Curation Rules
- Re-prioritize on every read.
- Keep recurring, high-value notes only.
- Max 10 items per category.
- Each item includes date + "Do instead".

## Execution & Validation (Highest Priority)
1. **[2026-03-15] Prefer real AuthServer integration coverage for auth changes**
   Do instead: validate auth/token/scope behavior in `tests/backend/AuthServer.IntegrationTests` before trusting unit-level coverage.
2. **[2026-03-15] Scope changes must stay aligned across config, policies, and frontend guards**
   Do instead: update `src/backend/AuthServer/appsettings.Scopes.json`, `src/backend/AuthServer/appsettings.Clients.json`, backend authorization policies, and `src/web-app` permission helpers together.

## Domain Behavior Guardrails
1. **[2026-03-15] `web-app` is the active frontend target**
   Do instead: implement frontend auth/admin changes in `src/web-app` and avoid routing new work into deprecated `src/web-admin`.
2. **[2026-03-15] Runtime RBAC source of truth lives in AuthServer DB**
   Do instead: treat `AspNetRoleScopes` as the live role-to-scope mapping and use config only for bootstrap/default seeding behavior.

## User Directives
1. **[2026-03-15] Feature work follows the docs-ai workflow**
   Do instead: keep feature planning, tasks, audits, and implementation artifacts in `docs-ai/` and wait for explicit implementation approval before coding feature changes.
