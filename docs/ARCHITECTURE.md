# Backend Architecture

<document_overview>
This document details the backend architecture patterns and best practices for the Identity System.
</document_overview>

## Service Overview

<service_architecture>
  <service name="AuthServer" port="5000">
    <component>OpenIddict Server: OAuth2/OpenID Connect with JWT token issuance</component>
    <component>ASP.NET Core Identity: User management with password policies</component>
    <component>Entity Framework Core: Data access with PostgreSQL</component>
    <component>Scope Configuration: Dynamic client/scope management via appsettings.Scopes.json</component>
  </service>

  <service name="Gateway" port="5002" pattern="BFF">
    <component>YARP Reverse Proxy: Routes requests to AuthServer and Orders</component>
    <component>BFF Authentication Controller: Handles login, refresh, and user info</component>
    <component>JWT Bearer Authentication: Validates tokens from AuthServer</component>
    <component>CORS Configuration: Supports frontend applications</component>
  </service>

  <service name="Orders" port="5003">
    <component>JWT Bearer Authentication: Token validation from AuthServer</component>
    <component>Dynamic Authorization Service: Policy registration based on configuration</component>
    <component>Multi-issuer Support: Can validate tokens from multiple providers</component>
    <component>Scope-based Access Control: Authorization policies in appsettings.json</component>
  </service>
</service_architecture>

## BFF (Backend for Frontend) Pattern

<bff_pattern>
  <mandatory_rules>
    <rule priority="critical">All external client endpoints MUST be exposed through Gateway BFF</rule>
    <rule priority="critical">Direct access to AuthServer/Orders is PROHIBITED for external clients</rule>
    <rule priority="critical">Gateway acts as single entry point for all client requests</rule>
  </mandatory_rules>

  <routing_strategy>
```csharp
// ✅ Correct - External clients access through Gateway BFF
POST /auth/login          -> Gateway -> AuthServer /connect/token
GET /auth/user           -> Gateway -> AuthServer /account/profile  
GET /data/users          -> Gateway -> Orders /data
POST /account/register   -> Gateway -> AuthServer /account/register
```
  </routing_strategy>
</bff_pattern>

## Coding Standards

<coding_standards>
  <primary_constructors dotnet_version="8">
    <rule>ALWAYS use primary constructors for dependency injection</rule>

```csharp
// ✅ Preferred - Primary constructor
public class MyService(ILogger<MyService> logger, IConfiguration config)
{
    public void DoWork() => logger.LogInformation("Config: {Value}", config["Key"]);
}

// ❌ Avoid - Traditional constructor with boilerplate
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly IConfiguration _config;
    
    public MyService(ILogger<MyService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }
}
```
  </primary_constructors>
</coding_standards>

## Authentication Flow Types

<authentication_flows>
  <flow type="password">
    <description>User authentication</description>
    <clients>web-client, mobile-app, gateway-bff</clients>
  </flow>
  <flow type="client_credentials">
    <description>Service-to-service authentication</description>
    <clients>Orders, gateway-bff</clients>
  </flow>
  <flow type="refresh_token">
    <description>Token renewal for long-lived sessions</description>
  </flow>
</authentication_flows>

## Service Communication Patterns

<service_communication>
  <internal_communication>
```csharp
// ✅ Service-to-service with client credentials
public class OrdersController(IHttpClientFactory httpClientFactory)
{
    public async Task<UserProfile> GetUserProfile(string userId)
    {
        var client = httpClientFactory.CreateClient("AuthServer");
        // Uses client credentials token for service-to-service auth
        var response = await client.GetAsync($"/account/profile/{userId}");
        return await response.Content.ReadFromJsonAsync<UserProfile>();
    }
}
```
  </internal_communication>

  <jwt_validation>
```csharp
// ✅ Unified JWT validation across services
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Auth:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
```
  </jwt_validation>

  <authorization_policies>
