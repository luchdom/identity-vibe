# AGENTS Notes (Project-Specific)

## Current Architecture

- Backend services: `AuthServer`, `Orders`, `Shared`
- Frontend: `src/web-app`
- Frontend calls `AuthServer` and `Orders` directly (no proxy layer)

## Local Startup

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build postgres authserver orders client
```

## Working Rules

- Update frontend HTTP calls in `src/web-app/lib/api.ts` (`authApi`, `ordersApi`) instead of introducing a new proxy layer.
- Keep user profile fetches on `/connect/userinfo`.
- When changing auth scopes/clients, update both:
- `src/backend/AuthServer/appsettings.Clients.json`
- `src/backend/AuthServer/appsettings.Scopes.json`
- RBAC model in this repo:
- Runtime role->permission assignments live in AuthServer DB (`AspNetRoleScopes`)
- `ScopeConfiguration:UserScopes:RoleScopes` is bootstrap/default seeding config, not runtime source of truth
- APIs (for example `Orders`) authorize routes by scopes/policies, not role fallback
- User PKCE tokens include role-derived permission scopes (project-specific behavior), while protocol scopes remain request-driven
- Refresh token grants recompute current role-derived permission scopes (role changes apply on refresh/re-login/expiry)
- `web-app` roles/permissions UI uses scope-based guards (`admin.roles` or `admin.full`) and calls `AuthServer` admin APIs under `/api/admin/roles`
- For `AuthServer` auth changes, prefer real backend integration tests (`tests/backend/AuthServer.IntegrationTests`) over mocks for regression coverage.
- For scope/claims helper changes, add/update `tests/backend/Shared.UnitTests`.

## Modeling Conventions (.NET)

- Prefer `sealed record` for DTOs, commands, results, API response models, and read/projection models.
- Prefer `class` for EF/Identity entities, `DbContext`, controllers, services, repositories, middleware, and framework infrastructure.
- Prefer `init` for immutable DTO/config properties.
- Use `required` to make construction intent explicit, but do not treat it as runtime validation.
- For configuration models, pair `record/init/required` with runtime options validation (`IValidateOptions<T>` + `ValidateOnStart()`).
- Keep semantic collection defaults only when empty is a valid meaning (for example optional `Roles`/`Scopes` lists in policy config).

## Cleanup Expectations

- When a refactor makes code unused, remove the dead code in the same pass (including DI registrations/usings/tests/docs references).
- Treat repeated warnings like unused parameters and `async` without `await` as cleanup debt to fix while touching the file.
- Keep solution/project hygiene up to date (for example add new test projects to `IdentityVibe.sln`).

## Manual Smoke

- Login through the web app UI
- Load orders list from `Orders`
- Verify role-based permissions manifest as scopes in API behavior (user vs admin paths)
- Confirm auth + orders logs include correlation IDs

## Feature workflow: Plan → Clarify/Task → Audit/Implement (mandatory)

Whenever the user asks to **introduce a new feature**, **develop something new**, **change behavior**, **add an endpoint**, **refactor with functional impact**, or any request that is more than a trivial edit, you MUST follow this workflow. Do not start implementing until the workflow reaches the **Implement** step and the user explicitly chooses it.

### 1) Plan first (no code changes)
You MUST create a **detailed implementation plan** using the **Planning Format** below.

- Write the plan as a Markdown file under: `docs-ai/`
- Use filename format: `docs-ai/<YYYY-MM-DD>-<short-feature-name>-plan.md`
- The plan must be detailed enough that another engineer/agent can implement it without guessing.

After writing the plan file, present a short summary in chat and then ask the user:
> "Do you want to **Clarify** (resolve open questions) or **Task it out** (produce executable task breakdown)?"

### 2) Clarification mode (one question at a time)
If the user chooses **Clarify**, you will:
- Identify the most important **gaps / open questions** that block or change the plan.
- Ask **one question at a time** (never multiple in the same message).
- For each question, provide **2–5 suggested options** (clear alternatives), and ask the user to:
  - choose one option, OR
  - describe how they want to approach it.

After the user answers, update the plan file in `docs-ai/` to reflect the decision, then ask the next single clarification question. Continue until there are no blocking gaps.

### 3) Task-it-out mode (executable task breakdown)
If the user chooses **Task it out**, you will:
- Produce a detailed, step-by-step task breakdown that another agent can execute.
- Write it as a Markdown file under: `docs-ai/`
- Use filename format: `docs-ai/<YYYY-MM-DD>-<short-feature-name>-tasks.md`
- Tasks must include:
  - ordered steps
  - files/components to touch
  - acceptance criteria per task
  - test requirements per task
  - risks/rollout notes if applicable

After writing the tasks file, ask the user:
> "Do you want to **Audit** or **Implement**?"

### 4) Audit mode (consistency validation)
If the user chooses **Audit**, you will validate consistency across:
- the **requested feature**
- the **implementation plan**
- the **identified gaps/open questions** (and their resolutions)
- the **tasks**

Audit output must include:
- mismatches, missing pieces, or unclear requirements
- plan sections that need strengthening
- tasks that are missing, too vague, or not aligned
- any recommended adjustments

Write the audit as: `docs-ai/<YYYY-MM-DD>-<short-feature-name>-audit.md`

After completing the audit, ask the user:
> "Proceed to **Implement** (start coding) or go back to **Clarify/Task it out** to address findings?"

### 5) Implement mode (only after explicit approval)
Only begin code changes after the user explicitly says **Implement**.

---

## Planning Format (required)

Every plan MUST include these sections:

1. **Overview**
   - Problem statement
   - Goals
   - Non-goals

2. **Assumptions & Constraints**
   - Known constraints (tech, time, compatibility, security, performance)
   - Explicit assumptions

3. **Architecture / Approach**
   - Proposed solution
   - Data flows / control flows (as needed)
   - Key components and responsibilities

4. **API / Contracts (if applicable)**
   - Endpoints / inputs / outputs
   - Error handling
   - Authn/authz expectations

5. **Data Model & Storage (if applicable)**
   - Schema changes
   - Migration approach
   - Backward compatibility

6. **Implementation Steps**
   - Ordered steps with enough detail to implement
   - Files/modules likely to change

7. **Testing Strategy**
   - Unit tests
   - Integration tests
   - Edge cases
   - How to validate locally/CI

8. **Observability / Debuggability (as applicable)**
   - Logs/metrics/traces needed to support the feature
   - Key events to log

9. **Rollout Plan**
   - Feature flags (if needed)
   - Deployment steps
   - Backward compatibility
   - Monitoring after release

10. **Risks & Mitigations**
    - Top risks and what you’ll do about them

11. **Open Questions**
    - List of gaps that require Clarification (if any)
