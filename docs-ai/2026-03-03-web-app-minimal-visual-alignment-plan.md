# Web App Minimal Visual Alignment Plan

Date: 2026-03-03

## Goal

Bring `src/web-app` much closer to the Minimal template visual language before continuing deep feature migration.

## Audit Summary

Playwright comparison confirmed the current `web-app` is still scaffold-stage:

- the shell is functional but too simple
- the dashboard is still a placeholder
- the auth page is improved, but still not close enough to the live Minimal feel
- the app still reads as a generic MUI scaffold instead of a Minimal-style dashboard

The mismatch comes from four missing layers:

1. theme tokens are too generic
2. shell proportions and density are too simple
3. dashboard composition is not implemented
4. shared surface primitives are not yet aligned to the template

## Recommended Correction

Do a dedicated visual-alignment stream before continuing major business page migrations.

Recommended sequence:

1. rebuild theme tokens
2. rework sidebar and header
3. replace the dashboard placeholder with a real composed dashboard
4. tighten auth screens
5. add shared Minimal-style surface primitives
6. clean up assets and favicon
7. run a browser polish pass with Playwright comparison

## Priority

Before continuing major feature migrations:

1. theme + shell
2. dashboard
3. auth tightening
4. assets and polish

That is the highest-leverage path to make the migration visually credible.