```csharp
// ✅ Configuration-driven authorization
public class AuthorizationService(IConfiguration config)
{
    public void RegisterPolicies(IServiceCollection services)
    {
        var authConfig = config.GetSection("Auth:AuthenticationProviders");
        foreach (var provider in authConfig.GetChildren())
        {
            var policies = provider.GetSection("AuthorizationPolicies");
            foreach (var policy in policies.GetChildren())
            {
                var scopes = policy.GetSection("Scopes").Get<string[]>();
                services.AddAuthorization(options =>
                {
                    options.AddPolicy(policy.Key, policyBuilder =>
                        policyBuilder.RequireClaim("scope", scopes));
                });
            }
        }
    }
}
```
  </authorization_policies>
</service_communication>

## Database and Entity Framework

<database_configuration>
  <entity_configuration>
```csharp
// ✅ Primary constructor for EF contexts
public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : IdentityDbContext<AppUser, AppRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
```
  </entity_configuration>
</database_configuration>

## OpenIddict Configuration

<openiddict_configuration>
  <scope_client_management>
```csharp
// ✅ Configuration-driven OpenIddict setup
public class ScopeConfigurationService(IConfiguration config, IOpenIddictScopeManager scopeManager)
{
    public async Task ConfigureScopesAsync()
    {
        var scopeConfig = config.GetSection("OpenIddict:Scopes");
        foreach (var scope in scopeConfig.GetChildren())
        {
            await CreateScopeIfNotExistsAsync(scope.Key, scope["DisplayName"], scope["Description"]);
        }
    }
}
```
  </scope_client_management>
</openiddict_configuration>

## Key Configuration Files

<configuration_files>
  <file path="AuthServer/appsettings.Scopes.json">Client and scope definitions</file>
  <file path="Gateway/appsettings.json">YARP routing and authentication configuration</file>
  <file path="Orders/appsettings.json">Authorization policies and authentication providers</file>
  <note>All services support environment-specific configurations (Development, Docker, Production)</note>
</configuration_files>

## Database Seeding

<database_seeding>
  <auto_created_items>
    <item type="user">Default admin user: admin@example.com / Admin123!</item>
    <item type="entities">OpenIddict entities (clients, scopes, applications)</item>
    <item type="schema">Database schema via Entity Framework migrations</item>
  </auto_created_items>
</database_seeding>

## Service Endpoints

<service_endpoints>
  <service name="AuthServer" port="5000">
    <endpoint path="/connect/token">OAuth2 token endpoint</endpoint>
    <endpoint path="/account/register">User registration</endpoint>
    <endpoint path="/account/login">User authentication</endpoint>
    <endpoint path="/account/profile">User profile management</endpoint>
    <endpoint path="/scope/*">Client and scope management</endpoint>
  </service>

  <service name="Gateway" port="5002" type="BFF">
    <endpoint path="/auth/login">BFF authentication</endpoint>
    <endpoint path="/auth/refresh">Token refresh</endpoint>
    <endpoint path="/auth/user">Current user information</endpoint>
    <endpoint path="/auth/logout">BFF logout</endpoint>
    <endpoint path="/account/*">Proxied AuthServer endpoints</endpoint>
    <endpoint path="/data/*">Proxied Orders endpoints</endpoint>
  </service>

  <service name="Orders" port="5003">
    <endpoint path="/data">CRUD operations with scope-based authorization</endpoint>
    <endpoint path="/data/user">User-specific data endpoints</endpoint>
    <endpoint path="/data/info">Token information endpoint</endpoint>
  </service>
</service_endpoints>

## Clean Architecture Implementation (MANDATORY)

### Three-Layer Model Separation (MANDATORY)
**EVERY backend service MUST implement strict three-layer model separation**:

#### **API Layer Models** (Controllers + Request/Response Models)
- **ONLY** handle HTTP concerns (validation, serialization, status codes)
- Controllers **MUST** use only Request/Response models - **NO domain models**
- Controllers **MUST** perform API ↔ Domain mapping using static mapper classes
- Located in `Models/Requests/` and `Models/Responses/`
- Example: `CreateOrderRequest.cs`, `OrderResponse.cs`

