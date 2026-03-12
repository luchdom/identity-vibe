# Logging Implementation Notes

## Services

- `AuthServer`
- `Orders`
- `web-app` (browser/client-side logging)

## Backend Logging

Both backend services use:

- Serilog JSON console/file sinks
- Correlation ID enrichment
- OpenTelemetry middleware/instrumentation

## Correlation IDs

- Frontend generates and persists a correlation ID in `sessionStorage`
- Frontend sends `X-Correlation-ID` on API requests
- Backend services include correlation IDs in logs and problem details responses

## Validation

- Trigger requests from `web-app`
- Check `AuthServer` and `Orders` logs for the same correlation ID across related requests
