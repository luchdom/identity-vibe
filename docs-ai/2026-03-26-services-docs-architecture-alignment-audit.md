# Audit: Services And Docs Architecture Alignment

## Verdict

The clarified plan and task breakdown are directionally aligned with the requested architecture/doc cleanup, but they still have a few important gaps. The biggest issues are:

- the documentation ownership model does not yet explicitly preserve the current agent workflow rules after `docs/AGENTS.MD` is merged/reduced
- the Ardalis Result migration scope may be overreaching into repository contracts without first locking whether repositories should return results at all
- the task breakdown still contains vocabulary the user explicitly rejected
- the verification strategy is stronger for `AuthServer` than for `Orders`, despite both services being equal first-pass refactor targets

## Findings

### 1. High: The new doc structure does not explicitly assign a permanent home for the current agent workflow rules

Sources:

- `docs/AGENTS.MD`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-tasks.md`

Why this matters:

The current `docs/AGENTS.MD` contains repo workflow rules that are not backend-specific:

- Plan -> Clarify -> Task it out -> Audit -> Implement
- required plan format
- approval gating before implementation

The new direction says:

- root `AGENTS.md` should be a router/entrypoint
- `docs/BACKEND.md` should hold backend guidance
- `docs/AGENTS.MD` may be trimmed, redirected, or retired

What is missing:

Neither the plan nor the task breakdown explicitly says where those workflow rules will live after the merge. If root `AGENTS.md` becomes only a pointer and `docs/AGENTS.MD` becomes only a stub, the repo can accidentally lose the current mandatory workflow contract.

Recommended adjustment:

- Add an explicit documentation ownership decision for workflow rules:
  - either keep them in root `AGENTS.md`
  - or move them to a dedicated workflow doc and have root `AGENTS.md` route to it
- Update the task breakdown so the merge of `docs/AGENTS.MD` names the destination for:
  - workflow/gating rules
  - planning format
  - audit/implement transition rules

### 2. High: The Ardalis migration task assumes repository contracts should use Ardalis Result, but that is not yet grounded in the architecture contract

Sources:

- `docs-ai/arch.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-tasks.md`

Why this matters:

The architecture examples clearly say:

- controllers map `Request -> Command/Query`
- services accept commands/queries and return results
- repositories encapsulate persistence

The plan/tasks currently say to migrate both service and repository contracts to Ardalis Result. That may be correct for this repo, but it is not explicitly justified by the target architecture. Locking that in now can accidentally make the repository layer look like another application boundary rather than a persistence abstraction.

What is missing:

- a repo-level rule that says whether repositories should return Ardalis Result, plain entities/DTOs, exceptions, or a mixed pattern

Recommended adjustment:

- Add a short architecture decision in `docs/BACKEND.md` and the plan/task docs that states one of:
  - repositories return Ardalis Result in this repo
  - repositories do not return Ardalis Result; only services do
  - repositories may return Ardalis Result only for expected persistence/business-safe failures
- Narrow Task 5 language to match that decision before implementation starts

### 3. Medium: The task breakdown still contains vocabulary the user already rejected

Source:

- `docs-ai/2026-03-26-services-docs-architecture-alignment-tasks.md`

Why this matters:

The user explicitly pushed back on vocabulary outside the agreed architecture naming. The tasks file still says the docs review should ensure examples do not reintroduce terms like `ReadModel` or `Projection`.

That is small, but it shows the task file has not been fully normalized to the agreed vocabulary and can confuse implementers.

Recommended adjustment:

- Remove those terms from the task file entirely
- Keep the acceptance rule simple:
  - examples must use only the approved architecture vocabulary

### 4. Medium: Orders verification is under-specified compared with the amount of structural change planned

Sources:

- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-tasks.md`

Why this matters:

The clarified decision is to refactor `AuthServer` and `Orders` equally in the first pass. But the verification strategy remains asymmetric:

- `AuthServer` has strong integration coverage
- `Orders` mostly has seed-focused unit tests plus manual smoke

Given the planned changes in `Orders`:

- repository moves
- `ViewModel` removal
- validator introduction
- controller/service/result-contract changes

there is a meaningful regression risk without at least a small automated endpoint-level verification layer.

Recommended adjustment:

- Strengthen Task 7 or Task 10 with one of:
  - a small `Orders` integration/smoke test project
  - or a clearly defined minimal API verification suite for core endpoints
- If the team chooses not to add it now, document that as an explicit risk acceptance rather than leaving it implicit

### 5. Medium: The root AGENTS router does not yet cover the full doc-routing promise

Sources:

- user clarification in chat
- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-tasks.md`

Why this matters:

The requested direction was to create an entrypoint `AGENTS.md` in the root that redirects agents to the correct document “for whatever he need to do”. The current task breakdown routes to:

- backend
- architecture
- development
- deployment
- testing

but it does not explicitly route to:

- frontend guidance in `docs/FRONTEND.md`
- the workflow rules destination once `docs/AGENTS.MD` is merged/reduced

Recommended adjustment:

- Extend Task 2 so root `AGENTS.md` also routes to:
  - `docs/FRONTEND.md` for frontend work
  - the canonical workflow-rules destination

## Plan Sections That Need Strengthening

### Architecture / Approach

Needs one explicit rule about repository return contracts relative to Ardalis Result.

### Documentation consolidation

Needs one explicit rule saying where the current Plan/Clarify/Task/Audit/Implement workflow will live after the doc restructure.

### Testing Strategy

Needs a sharper decision on minimal automated verification for `Orders`, not only manual smoke.

## Task Areas That Are Too Vague

### Task 2

The routing destinations are not complete enough yet. It should name the final home of workflow rules and likely include `docs/FRONTEND.md`.

### Task 5

The repository-contract migration language is too broad until the repo decides whether repositories should return Ardalis Result.

### Task 10

“simple checks or focused tests” is reasonable, but it should name at least one required lightweight enforcement target to avoid being skipped.

Recommended minimum:

- detect backend `ViewModel` names
- detect direct controller -> repository dependency

## Recommended Adjustments Before Implementation

1. Add an explicit destination for the current agent workflow rules.
2. Decide and document whether repositories should return Ardalis Result.
3. Remove leftover rejected vocabulary from the tasks file.
4. Strengthen the `Orders` verification plan with at least one minimal automated endpoint-level check, or explicitly accept that risk.
5. Expand the root `AGENTS.md` routing contract to include frontend and workflow guidance.

## Sources Consulted

- `docs/AGENTS.MD`
- `docs-ai/arch.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-plan.md`
- `docs-ai/2026-03-26-services-docs-architecture-alignment-tasks.md`