#### **Domain Layer Models** (Services + Commands/Queries/Results/ViewModels)
- **ONLY** handle business logic and domain rules
- Services **MUST** accept Commands/Queries as inputs - **NO Request models**
- Services **MUST** return Results containing ViewModels - **NO Response models**
- Located in `Models/Commands/`, `Models/Queries/`, `Models/Results/`, `Models/ViewModels/`
- Example: `CreateOrderCommand.cs`, `OrderQuery.cs`, `OrderResult.cs`, `OrderViewModel.cs`

#### **Data Layer Models** (Repositories + Entity Models)
- **ONLY** handle data persistence with EF Core
- Repositories **MUST** accept and return Domain ViewModels - **NO Entity models exposed**
- Repositories **MUST** handle Entity ↔ Domain mapping internally
- Located in `Data/Entities/` and `Data/Entities/Mappers/`
- Example: `OrderEntity.cs`, `OrderEntityMappers.cs`

### Connector Pattern (MANDATORY for External Service Calls)
**All HTTP calls to external services MUST use Connectors**:

#### **Connectors** (External service communication)
```csharp
// Gateway/Connectors/AuthServerConnector.cs
public class AuthServerConnector(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AuthServerConnector> logger) : IAuthServerConnector
{
    private readonly string _authServerBaseUrl = configuration["Services:AuthServer:BaseUrl"] ?? "http://authserver:8080";

    public async Task<HttpResponseMessage> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Connector: Making HTTP call to AuthServer login endpoint");
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{_authServerBaseUrl}/Account/login", content);
        
        logger.LogInformation("Connector: AuthServer login response status: {StatusCode}", response.StatusCode);
        return response;
    }
}
```

#### **Services** (Use Connectors for external calls)
```csharp
// Gateway/Services/AuthProxyService.cs  
public class AuthProxyService(
    IAuthServerConnector authServerConnector,
    ILogger<AuthProxyService> logger) : IAuthProxyService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Service: Processing login request for {Email}", request.Email);
        
        // Use Connector for external HTTP call - NEVER make HTTP calls directly in Services
        var response = await authServerConnector.LoginAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            logger.LogInformation("Service: Login successful for {Email}", request.Email);
            return authResponse ?? new AuthResponse { Success = false, Message = "Invalid response" };
        }
        
        logger.LogWarning("Service: Login failed for {Email}", request.Email);
        return new AuthResponse 
        { 
            Success = false, 
            Message = "Authentication failed" 
        };
    }
}
```

### Service Layer Architecture (MANDATORY)
Each backend service **MUST** implement the following clean architecture layers:

#### **Controllers** (API Layer - HTTP Concerns ONLY)
**MANDATORY RULES:**
- Controllers **MUST** use only Request/Response models
- Controllers **MUST** perform API ↔ Domain mapping using mappers
- Controllers **MUST NOT** contain business logic
- Controllers **MUST NOT** access repositories directly
- Controllers **MUST NOT** use Entity or Domain models directly

```csharp
[ApiController]
[Route("[controller]")]
public class OrdersController(
    IOrdersService ordersService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = GetUserId();
        var correlationId = GetCorrelationId();
        
        // 🔄 API → Domain mapping (MANDATORY)
        var command = request.ToDomain(userId, correlationId);
        
        // Business logic handled by service (MANDATORY)
        var result = await ordersService.CreateOrderAsync(command);
        
        // 🔄 Domain → API mapping (MANDATORY)
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetOrderById), 
                new { id = result.Value.Order.Id }, 
                result.ToPresentation()) 
            : BadRequest(result.ToErrorResponse());
    }
}
```

#### **Services** (Domain Layer - Business Logic ONLY)
**MANDATORY RULES:**
- Services **MUST** accept Commands/Queries as inputs
- Services **MUST** return Results containing ViewModels
- Services **MUST NOT** use Request/Response models
- Services **MUST NOT** access Entity models directly
- Services **MUST** perform Domain ↔ Entity mapping when calling repositories

