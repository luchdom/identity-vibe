# Backend Architecture Conventions

## General architecture

Each backend API should use:

- a clear folder structure by technical responsibility
- optional shared BuildingBlocks project for cross-cutting abstractions
- a simple **service-layer architecture**
- no DDD patterns, no aggregates, no domain events unless explicitly needed
- business logic should live primarily in services
- data access should stay inside Data folders
- controllers must not access DbContext directly
- services must not depend on controllers or HTTP-specific concerns

The application is organized by **technical responsibility**, not by modules.

Each API project should usually contain:

- Controllers
- Services
- Validators
- Models
- Data
- Common
- Middleware
- Composition

## Top-level structure

```text
src/
  MyApp.Api/
    Program.cs
    appsettings.json

    Composition/
      DependencyInjection/
        ServiceCollectionExtensions.cs

    Middleware/
      ExceptionHandlingMiddleware.cs
      TenantResolutionMiddleware.cs
      RequestContextMiddleware.cs

    Common/
      Authorization/
      MultiTenancy/
      Results/
      Exceptions/
      Mapping/

    Controllers/
      MembershipsController.cs
      TenantsController.cs
      SubscriptionsController.cs
      AuthController.cs

    Services/
      Interfaces/
        IMembershipService.cs
        ITenantService.cs
        ISubscriptionService.cs
        IAuthService.cs

      MembershipService.cs
      TenantService.cs
      SubscriptionService.cs
      AuthService.cs

    Validators/
      GetMembershipsRequestValidator.cs
      CreateTenantRequestValidator.cs
      CreateTenantCommandValidator.cs
      ChangeSubscriptionPlanCommandValidator.cs

    Models/
      Membership.cs
      Tenant.cs
      Subscription.cs
      Plan.cs
      User.cs

      Requests/
        GetMembershipsRequest.cs
        CreateTenantRequest.cs
        ChangeSubscriptionPlanRequest.cs
        LoginRequest.cs

      Responses/
        GetMembershipsResponse.cs
        CreateTenantResponse.cs
        ChangeSubscriptionPlanResponse.cs
        LoginResponse.cs

      Queries/
        GetMembershipsQuery.cs
        GetTenantByIdQuery.cs
        ListSubscriptionsQuery.cs

      Commands/
        CreateTenantCommand.cs
        ChangeSubscriptionPlanCommand.cs
        LoginCommand.cs

      Results/
        GetMembershipsResult.cs
        CreateTenantResult.cs
        ChangeSubscriptionPlanResult.cs
        LoginResult.cs

      Enums/
        MembershipStatus.cs
        SubscriptionStatus.cs
        TenantType.cs

      Constants/
        Permissions.cs
        ClaimTypes.cs
        FeatureCodes.cs

    Data/
      Entities/
        MembershipEntity.cs
        TenantEntity.cs
        SubscriptionEntity.cs
        UserEntity.cs

      Repositories/
        Interfaces/
          IMembershipRepository.cs
          ITenantRepository.cs
          ISubscriptionRepository.cs
          IUserRepository.cs

        MembershipRepository.cs
        TenantRepository.cs
        SubscriptionRepository.cs
        UserRepository.cs

      External/
        StripeBillingClient.cs
        AsaasBillingClient.cs
        OpenIddictTokenService.cs
        EmailProviderClient.cs

      Mappings/
        MembershipMappings.cs
        TenantMappings.cs
        SubscriptionMappings.cs
        AuthMappings.cs

      Configurations/
        MembershipEntityConfiguration.cs
        TenantEntityConfiguration.cs
        SubscriptionEntityConfiguration.cs
        UserEntityConfiguration.cs
Also
      ApplicationDbContext.cs

  MyApp.BuildingBlocks/
    Time/
    Results/
    Exceptions/
    Authorization/
    MultiTenancy/
    Persistence/
```

## Controllers/

**Purpose:**

- expose HTTP endpoints
- receive HTTP requests
- validate or delegate validation
- map Request -> Command/Query
- call services
- map Result -> Response
- return HTTP responses

**Rules:**

- no business logic
- no direct repository access
- no DbContext usage
- keep thin and orchestration-focused

## Services/

**Purpose:**

- contain business logic
- orchestrate repositories, external clients, and context access
- enforce business rules
- handle permissions and tenancy

**Rules:**

- accept Commands/Queries
- return Results
- no HTTP dependencies
- split when too large

## Validators/

**Purpose:**

- validate Requests and Commands

**Rules:**

- simple validation -> validators
- complex validation -> services
- avoid turning validators into business logic layer

## Models/

### Root

Shared business objects:

- Membership
- Tenant
- Subscription
- Plan
- User

### Requests/

HTTP input models

### Responses/

HTTP output models

### Queries/

Service read inputs

### Commands/

Service write inputs

### Results/

Service outputs

### Enums/

Application enums

### Constants/

Grouped constant values

## Data/

### Entities/

EF Core database models

### Repositories/

Encapsulate database access

### External/

External integrations (Stripe, Auth, Email, etc.)

### Mappings/

All object transformations

### Configurations/

EF Core configurations

### ApplicationDbContext

Single DbContext for the app

## Common/

Shared API-level concerns:

- authorization
- tenant context
- mapping helpers
- exceptions
- result wrappers

## Middleware/

Cross-cutting pipeline logic:

- exception handling
- tenant resolution
- request context

## Composition/

Dependency injection setup

## Naming conventions

### Business objects
- Membership
- Tenant
- Subscription

### Request / Response
- CreateTenantRequest
- CreateTenantResponse

### Query / Command / Result
- GetMembershipsQuery
- CreateTenantCommand
- CreateTenantResult

### Persistence
- MembershipEntity

### Repository
- IMembershipRepository
- MembershipRepository

### Service
- IMembershipService
- MembershipService

## Recommended flow

HTTP Request  
-> Controller (Request)  
-> Command / Query  
-> Service  
-> Result  
-> Response  
-> HTTP Response

## Model separation rules

Use separate types when:
- API differs from internal logic
- response hides internal fields
- formatting differs

Avoid duplication when:
- shapes are identical


- all tenant data must include `TenantId`
- resolve tenant before service execution
- enforce tenant isolation in services

## Service interaction rules

- services may call other services
- avoid circular dependencies
- keep orchestration cohesive

## Simplicity rules

- use records for DTOs
- use classes for services and entities
- avoid over-engineering
- no unnecessary abstractions
- business logic in services
- persistence in repositories
- transport in controllers
