# Web App Loading UX - Phase 6 Implementation Notes

Date: 2026-03-04  
Scope: `src/web-app`  
Status: Completed

## Implemented

1. Added reusable delayed-loading helper:
   - `src/web-app/hooks/use-delayed-flag.ts`
   - Exposes a boolean that turns true only after configurable delay (default 1200ms).

2. Added non-blocking slow-request hints in key pages:
   - Users list:
     - `src/web-app/app/(dashboard)/users/page.tsx`
   - Roles & permissions:
     - `src/web-app/app/(dashboard)/users/roles/page.tsx`
   - Orders list:
     - `src/web-app/app/(dashboard)/orders/page.tsx`
   - Order detail:
     - `src/web-app/app/(dashboard)/orders/[id]/page.tsx`

Behavior:
- Shows compact “taking longer than expected” captions only when loading exceeds threshold.
- Keeps content visible and interactions predictable; no new blocking overlays.

## UX Outcomes

- Long-running requests now provide explicit reassurance that work is still in progress.
- Feedback remains subtle and consistent with compact Minimal-like styling.

## Validation

- `npm run lint` passed
- `npm run build` passed