```csharp
public class OrdersService(
    IOrdersRepository ordersRepository,
    IInventoryService inventoryService) : IOrdersService
{
    public async Task<Result<OrderResult>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Pure business logic - validate business rules
        var inventoryCheck = await inventoryService.ValidateAvailabilityAsync(command.Items);
        if (!inventoryCheck.IsSuccess)
            return Result<OrderResult>.Failure("INSUFFICIENT_INVENTORY", "Items unavailable");
        
        // Create domain model
        var orderViewModel = new OrderViewModel
        {
            UserId = command.UserId,
            Items = command.Items,
            TotalAmount = command.Items.Sum(i => i.UnitPrice * i.Quantity),
            Status = OrderStatus.Pending,
            CorrelationId = command.CorrelationId
        };
        
        // Repository call with domain model (MANDATORY)
        var savedOrder = await ordersRepository.CreateAsync(orderViewModel);
        
        return Result<OrderResult>.Success(new OrderResult
        {
            Order = savedOrder,
            CorrelationId = command.CorrelationId
        });
    }
}
```

#### **Repositories** (Data Layer - Entity Mapping ONLY)
**MANDATORY RULES:**
- Repositories **MUST** accept and return Domain ViewModels
- Repositories **MUST NOT** expose Entity models outside the repository
- Repositories **MUST** handle Entity ↔ Domain mapping internally
- Repositories **MUST NOT** contain business logic

```csharp
public class OrdersRepository(OrdersDbContext context) : IOrdersRepository
{
    public async Task<OrderViewModel> CreateAsync(OrderViewModel orderViewModel)
    {
        // 🔄 Domain → Entity mapping (MANDATORY)
        var entity = orderViewModel.ToEntity();
        
        context.Orders.Add(entity);
        await context.SaveChangesAsync();
        
        // 🔄 Entity → Domain mapping (MANDATORY)
        return entity.ToDomain();
    }
    
    public async Task<OrderViewModel?> GetByIdAsync(int id)
    {
        var entity = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        // 🔄 Entity → Domain mapping (MANDATORY)
        return entity?.ToDomain();
    }
}
```

### Mapping Infrastructure (MANDATORY)
**ALL transformations between layers MUST use static mapper classes**:

#### **API ↔ Domain Mappers** (Controllers MUST Use)
**Location**: `Models/Mappers/`
**Purpose**: Transform Request/Response ↔ Commands/Queries/Results

```csharp
// Models/Mappers/OrderMappers.cs
public static class OrderMappers
{
    // Request → Command (API to Domain)
    public static CreateOrderCommand ToDomain(this CreateOrderRequest request, 
        string userId, string correlationId) => new()
    {
        UserId = userId,
        Items = request.Items.Select(i => new OrderItemCommand
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        ShippingAddress = request.ShippingAddress,
        CorrelationId = correlationId
    };

    // Result → Response (Domain to API)  
    public static OrderResponse ToPresentation(this OrderResult result) => new()
    {
        Id = result.Order.Id,
        OrderNumber = result.Order.OrderNumber,
        Status = result.Order.Status.ToString(),
        TotalAmount = result.Order.TotalAmount,
        Items = result.Order.Items.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        CreatedAt = result.Order.CreatedAt
    };
    
    // Result → Error Response (Domain to API)
    public static ErrorResponse ToErrorResponse(this Result<OrderResult> result) => new()
    {
        Error = result.Error?.Code ?? "UNKNOWN_ERROR",
        Message = result.Error?.Message ?? "An error occurred",
        Details = result.Error?.Details
    };
}
```

#### **Entity ↔ Domain Mappers** (Repositories MUST Use)
**Location**: `Data/Entities/Mappers/`
**Purpose**: Transform Entity ↔ ViewModels (Domain objects)

```csharp
// Data/Entities/Mappers/OrderEntityMappers.cs
public static class OrderEntityMappers
{
    // ViewModel → Entity (Domain to Data)
    public static OrderEntity ToEntity(this OrderViewModel viewModel) => new()
    {
        Id = viewModel.Id,
        UserId = viewModel.UserId,
        OrderNumber = viewModel.OrderNumber,
        Status = viewModel.Status,
        TotalAmount = viewModel.TotalAmount,
        ShippingAddress = viewModel.ShippingAddress,
        Items = viewModel.Items.Select(i => new OrderItemEntity
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        CreatedAt = viewModel.CreatedAt,
        UpdatedAt = viewModel.UpdatedAt
    };

    // Entity → ViewModel (Data to Domain)
    public static OrderViewModel ToDomain(this OrderEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        OrderNumber = entity.OrderNumber,
        Status = entity.Status,
        TotalAmount = entity.TotalAmount,
        ShippingAddress = entity.ShippingAddress,
        Items = entity.Items.Select(i => new OrderItemViewModel
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
```

