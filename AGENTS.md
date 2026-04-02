# AGENTS

This file is the canonical agent entrypoint and the canonical workflow-rules document for this repo.

## Read This First

- Backend/.NET architecture and implementation rules: `docs/BACKEND.md`
- Frontend implementation rules: `docs/FRONTEND.md`
- Runtime/system topology: `docs/ARCHITECTURE.md`
- Local development and run flows: `docs/DEVELOPMENT.md`
- Deployment/runtime ops: `docs/DEPLOYMENT.md`
- Testing expectations and commands: `docs/TESTING.md`

## Repo Snapshot

- Backend services: `AuthServer`, `Orders`, `Shared`
- Primary frontend: `src/web-app`
- Legacy frontend kept for compatibility/migration: `src/web-admin`
- Browser traffic goes directly to `AuthServer` and `Orders`

## Always-Follow Rules

- Keep controllers thin and HTTP-focused.
- Keep business logic in services.
- Keep persistence in repositories and `Data/`.
- Do not access `DbContext` from controllers.
- Treat top-level `Configuration/` as an approved repo-specific exception.
- Prefer positional `public sealed record Type(...);` syntax for DTO-style backend models where practical.
- Keep `Shared` for genuinely cross-service concerns only.

## Mandatory Workflow

When the user asks for a non-trivial feature, behavior change, endpoint, refactor with functional impact, or any request larger than a trivial edit, use this workflow and do not start implementation until the user explicitly chooses `Implement`.

### 1. Plan first

- Create a detailed plan under `docs-ai/`
- Filename format: `docs-ai/<YYYY-MM-DD>-<short-feature-name>-plan.md`
- Use the required planning format below

After writing the plan, summarize it briefly and ask:

`Do you want to Clarify (resolve open questions) or Task it out (produce executable task breakdown)?`

### 2. Clarify

- Ask one question at a time
- Offer 2-5 clear options
- Update the plan file after each answer
- Continue until no blocking gaps remain

### 3. Task it out

- Create an executable task breakdown under `docs-ai/`
- Filename format: `docs-ai/<YYYY-MM-DD>-<short-feature-name>-tasks.md`
- Include ordered steps, files to touch, acceptance criteria, tests, and rollout/risk notes when relevant

After writing the tasks file, ask:

`Do you want to Audit or Implement?`

### 4. Audit

- Validate the request, plan, clarification decisions, and tasks for consistency
- Call out mismatches, gaps, weak sections, and recommended adjustments
- Write the audit under `docs-ai/<YYYY-MM-DD>-<short-feature-name>-audit.md`

After auditing, ask:

`Proceed to Implement or go back to Clarify/Task it out to address findings?`

### 5. Implement

- Start coding only after the user explicitly says `Implement`

## Required Planning Format

Every plan must include:

1. Overview
2. Assumptions & Constraints
3. Architecture / Approach
4. API / Contracts
5. Data Model & Storage
6. Implementation Steps
7. Testing Strategy
8. Observability / Debuggability
9. Rollout Plan
10. Risks & Mitigations
11. Open Questions
