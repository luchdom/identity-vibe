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
  It uses standard MVC registrations to discover action descriptors for body binding.
  An explicit `[FromServices]` attribute identifies action parameters resolved from
  application-specific services to this suite. The source checks use each backend
  project's compiler inputs, including its C# language version and symbols.
- Changing an established architecture rule, reducing its test coverage, or bypassing
  its check requires a recorded, owner-approved architecture decision and independent
  Staff Engineer review. Update the Backend Guide and its test together.

## Common Pitfalls

- Missing Docker/Testcontainers availability for integration tests
- Wrong content root in `WebApplicationFactory`
- Redirect URI mismatch in auth tests
- Assuming scope claims are delivered in only one format
- Accidentally re-enabling automatic MVC model-state short-circuiting while controllers are expected to own validation responses
