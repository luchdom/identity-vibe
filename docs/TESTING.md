# Testing Guide

## Backend Test Split

- `tests/backend/AuthServer.IntegrationTests`
  - auth flows
  - OpenIddict/Identity wiring
  - admin roles/users API behavior
- `tests/backend/Shared.UnitTests`
  - shared helpers and cross-service utilities
- `tests/backend/Orders.UnitTests`
  - seed and validator tests
- `tests/backend/Orders.IntegrationTests`
  - `Orders` endpoint smoke tests
- `tests/backend/Architecture.UnitTests`
  - executable backend architecture rules

## Commands

```bash
dotnet test tests/backend/AuthServer.IntegrationTests/AuthServer.IntegrationTests.csproj
dotnet test tests/backend/Shared.UnitTests/Shared.UnitTests.csproj
dotnet test tests/backend/Orders.UnitTests/Orders.UnitTests.csproj
dotnet test tests/backend/Orders.IntegrationTests/Orders.IntegrationTests.csproj
dotnet test tests/backend/Architecture.UnitTests/Architecture.UnitTests.csproj
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
- Use `Architecture.UnitTests` for stable rules from the [Backend Guide](BACKEND.md).
  The suite runs as part of `dotnet test IdentityVibe.sln` and needs no database.
  It uses MVC action descriptors for body binding and parses the files selected by
  each backend project's build with its configured C# language version and compiler symbols.
- When an architecture rule changes, update the Backend Guide and its test together
  so reviewers can check the change against the documented decision.

## Common Pitfalls

- Missing Docker/Testcontainers availability for integration tests
- Wrong content root in `WebApplicationFactory`
- Redirect URI mismatch in auth tests
- Assuming scope claims are delivered in only one format
- Accidentally re-enabling automatic MVC model-state short-circuiting while controllers are expected to own validation responses
