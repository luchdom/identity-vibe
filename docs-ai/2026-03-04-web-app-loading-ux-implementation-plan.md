# Web App Loading UX Detailed Implementation Plan

Date: 2026-03-04  
Target app: `src/web-app`  
Objective: eliminate “page freeze” perception during navigation/filter/pagination by standardizing loading behavior and improving client-side transition UX.

## 1. Problem Statement

Users experience apparent freezes when navigating between pages or applying filters.  
Root causes are primarily UX/perception issues:

- no consistent global navigation loading signal
- inconsistent local loading states across pages
- content hard-resets during refetch (blank/loading flash)
- no caching for recently requested list queries
- no cancellation/guarding for rapid repeated actions

## 2. Scope

Included:

- global route-transition loading UI
- page-level loading/error/empty/success contract
- list-page stale-while-refetch behavior
- lightweight query cache for key list endpoints
- request cancellation to prevent stale updates
- Playwright coverage for loading indicators
- docs updates for mandatory loading UX rules

Not included:

- migration to React Query/SWR in this phase
- backend API contract changes
- redesign of auth model

## 3. Technical Principles

- Keep data fetching client-side in feature pages.
- Show immediate feedback for every user action with network impact.
- Prefer “keep current content + subtle loading state” over full-screen reset.
- Avoid double-submit and repeated route/action clicks while pending.
- Preserve existing auth/scope guard behavior.

## 4. Detailed Phases

## Phase 0: Baseline and Observability

Goal: identify where latency is perceived and establish measurable baseline.

Tasks:

1. Add request duration timing in shared API interceptors:
   - file: `src/web-app/lib/api.ts`
   - record start timestamp in request interceptor
   - log method/url/status/duration in dev mode
2. Add route transition timing helper:
   - new file: `src/web-app/lib/perf/navigation-metrics.ts`
   - capture timestamp before router push/replace for instrumented navigations
   - log total transition duration after destination mounts
3. Add temporary console diagnostics to high-traffic pages:
   - `/orders`
   - `/users`
   - `/users/roles`

Acceptance:

- dev console shows API durations and transition durations
- team can list top 3 slowest perceived interactions

---

## Phase 1: Global Navigation Loading UX

Goal: always show immediate “navigation is in progress” feedback.

Tasks:

1. Add shell-level navigation pending state:
   - file: `src/web-app/components/app-shell/app-shell.tsx`
   - expose `isNavigating` state from a small `NavigationProgressProvider`
2. Add top progress bar:
   - new component: `src/web-app/components/common/navigation-progress-bar.tsx`
   - slim bar under header, visible while route transition is pending
3. Add route-segment loading fallbacks:
   - `src/web-app/app/(dashboard)/loading.tsx`
   - `src/web-app/app/(auth)/loading.tsx`
   - use compact skeleton layout consistent with current UI rules
4. Wrap action navigations in transitions:
   - use `startTransition` where user clicks trigger route changes
   - disable repeated clicks while transition pending

Acceptance:

- every route change shows loading indication within 100ms
- repeated rapid clicks do not produce UI lock perception

---

## Phase 2: Standard Page Loading Contract

Goal: same loading behavior on all data pages.

Tasks:

1. Define standard local states contract:
   - `isInitialLoading`
   - `isRefetching`
   - `error`
   - `data`
2. Apply contract to:
   - `src/web-app/app/(dashboard)/orders/page.tsx`
   - `src/web-app/app/(dashboard)/users/page.tsx`
   - `src/web-app/app/(dashboard)/users/roles/page.tsx`
   - `src/web-app/app/(dashboard)/orders/[id]/page.tsx`
3. Keep current content mounted while refetching:
   - show lightweight progress indicator in toolbar/card header
   - avoid replacing table with full-screen spinner on every query change
4. Standardize error panels:
   - compact inline `Alert` + retry action
   - no raw/unstructured error text

Acceptance:

- page never “goes blank” during normal refetch operations
- all listed pages follow identical loading/error patterns

---

## Phase 3: Lightweight Cache (No New Library)

Goal: show instant previously-fetched results while revalidating.

Tasks:

1. Implement in-memory cache utility:
   - new file: `src/web-app/lib/cache/query-cache.ts`
   - key-value map with TTL (e.g. 30–60s)
   - helpers: `get`, `set`, `invalidatePrefix`
2. Create stable query key helpers:
   - new file: `src/web-app/lib/cache/query-keys.ts`
   - keys for:
     - orders list (`status/page/pageSize`)
     - users list (`search/status/role/page/pageSize`)
     - roles list/search
3. Integrate cache read-before-fetch:
   - if cache hit: render immediately, mark as `isRefetching`
   - if miss: do normal initial loading
