# Web App Loading UX - Phase 1 Implementation Notes

Date: 2026-03-04  
Scope: `src/web-app`  
Status: Completed

## Implemented

1. Added shared navigation progress context/provider:
   - `src/web-app/contexts/navigation-progress-context.tsx`
   - Supports `isNavigating` + `startNavigation()`
   - Uses route settle logic + safety timeout
   - Wrapped with `Suspense` to keep App Router static prerender compatibility

2. Wired provider in root layout:
   - `src/web-app/app/layout.tsx`

3. Added global progress UI:
   - `src/web-app/components/common/navigation-progress-bar.tsx`
   - Mounted in `src/web-app/components/app-shell/header.tsx`

4. Added transition-aware navigation triggers:
   - Sidebar item clicks:
     - `src/web-app/components/app-shell/sidebar.tsx`
   - Account/Profile and Logout menu actions:
     - `src/web-app/components/app-shell/account-menu.tsx`
   - Orders route/query navigations with `useTransition`:
     - `src/web-app/app/(dashboard)/orders/page.tsx`
     - `src/web-app/app/(dashboard)/orders/new/page.tsx`
     - `src/web-app/app/(dashboard)/orders/[id]/page.tsx`

5. Added route-segment loading fallbacks:
   - `src/web-app/app/(dashboard)/loading.tsx`
   - `src/web-app/app/(auth)/loading.tsx`

## Validation

- `npm run lint` passed
- `npm run build` passed

## Notes

- This phase focuses on immediate route-transition feedback and reduced freeze perception.
- Phase 2 should standardize stale-while-refetch behavior across list pages (`users`, `roles`, `orders`) to avoid replacing loaded tables with full-screen spinners during refreshes.

