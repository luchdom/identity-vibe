# Web App Loading UX - Phase 2 Implementation Notes

Date: 2026-03-04  
Scope: `src/web-app`  
Status: Completed

## Implemented

1. Applied loading-state contract (`isInitialLoading` vs `isRefetching`) to core data pages:
   - `src/web-app/app/(dashboard)/users/page.tsx`
   - `src/web-app/app/(dashboard)/users/roles/page.tsx`
   - `src/web-app/app/(dashboard)/orders/page.tsx`
   - `src/web-app/app/(dashboard)/orders/[id]/page.tsx`

2. Standardized non-blocking refetch behavior:
   - Existing table/content stays mounted during refetch.
   - Compact inline progress indicators (`LinearProgress` + caption where appropriate).
   - Initial spinner reserved for first-load only.

3. Added consistent inline load error feedback with retry actions:
   - Users list/roles-filter load errors: inline `Alert` + `Retry`.
   - Roles/scopes load errors: inline `Alert` + `Retry`.
   - Orders list/detail load errors: inline `Alert` + `Retry`.

## UX Outcomes

- Users no longer see full table replacement during ordinary filter/pagination refresh.
- Roles page keeps current role context visible while list/scope data refreshes.
- Orders list/detail now provide compact progress feedback rather than hard loading resets.

## Validation

- `npm run lint` passed
- `npm run build` passed

## Next Candidate Phase

- Phase 3: add lightweight query cache (`orders`, `users`, `roles`) with TTL + mutation invalidation to make return visits and repeated filters feel near-instant.