4. Invalidate relevant keys after mutations:
   - user create/edit/roles/password/status
   - role create/update/scopes
   - order create/cancel/status changes

Acceptance:

- returning to recently visited lists is near-instant
- fresh data still appears after background refresh

---

## Phase 4: Request Cancellation and Stale-Update Protection

Goal: prevent outdated responses from overriding newer state.

Tasks:

1. Add cancellation support in API layer:
   - use Axios `AbortController`/signal for GET requests
2. Cancel in-flight requests on filter/page changes:
   - keep newest request active, cancel previous request
3. Add stale-response guards:
   - request id token per fetch cycle
   - ignore late responses for obsolete request id

Acceptance:

- rapid filter/page changes do not cause flicker or wrong final data
- no stale response overwrites latest selected state

---

## Phase 5: Prefetch Strategy

Goal: reduce perceived wait time for common next navigations.

Tasks:

1. Add route prefetch for primary nav links and frequent detail links:
   - sidebar nav items
   - top actions like `New order`
2. Prefetch detail route on row hover/focus (bounded):
   - orders row hover -> prefetch `/orders/[id]`
3. Avoid aggressive prefetch on low-bandwidth patterns:
   - debounce hover prefetch
   - skip duplicate in-session prefetch

Acceptance:

- common “next click” routes feel faster
- no network storm from prefetch behavior

---

## Phase 6: UX Feedback Thresholds

Goal: reduce user anxiety for slower requests.

Tasks:

1. Add delayed “Still loading…” helper:
   - show only if request > ~1200ms
2. Keep feedback subtle and non-blocking:
   - small caption in toolbar/card header
3. Reserve full blocking state only for first-load screens

Acceptance:

- long requests clearly communicate progress without harsh UI reset

---

## Phase 7: Testing and Verification

Goal: prevent regressions.

Tasks:

1. Add Playwright tests with artificial API delay:
   - assert progress indicator appears on navigation
   - assert table remains visible during refetch (stale-while-refetch)
2. Add tests for rapid filter changes:
   - final state matches latest selection
3. Add mutation invalidation tests:
   - post-create/edit actions refresh lists correctly

Acceptance:

- loading UX behavior is covered in test suite
- no regressions in auth guard redirects

---

## Phase 8: Documentation Updates

Goal: enforce consistency for future implementation.

Tasks:

1. Update `docs/FRONTEND.md`:
   - add “Loading UX Rules” section
2. Update `docs/DEVELOPMENT.md` quick checklist:
   - require loading indicator for route and data transitions
3. Add short implementation recipe:
   - where to place progress indicators
   - how to apply cache + cancellation

Acceptance:

- new pages follow the same loading UX standard by default

## 5. File-Level Task Map

Core infrastructure:

- `src/web-app/lib/api.ts`
- `src/web-app/lib/cache/query-cache.ts` (new)
- `src/web-app/lib/cache/query-keys.ts` (new)
- `src/web-app/lib/perf/navigation-metrics.ts` (new)

Global UI:

- `src/web-app/components/app-shell/app-shell.tsx`
- `src/web-app/components/common/navigation-progress-bar.tsx` (new)
- `src/web-app/app/(dashboard)/loading.tsx` (new)
- `src/web-app/app/(auth)/loading.tsx` (new)

Page integrations:

- `src/web-app/app/(dashboard)/orders/page.tsx`
- `src/web-app/app/(dashboard)/orders/[id]/page.tsx`
- `src/web-app/app/(dashboard)/users/page.tsx`
- `src/web-app/app/(dashboard)/users/roles/page.tsx`

Testing:

- `src/web-app/tests/*` (new or expanded)

Docs:

- `docs/FRONTEND.md`
- `docs/DEVELOPMENT.md`

## 6. Rollout Order (Recommended)

1. Phase 1 (global transition loading)
2. Phase 2 (page loading contract)
3. Phase 4 (cancellation/stale guards)
4. Phase 3 (cache integration)
5. Phase 5 (prefetch)
6. Phase 6 (slow-request messaging)
7. Phase 7 + 8 (tests/docs)

Rationale: visible UX win first, then data-consistency hardening, then speed optimization.

## 7. Risks and Mitigations

Risk: over-fetch or cache stale data  
Mitigation: TTL + mutation-triggered invalidation + explicit refresh actions

Risk: added complexity in each page  
Mitigation: shared helper utilities and a standard state contract

Risk: false perception of “stuck” if indicator not cleared  
Mitigation: always bind indicator to actual transition/request lifecycle with safe finally blocks

## 8. Definition of Done

- Navigation always provides immediate feedback
- Data pages never appear frozen/blank during refetch
- Rapid user interactions cannot produce stale final UI
- Recent query revisit feels instant due to cache
- Loading UX behavior is tested and documented

