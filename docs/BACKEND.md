# Backend Guide

`docs/BACKEND.md` is the backend/.NET source of truth for architecture conventions in this repo.

## Services

- `AuthServer`
- `Orders`
- `Shared`

`Shared` is the repo's cross-service building-block layer. The name stays `Shared` even though `docs-ai/arch.md` uses a `BuildingBlocks` example name.

## Folder Responsibilities

| Folder | Purpose | Allowed Dependencies | Avoid |
| --- | --- | --- | --- |
| `Controllers/` | HTTP endpoints, auth context extraction, request/response mapping | services, request/response mappers, shared HTTP helpers | repositories, `DbContext`, business rules |
| `Services/` | business/application logic, orchestration, rule enforcement | repositories, validators, shared cross-cutting helpers | controllers, HTTP-specific concerns |
| `Validators/` | request validation for API bodies | model types, shared validation helpers | persistence access, large business workflows |
| `Models/Requests` | transport input models | simple value types, nested request models | persistence concerns |
| `Models/Responses` | transport output models | response mappers | service orchestration logic |
| `Models/Commands` | write use-case inputs | services, validators | HTTP-only concerns |
| `Models/Queries` | read use-case inputs | services, validators | HTTP-only concerns |
| `Models/Results` | service outputs | services, response mappers | controller logic |
| `Data/Entities` | EF Core entities | EF Core, mappings | controller/service transport concerns |
| `Data/Repositories` | persistence access | `DbContext`, entities, data mappings | HTTP concerns |
| `Data/Mappings` or `Data/Entities/Mappers` | entity-to-model persistence mapping | entities, result models | controller transport logic |
| `Common/` | service-local cross-cutting helpers | local service concerns | code that belongs in `Shared` |
| `Middleware/` | ASP.NET pipeline behavior | HTTP pipeline services | business workflows |
| `Composition/DependencyInjection` | DI registration and startup wiring | all local service registrations | runtime request handling |
| `Configuration/` | options/config models + validation | options binding/validation | unrelated service logic |

## Approved Exceptions

- Top-level `Configuration/` stays in backend services and is intentional.
- `AuthServer` keeps MVC/Razor assets such as `Views/` and `wwwroot/`.
- `Shared` remains the project name for cross-service building blocks.

## Naming Rules

| Type | Name Pattern | Lives In | Notes |
| --- | --- | --- | --- |
| Controller | `*Controller` | `Controllers/` | HTTP boundary only |
| Service interface | `I*Service` | `Services/Interfaces/` | service layer contract |
| Service | `*Service` | `Services/` | business/application logic |
| Repository interface | `I*Repository` | `Data/Repositories/Interfaces/` | persistence contract |
| Repository | `*Repository` | `Data/Repositories/` | persistence implementation |
| Request | `*Request` | `Models/Requests/` | HTTP input |
| Response | `*Response` | `Models/Responses/` | HTTP output |
| Command | `*Command` | `Models/Commands/` | service write input |
| Query | `*Query` | `Models/Queries/` | service read input |
| Result | `*Result` | `Models/Results/` | service output |
| Entity | `*Entity` only when needed for clarity | `Data/Entities/` | persistence model |
| Validator | `*Validator` | `Validators/` or `Configuration/` | validation concern |
| Mapper | `*Mapper` / `*Mappers` | near the mapping concern | do not create vague catch-all mappers |

`ViewModel` is not part of the active backend vocabulary in this repo.

## Validation Strategy

- Use FluentValidation for API body-bound request models.
- Register validators through DI only.
- Do not enable automatic MVC/ASP.NET FluentValidation behavior in this repo.
- API controllers run validators explicitly and convert failures into Ardalis `Result.Invalid`.
- Controllers then rely on shared `Shared.Common` helpers to map invalid results to ProblemDetails.
- Route values, auth-derived values, and query parameters stay on explicit controller/shared guard helpers unless they are intentionally modeled as request objects.
- MVC/Razor flows, such as `AccountController`, stay on the existing `ModelState` path unless a later change explicitly moves them.

## Model Shape Rule

Prefer concise positional records for DTO-style backend models where practical:

```csharp
public sealed record Person(string FirstName, string LastName);
```

Default this to:

- `Request`
- `Response`
- `Command`
- `Query`
- `Result`

Keep these as `class`:

- EF/Identity entities
- `DbContext`
- controllers
- services
- repositories
- middleware

Allowed exceptions:

- framework-bound MVC form models that need mutable setters
- models where positional syntax materially hurts readability or tooling compatibility

### Rules enforced by tests

- An API action parameter bound from the request body must use a top-level backend
  record class named `*Request`, declared in a matching file such as `CreateOrderRequest.cs`.
  This applies to explicit `[FromBody]` parameters and body binding inferred by MVC.
  If the payload needs a collection or dictionary, declare it on the request record.
- Handwritten C# files in `AuthServer`, `Orders`, and `Shared` may declare at most one
  top-level class or record class. Nested types and generated source are outside this rule.

MVC/Razor account form models can keep mutable setters. See [Testing](TESTING.md)
for the architecture suite and how to run it.

## Result Strategy

- Use `Ardalis.Result` at the service layer.
- Services return `Result` / `Result<T>`.
- Repositories do not return Ardalis results.
- Repositories return persistence/domain values, nullable values, tuples, or throw for unexpected infrastructure failures.
- Controllers translate Ardalis results to HTTP responses through shared helpers in `Shared.Common`.
- Validation failures should enter that same path by turning FluentValidation failures into `Result.Invalid`.

## Mapping Flow

Preferred flow:

`HTTP Request -> Controller(Request) -> Command/Query -> Service -> Result -> Response -> HTTP Response`

Additional mapping rules:

- request/response mapping belongs near `Models/Mappers`
- entity/persistence mapping belongs under `Data`
- services should not return entities to controllers

## Shared vs Local Ownership

Put code in `Shared` only when it is genuinely cross-service and reusable, such as:

- authorization helpers
- correlation/logging helpers
- problem-details/result translation helpers
- generic validation/utilities used by multiple services

Keep code service-local when it is specific to one backend service, such as:

- service-specific mappers
- service-specific validators
- service-specific configuration
- business rules and orchestration

## Current Repo Mapping

### `AuthServer`

- Keeps `Views/`, `wwwroot/`, and account flow MVC behavior
- Uses `Composition/DependencyInjection` for DI setup
- Uses `Configuration/` as a top-level exception

### `Orders`

- Repositories live under `Data/Repositories`
- Entity mapping stays under `Data/Entities/Mappers`
- Validators belong in `Validators/`
- Controllers should delegate business-rule validation to validators/services

## Review Checklist

- No controller directly depends on a repository.
- No active backend contract uses `ViewModel`.
- New DTO-style backend models use positional `sealed record` syntax unless there is a documented reason not to.
- `Program.cs` stays orchestration-focused and pushes registrations into `Composition`.