### Data Flow Architecture (MANDATORY)

#### Complete Request-Response Flow
The system **MUST** implement strict three-layer model separation with clear data transformations:

```
🌐 HTTP Request → Request Model → Command/Query → ViewModel → Entity → Database
     ↓               ↓              ↓            ↓          ↓
🌐 HTTP Response ← Response Model ← Result ← ViewModel ← Entity ← Database

📊 Detailed Flow:
1. Controller receives Request Model (API contract)
2. Controller maps Request → Command/Query (API → Domain)
3. Service processes Command/Query with business logic  
4. Service calls Repository with ViewModel (Domain model)
5. Repository maps ViewModel → Entity (Domain → Data)
6. Repository persists Entity to Database
7. Repository maps Entity → ViewModel (Data → Domain)
8. Service returns Result containing ViewModel
9. Controller maps Result → Response Model (Domain → API)
10. Controller returns HTTP Response
```

#### Model Flow Diagram
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API LAYER     │    │  DOMAIN LAYER   │    │   DATA LAYER    │
│                 │    │                 │    │                 │
│ • Controllers   │◄──►│ • Services      │◄──►│ • Repositories  │
│ • Requests      │    │ • Commands      │    │ • Entities      │
│ • Responses     │    │ • Queries       │    │ • DbContext     │
│                 │    │ • Results       │    │                 │
│                 │    │ • ViewModels    │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
       │                        │                        │
       │                        │                        │
    🔄 API ↔ Domain         🔷 Pure Business         🔄 Domain ↔ Entity
    Mapping Required         Logic Processing        Mapping Required
```

### Repository Pattern (MANDATORY)
**ALL data access MUST follow the repository pattern with strict domain model contracts**:

#### **Repository Interface Requirements**
```csharp
// Repositories/Interfaces/IOrdersRepository.cs
public interface IOrdersRepository
{
    // 🔷 MUST accept and return Domain ViewModels
    Task<OrderViewModel> CreateAsync(OrderViewModel order);
    Task<OrderViewModel?> GetByIdAsync(int id, string userId);
    Task<List<OrderViewModel>> GetByUserIdAsync(string userId, OrderQuery query);
    Task<OrderViewModel> UpdateAsync(OrderViewModel order);
    Task DeleteAsync(int id, string userId);
    
    // 🔷 Query methods return Domain ViewModels
    Task<List<OrderViewModel>> GetOrdersByStatusAsync(OrderStatus status);
    Task<decimal> GetTotalOrderValueAsync(string userId);
}
```

#### **Repository Implementation Requirements**
```csharp
// Repositories/OrdersRepository.cs
public class OrdersRepository(OrdersDbContext context) : IOrdersRepository
{
    public async Task<OrderViewModel> CreateAsync(OrderViewModel orderViewModel)
    {
        // 🔄 Domain → Entity mapping (MANDATORY)
        var entity = orderViewModel.ToEntity();
        
        context.Orders.Add(entity);
        await context.SaveChangesAsync();
        
        // 🔄 Entity → Domain mapping (MANDATORY)
        return entity.ToDomain();
    }
    
    public async Task<OrderViewModel?> GetByIdAsync(int id, string userId)
    {
        var entity = await context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        // 🔄 Entity → Domain mapping (MANDATORY)
        return entity?.ToDomain();
    }
    
