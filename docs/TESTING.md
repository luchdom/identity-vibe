# Testing Guide

## Backend Test Split

- `tests/backend/AuthServer.IntegrationTests`
  - auth flows
  - OpenIddict/Identity wiring
  - admin roles/users API behavior
- `tests/backend/Shared.UnitTests`
  - shared helpers and cross-service utilities
- `tests/backend/Orders.UnitTests`
  - seed tests
  - lightweight architecture checks
  - minimal `Orders` endpoint smoke coverage

## Commands

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj
dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj
dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj
```

Frontend Playwright checks:

```bash
cd src/web-app
pnpm test:e2e
```

## Expectations

- Prefer real integration coverage for auth changes
- Use unit tests for shared helper behavior
- Keep at least minimal automated endpoint-level verification for `Orders`
- Cover FluentValidation-to-ProblemDetails behavior with both helper-level and endpoint-level tests
- Cover architecture guardrails where practical:
  - no active backend `ViewModel` contracts
  - controllers do not depend on repositories directly

## Common Pitfalls

- Missing Docker/Testcontainers availability for integration tests
- Wrong content root in `WebApplicationFactory`
- Redirect URI mismatch in auth tests
- Assuming scope claims are delivered in only one format
- Accidentally re-enabling automatic MVC model-state short-circuiting while controllers are expected to own validation responses
