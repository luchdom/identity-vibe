# Web App Loading UX - Phase 3 Implementation Notes

Date: 2026-03-04  
Scope: `src/web-app`  
Status: Completed

## Implemented

1. Added lightweight in-memory query cache with TTL:
   - `src/web-app/lib/cache/query-cache.ts`
   - Default TTL: 45 seconds
   - Supports `get`, `set`, `invalidate`, `invalidatePrefix`, `clear`

2. Added shared query key helpers:
   - `src/web-app/lib/cache/query-keys.ts`
   - Keys/prefixes for:
     - users list and available roles
     - roles list, role detail, available scopes
     - orders list, order detail, status counts

3. Integrated cache read-before-fetch + background revalidation:
   - Users:
     - `src/web-app/app/(dashboard)/users/page.tsx`
   - Roles & permissions:
     - `src/web-app/app/(dashboard)/users/roles/page.tsx`
   - Orders list:
     - `src/web-app/app/(dashboard)/orders/page.tsx`
   - Order detail:
     - `src/web-app/app/(dashboard)/orders/[id]/page.tsx`

4. Added mutation-based cache invalidation:
   - User create/update/roles/password/status mutations invalidate users list cache prefix.
   - Role create/update/scopes mutations invalidate role list/detail cache prefixes.
   - Order create/cancel mutations invalidate orders list/status counts and refresh detail cache.
   - New order flow updated:
     - `src/web-app/components/orders/new-order-form.tsx`

## UX Outcomes

- Revisiting recently-viewed list states returns cached data instantly, then silently revalidates.
- Filter/pagination changes feel more responsive while preserving fresh data behavior.
- Mutations avoid stale list/detail displays by invalidating affected caches.

## Validation

- `npm run lint` passed
- `npm run build` passed

## Next Candidate Phase

- Phase 4: request cancellation and stale-response protection for rapid filter/page changes.

