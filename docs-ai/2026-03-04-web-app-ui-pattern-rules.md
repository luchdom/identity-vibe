# Web App UI Pattern Rules (Locked Defaults)

Date: 2026-03-04
Status: Active default rules for `src/web-app`

## Purpose

Define stable UI defaults for the migration so every new page follows the same density, control style, and interaction pattern.

## Global Principle

If a choice is compact vs medium (or similar density choice), default to **compact / Minimal style**.

## Locked Defaults

### 1. Toolbar and List Controls

- Baseline style: compact toolbar controls
- Variant: `outlined`
- Size: `small`
- Control height: `36px`
- Radius: subtle (`6px–8px`)
- Toolbar labels: placeholders only
- No floating labels in toolbars
- Empty dropdown text uses placeholder tone (`text.secondary`)
- Selected dropdown text uses normal text color

### 2. List Page Header Pattern

- Header density: compact
- Structure:
  - page title
  - compact breadcrumb line
  - primary action button on the right
- Avoid large subtitle blocks in list pages unless explicitly requested

### 3. Primary Actions (List Pages)

- Default button style: `contained` + `color="inherit"`
- Keep action control compact (`small` size)
- Use icon + label where useful (`Add user`, `New order`)

### 4. Filter Toolbar Pattern

- Pattern:
  - search input
  - filter icon button
  - optional `Clear` button
- No `Apply` text button by default
- Enter key in search input should trigger filtering

### 5. Status Tabs Pattern

- Minimal tabs with thin indicator
- Small soft count badge in each tab
- Compact spacing and typography

### 6. Table Pattern

- Default density: compact rows
- Row actions: quick edit icon + 3-dot overflow menu
- Sorting: enabled where relevant through sortable headers
- Pagination: include rows-per-page selector for list tables

### 7. Badge/Label Pattern

- Use soft labels only for status and role tags
- Keep labels compact
- Avoid heavy outlined badges for default status/tag usage

### 8. Forms and Dialogs (Create/Edit)

- Use MUI `TextField` `variant="outlined"` with floating `label`
- This applies to forms/dialogs, not list filter toolbars

### 9. Destructive Actions

- Keep destructive actions in overflow menus by default
- Use danger color styling
- Require confirmation for irreversible actions

## Implementation Guardrails

- Do not mix medium and compact control heights in the same toolbar
- Do not mix floating-label controls with placeholder-only controls in the same toolbar
- Keep one visual rhythm per surface:
  - compact paddings
  - compact gaps
  - consistent radius
- When uncertain, copy the pattern used on Minimal list pages first, then adapt behavior to project contracts

## Immediate Follow-up Rule

Before implementing new list pages (`Roles`, `Orders` list refresh), verify these checks:

1. Toolbar controls all `36px` high.
2. Toolbar dropdowns use placeholder behavior (no floating labels).
3. Primary list action uses `contained` + `color="inherit"`.
4. Row action pattern is quick edit + overflow menu.
5. Badges are soft and compact.

