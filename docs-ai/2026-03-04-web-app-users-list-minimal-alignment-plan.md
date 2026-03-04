# Web App Users List Minimal Alignment Plan

Date: 2026-03-04

## Reference

Target reference page:
- `https://minimals.cc/dashboard/user/list`

Observed live structure:
- page title is `List`
- breadcrumb is `Dashboard / User / List`
- top-right primary action is `Add user`
- main card starts with a tab row for status buckets with counts
- below tabs is a compact toolbar with:
  - role filter dropdown
  - search input
  - filter action button
- table has:
  - checkbox selection column
  - sortable column headers
  - compact row layout
  - soft status labels
  - small quick-edit action plus a vertical-dots overflow action
- footer has:
  - pagination
  - rows-per-page
  - dense toggle

## Goal

Refactor `src/web-app/app/(dashboard)/users/page.tsx` so it visually and structurally follows the Minimal user-list pattern much more closely, while keeping the current project behavior and backend contracts:
- keep real AuthServer data
- keep current admin actions
- keep current scope enforcement
- keep MUI-only implementation

## Scope

This is a UI and interaction-alignment pass for `/users`, not a backend contract change.

Included:
- page header alignment
- status tabs
- richer filter toolbar
- sortable table columns
- role filter with checkbox dropdown
- denser row presentation
- action column alignment

Not included:
- bulk row actions
- checkbox row selection behavior
- dense-mode persistence
- avatar uploads
- separate quick-edit drawer

## Target Changes

### 1. Page Header Alignment

Replace the current custom summary card-first intro with a Minimal-style header row:
- left:
  - title: `List`
  - breadcrumb: `Dashboard / User / List`
- right:
  - `Add user` primary action

Keep the current create-user dialog behind the button.

Implementation notes:
- add a small reusable breadcrumb line or local inline header block
- move page summary counts out of the top card and into the status tab model below

### 2. Status Tabs

Add a status tab row above the toolbar, matching the Minimal pattern.

Recommended tabs for this project:
- `All`
- `Active`
- `Disabled`

Optional later:
- `Pending`
- `Invited`

Behavior:
- each tab shows a count badge
- selecting a tab updates the active filter
- `All` maps to no `isActive` filter
- `Active` maps to `isActive=true`
- `Disabled` maps to `isActive=false`

UI notes:
- tabs should be compact and sit inside the main table card
- count badges should be small, soft labels

### 3. Filter Toolbar

Replace the current multi-field filter grid with a compact toolbar directly below the tabs.

Desired layout:
- left:
  - role filter dropdown
- center/right:
  - search input with leading search icon
  - filter button / clear button cluster

Behavior:
- search remains explicit apply, not instant-fire on every keypress
- clear resets:
  - search
  - role
  - status tab
  - page number

UI notes:
- use one horizontal toolbar inside the card
- reduce visual weight compared with the current large stacked controls

### 4. Role Filter As Checkbox Dropdown

Replace the current simple role `<select>` with a dropdown menu containing checkboxes.

Behavior:
- support multi-select role filtering
- selected roles should be summarized in the trigger label:
  - `All roles`
  - `1 role`
  - `2 roles`
- filtering logic:
  - initial implementation can use OR semantics
  - if backend only supports one role parameter, perform client-side filtering after the server response for multi-select mode, or temporarily submit only one selected role until full parity is implemented

Implementation note:
- current API accepts a single `role` query parameter
- safest v1 approach:
  - UI supports one selected role only with checkbox-style menu
  - or allow multi-select visually but apply only first selected role until API expansion

Recommended near-term choice:
- single effective selected role, presented with checkbox menu styling for Minimal fidelity

### 5. Sortable Table Columns

Add sortable headers like the Minimal page.

Initial sortable columns:
- `Email` / name column
- `Phone`
- `Created`
- optionally `Status`

Implementation strategy:
- frontend-local sorting first
- sort the currently loaded page data after fetch
- keep pagination contract unchanged for now

Important constraint:
- current backend list endpoint does not expose server-side sort parameters
- so sorting should be client-side within the current result set in v1

UI notes:
- use `TableSortLabel`
- show active sort direction in the header

### 6. Denser Row Composition

Adjust the table rows to match Minimal’s denser composition.

Target row structure:
- first data column becomes a stacked identity cell:
  - primary line: email or display name
  - secondary line: username
- phone
- role summary
- status
- created
- actions

Refinements:
- tighten cell padding
- reduce tag size further if needed
- keep status labels small and soft

Optional enhancement:
- add avatar initials at the start of the identity cell if desired, but not required for v1 parity

### 7. Action Column Alignment

Current improvement already made:
- vertical-dots menu is in place

Next refinement to better match Minimal:
- keep one small inline quick action button for `Edit`
- keep vertical-dots for secondary actions:
  - roles
  - reset password
  - enable/disable

If we want exact structural parity with Minimal:
- action cell should show:
  - quick edit icon/button
  - vertical-dots menu

Recommended implementation:
- add a small ghost `Edit` button or icon
- retain the overflow menu for the rest

### 8. Footer Controls

Make the footer look more like Minimal:
- keep pagination
- add rows-per-page selector
- optionally add a local `Dense` toggle that reduces row padding only for this page

Implementation strategy:
- rows-per-page already maps well to the current paging model
- dense toggle can be local component state only

### 9. Selection Column

The Minimal page has checkboxes for bulk selection.

For this project:
- add checkbox column visually
- row selection can remain UI-only initially
- no bulk actions required yet

This gives stronger visual parity without forcing immediate bulk-operation support.

## Recommended Phased Implementation

### Phase 1: Structural Alignment

- change page header to `List` + breadcrumb + `Add user`
- move users page into one main card container
- add status tabs with counts
- replace current filter grid with compact toolbar

### Phase 2: Filter UX Alignment

- replace role select with checkbox-style dropdown
- refine search input styling
- keep clear/apply actions compact

### Phase 3: Table Behavior Alignment

- add sortable headers
- add rows-per-page control
- add optional dense toggle
- tighten row density and cell spacing

### Phase 4: Final Action Alignment

- keep current overflow menu
- optionally add quick edit inline action
- polish status labels and table affordances

## Implementation Notes For Current Codebase

Primary file to refactor:
- `src/web-app/app/(dashboard)/users/page.tsx`

Likely supporting additions:
- small breadcrumb/header helper in `src/web-app/components/common/`
- reusable lightweight dropdown-with-checkboxes filter component
- optional table state helpers for:
  - sort key
  - sort direction
  - dense mode
  - rows per page

## Preconditions

Before visual verification, fix the current local Next dev-server chunk issue:
- stop the `web-app` dev server
- delete `src/web-app/.next`
- restart `npm run dev` in `src/web-app`

Without that, the local browser may keep showing the stale `Cannot find module './611.js'` overlay.

## Recommended Next Step

Implement this in order:
1. page header + status tabs
2. compact toolbar
3. sortable table headers
4. rows-per-page + dense toggle
5. optional inline quick edit action

