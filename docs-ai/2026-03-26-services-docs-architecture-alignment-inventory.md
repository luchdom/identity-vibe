# Services And Docs Architecture Alignment Inventory

## Backend Shape Snapshot

### AuthServer

- Top-level architecture is partly aligned already: `Controllers`, `Services`, `Data`, `Models`, `Configuration`
- `Program.cs` still owns most DI and startup wiring
- Active backend `ViewModel` names still exist:
  - `Models/ViewModels/UserViewModel.cs`
  - `Models/ViewModels/AuthenticatedUserViewModel.cs`
- MVC/Razor assets are present and intentional:
  - `Views/`
  - `wwwroot/`

### Orders

- Repository implementation is still outside `Data/`:
  - `Repositories/OrdersRepository.cs`
  - `Repositories/Interfaces/IOrdersRepository.cs`
- Active backend `ViewModel` names still exist heavily under `Models/ViewModels/`
- Repositories currently own both persistence and result semantics
- `Program.cs` still owns DI and startup wiring

### Shared

- Cross-service helpers are in reasonable shape overall
- Custom result stack is still active in:
  - `Common/Result.cs`
  - `Common/ResultVoid.cs`
  - `Common/ResultExtensions.cs`
  - `Common/AsyncResultExtensions.cs`

## Result Usage Hotspots

- `AuthServer/Services/RoleAdminService.cs`
- `AuthServer/Services/UserAdminService.cs`
- `Orders/Services/OrdersService.cs`
- `Orders/Services/IdempotencyService.cs`
- `Orders/Repositories/OrdersRepository.cs`
- `Shared/Extensions/ValidationExtensions.cs`

## ViewModel Rename Map

### AuthServer

- `UserViewModel` -> `UserResult`
- `AuthenticatedUserViewModel` -> `AuthenticatedUserResult`

### Orders

- `CustomerViewModel` -> `CustomerResult`
- `OrderItemViewModel` -> `OrderItemResult`
- `OrderStatusHistoryViewModel` -> `OrderStatusHistoryResult`
- `OrderStatusViewModel` -> `OrderStatusDetailsResult`
- `OrderViewModel` -> `OrderDetailsResult`
- `PaginationViewModel` -> `PaginationResult`
- `ShippingViewModel` -> `ShippingResult`

## Docs Cleanup Inventory

| Path | Decision | Notes |
| --- | --- | --- |
| `AGENTS.md` | add | canonical router + workflow rules |
| `docs/BACKEND.md` | add | backend source of truth |
| `docs/AGENTS.MD` | trim | redirect to root `AGENTS.md` |
| `docs/ARCHITECTURE.md` | keep/clean | runtime topology, boundaries, exceptions |
| `docs/DEVELOPMENT.md` | keep/trim | local dev, links to canonical backend/frontend docs |
| `docs/DEPLOYMENT.md` | keep/trim | deployment/runtime notes |
| `docs/TESTING.md` | keep/trim | testing strategy and commands |
| `docs/FRONTEND.md` | keep | frontend source of truth |
| `docs/LOGGING_IMPLEMENTATION.md` | keep/trim | focused logging note, not architecture source |
| `README.md` | update | onboarding + canonical doc links |
| `CLAUDE.md` | update | tool-facing quick pointers aligned to canonical docs |
| `DOCKER.md` | trim | short Docker note that points to deployment/development docs |

## Code-to-Target Mapping

| Current | Target |
| --- | --- |
| `Orders/Repositories/*` | `Orders/Data/Repositories/*` |
| `Orders/Repositories/Interfaces/*` | `Orders/Data/Repositories/Interfaces/*` |
| `*/Models/ViewModels/*` | `*/Models/Results/*` with explicit result naming |
| DI registrations in `Program.cs` | `Composition/DependencyInjection/ServiceCollectionExtensions.cs` |
| `Shared.Common.Result*` | `Ardalis.Result` service-layer usage + shared HTTP translation helpers |