    public async Task<List<OrderViewModel>> GetByUserIdAsync(string userId, OrderQuery query)
    {
        var queryable = context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .AsQueryable();
            
        // Apply query filters
        if (query.Status.HasValue)
            queryable = queryable.Where(o => o.Status == query.Status);
            
        if (query.DateFrom.HasValue)
            queryable = queryable.Where(o => o.CreatedAt >= query.DateFrom);
            
        var entities = await queryable
            .OrderByDescending(o => o.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync();
            
        // 🔄 Entity → Domain mapping (MANDATORY)
        return entities.Select(e => e.ToDomain()).ToList();
    }
}
```

### Complete Folder Structure
All backend services follow this comprehensive clean architecture structure:

```
src/backend/{ServiceName}/
├── Controllers/                    # API endpoints - HTTP concerns only
├── Services/                       # Business logic & domain services
│   └── Interfaces/                 # Service contracts
├── Repositories/                   # Data access layer
│   └── Interfaces/                 # Repository contracts  
├── Connectors/                     # External service calls (Gateway only)
│   └── Interfaces/                 # Connector contracts
├── Models/                         # Three-layer model separation
│   ├── Requests/                   # API input contracts
│   ├── Responses/                  # API output contracts
│   ├── Commands/                   # Domain service inputs (CQRS style)
│   ├── Queries/                    # Domain service queries (CQRS style)
│   ├── Results/                    # Domain service outputs
│   ├── ViewModels/                 # Domain data models (business objects)
│   ├── Enums/                      # Domain enumerations
│   └── Mappers/                    # API ↔ Domain mapping extensions
├── Data/                          # EF Core context & entity management
│   ├── Entities/                  # Database entities (EF Core)
│   │   └── Mappers/               # Entity ↔ Domain mapping extensions
│   ├── Configurations/            # Entity configurations (IEntityTypeConfiguration)
│   ├── Migrations/                # EF Core migrations
│   └── Seeders/                   # Database seeding
├── Configuration/                  # Strongly-typed config classes
├── Middleware/                     # Request pipeline middleware
├── Filters/                        # Action filters and attributes
├── Extensions/                     # Extension methods & helpers
├── Infrastructure/                 # Cross-cutting concerns
│   ├── Logging/                   # Logging services and extensions
│   ├── Security/                  # Security utilities
│   ├── Validation/                # Validation services
│   └── Caching/                   # Caching abstractions
├── Constants/                     # Application constants
├── Exceptions/                    # Custom exception classes
├── appsettings*.json              # Configuration files
└── Program.cs                     # Application entry point & DI registration
```

### Clean Architecture Benefits

**✅ Complete Separation of Concerns**
- **API layer**: HTTP handling, validation, serialization, status codes
- **Domain layer**: Business logic, domain rules, workflow orchestration
- **Data layer**: Entity mapping, database queries, data persistence
- **No cross-layer contamination**: Each layer only knows about its immediate dependencies

**✅ Superior Testability** 
- **Unit test each layer independently**: Mock interfaces at layer boundaries
- **Pure business logic testing**: Services contain no HTTP or database code
- **Repository testing**: Easy to test with in-memory databases
- **API testing**: Controllers are thin and focus only on HTTP concerns
- **Mock-friendly interfaces**: All dependencies are abstracted

**✅ Enhanced Maintainability**
- **Single Responsibility**: Each class has one clear purpose
- **Change isolation**: Modifications in one layer don't affect others
- **Clear boundaries**: Explicit contracts between layers
- **Consistent patterns**: Same structure across all services
- **Self-documenting**: Code structure reflects business intentions

**✅ Production-Ready Scalability**
- **Independent scaling**: Services can be deployed and scaled separately
- **Clear interfaces**: Well-defined contracts enable microservice evolution
- **Database flexibility**: Repository pattern allows easy database changes
- **Caching strategies**: Domain models are perfect for caching layers
- **Load balancing**: Stateless services scale horizontally

**✅ Developer Productivity**
- **Predictable structure**: New developers know exactly where code belongs
- **Copy-paste templates**: Adding new endpoints follows identical patterns
- **Clear debugging**: Issues are isolated to specific architectural layers
- **Reduced cognitive load**: Developers focus on one concern at a time
- **Faster feature development**: Established patterns accelerate implementation

**✅ Business Value**
- **Faster time-to-market**: Consistent patterns reduce development time
- **Lower maintenance costs**: Clean architecture reduces technical debt
- **Higher quality**: Separation of concerns reduces bugs
- **Better testing**: Comprehensive test coverage through layer isolation
- **Future-proofing**: Architecture can evolve without major rewrites