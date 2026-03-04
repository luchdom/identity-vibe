# AuthServer Login Minimal Alignment Plan

Date: 2026-03-03

## Goal

Restyle the server-rendered AuthServer login page so it visually aligns much more closely with the Minimal sign-in page at `http://localhost:3039/sign-in`, while keeping the existing quick-user dropdown that pre-fills test credentials.

Target files:

- `src/backend/AuthServer/Views/Account/Login.cshtml`
- `src/backend/AuthServer/Views/Shared/_AuthLayout.cshtml`
- `src/backend/AuthServer/wwwroot/css/auth.css`
- `src/backend/AuthServer/wwwroot/js/auth.js`

## Visual Reference Audit

Reference page observed at `http://localhost:3039/sign-in`:

- clean split between a slim top bar and centered auth content
- logo on the left and a small “Need help?” action on the right
- compact, premium auth card area with generous whitespace
- strong heading + small supporting text block
- floating-label style fields
- inline “Forgot password?” treatment
- prominent full-width primary submit button
- separator with `OR`
- social icon row below the separator
- minimal, airy layout without heavy borders or dense copy

Current AuthServer login page:

- simple centered white card
- generic heading/subheading
- visible test credential dropdown above the form
- standard labels/inputs with basic styling
- no top bar, no refined layout, no secondary affordances

## Constraint To Preserve

Keep the current “Quick Login (Testing)” dropdown behavior:

- select Admin User
- select Regular User
- select Custom/manual entry
- continue filling `Email` and `Password` via `fillCredentials()`

This is a functional requirement, not optional polish.

## Design Approach

Do not try to clone the React/MUI Minimal sign-in page exactly.

Instead:

- reproduce the same visual hierarchy
- reproduce the same spacing rhythm
- reproduce the same overall composition
- keep the existing Razor form and server post behavior intact

This should remain a server-rendered Razor page with only light JS for the credential autofill and any optional password-toggle behavior.

## Implementation Plan

## Phase 1: Rebuild The Auth Layout Shell

Objective:

Make `_AuthLayout.cshtml` match the broader composition of the Minimal sign-in screen.

Tasks:

1. Add a top auth bar inside `_AuthLayout.cshtml`.
   - left: product/logo area
   - right: a small helper action such as “Need help?”
2. Keep the main auth content vertically centered below the top bar.
3. Expand the layout width slightly so it feels less cramped than the current narrow 400px card.
4. Preserve the existing shared layout integration and script/style loading.

Definition of done:

- the page no longer feels like a plain centered box
- the overall screen composition resembles the reference

## Phase 2: Restructure The Login Markup

Objective:

Adjust `Login.cshtml` markup so the content blocks map cleanly to the Minimal structure.

Tasks:

1. Rework the heading area:
   - title: “Sign in”
   - small supporting line beneath it
2. Keep validation summary, but style it as a compact inline alert rather than a large generic block.
3. Move the quick-user dropdown into a lighter “testing helper” block instead of treating it like a normal primary form field.
4. Keep the core form fields:
   - email
   - password
   - hidden `ReturnUrl`
5. Keep the submit button as the primary action.
6. Add a visual separator block (`OR`) below the submit action.
7. Add a footer row for secondary auth-style icon buttons or placeholder actions if desired.
   - these can be decorative/non-functional in v1
8. Keep the existing register link/footer only if it still fits the refined design.
   - if not, fold it into the supporting text near the heading to better match Minimal

Definition of done:

- the page content hierarchy matches the reference structure
- the quick-login helper remains available without dominating the layout

## Phase 3: Rewrite Auth CSS

Objective:

Replace the current utilitarian auth CSS with a more Minimal-like visual treatment.

Tasks:

1. Redesign the page background.
   - softer background color
   - optional subtle gradient or ambient glow
2. Redesign the auth wrapper:
   - larger radius
   - softer shadow
   - more refined internal spacing
   - less “basic white card” feel
3. Style the top auth bar.
4. Improve heading and supporting text typography.
5. Restyle inputs to feel closer to the reference:
   - stronger labels
   - cleaner padding
   - better focus states
   - more premium border/radius treatment
6. Restyle the submit button:
   - full width
   - stronger prominence
   - better height and radius
7. Add styling for:
   - divider with `OR`
   - small icon buttons row
   - compact helper links
   - testing helper block for quick login
8. Make the layout work cleanly on mobile and desktop.

Definition of done:

- the Razor login page feels visually aligned with Minimal
- the page no longer looks like a default server form

## Phase 4: Preserve And Refine Quick Login UX

Objective:

Keep the current test credential flow, but visually integrate it better.

Tasks:

1. Keep the current `fillCredentials()` behavior in `auth.js`.
2. Keep the current element IDs:
   - `credential-select`
   - `email-input`
   - `password-input`
3. Optionally relabel the section so it reads as a testing convenience rather than a primary field.
4. Style the dropdown as a compact helper control.
5. Ensure the dropdown still supports:
   - admin
   - user
   - custom
6. Ensure switching back to `custom` still clears the inputs.

Definition of done:

- test users can still be selected quickly
- the helper control feels intentional and visually subordinate to the main sign-in UI

## Phase 5: Optional Small Interaction Polish

Objective:

Add only low-risk enhancements that improve parity with the reference without complicating the server form.

Tasks:

1. Optionally add a password visibility toggle.
2. Add cleaner focus/hover states for links and buttons.
3. Ensure validation errors remain readable in the new layout.
4. Keep anti-forgery and normal form post behavior unchanged.

Definition of done:

- the page feels polished but remains simple and reliable

## What Should Not Change

Do not change these unless there is a real backend/auth requirement:

- controller actions
- POST target and model binding
- anti-forgery token usage
- `ReturnUrl` behavior
- credential autofill logic contract
- auth flow semantics

This is a presentation-layer redesign, not an auth behavior rewrite.

## Recommended Implementation Order

1. Update `_AuthLayout.cshtml`
2. Restructure `Login.cshtml`
3. Rewrite `auth.css`
4. Lightly refine `auth.js` only if needed
5. Validate the full login flow end-to-end

## Acceptance Criteria

The page is successful when:

- it visually reads as a Minimal-style sign-in page
- it still submits through the existing server-side login flow
- the quick-user dropdown still pre-fills credentials correctly
- `ReturnUrl` still posts correctly
- validation errors still render clearly
- mobile layout remains usable
