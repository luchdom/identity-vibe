# CLAUDE.md

This file provides essential guidance to Claude Code when working with this Identity System codebase.

## Project Overview

**.NET 8 OAuth2/OpenID Connect authentication system** with React frontend:

**Backend Services:**
- **AuthServer** (5000): OpenIddict OAuth2/OIDC server with ASP.NET Core Identity
- **Gateway** (5002): YARP-based BFF proxy with authentication handling  
- **Orders** (5003): Orders API with JWT validation and authorization

**Frontend:**
- **React App** (5173): TypeScript + Tailwind CSS + shadcn/ui components

## Quick Start Commands

### Recommended Development Setup (AI-Friendly)
```bash
# Backend services in Docker (consistent environment)
docker-compose up --build postgres authserver gateway orders

# Frontend locally (hot reload, easy file access)
cd src/client && pnpm dev
```

### Database Management
```bash
# Reset database completely
docker-compose down -v && docker-compose up --build postgres authserver

# Connect to database
docker-compose exec postgres psql -U postgres -d AuthServer
```

### Frontend Development
```bash
cd src/client

# Install dependencies
pnpm install

# Add shadcn/ui component
npx shadcn@latest add button

# Run tests
pnpm test                    # Unit tests
npx playwright test         # E2E tests
npx playwright test --ui    # E2E tests with debugging UI
```

## Clean Architecture Implementation (MANDATORY)

### Architecture Overview
This project **STRICTLY ENFORCES clean architecture principles** with mandatory separation between:
- **Presentation Layer**: Controllers, API models, HTTP concerns
- **Domain Layer**: Services, business logic, domain models  
- **Data Layer**: Repositories, entities, database access

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
- Located in `Entities/` and `Entities/Mappers/`
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
**Location**: `Entities/Mappers/`
**Purpose**: Transform Entity ↔ ViewModels (Domain objects)

```csharp
// Entities/Mappers/OrderEntityMappers.cs
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

#### **Commands ↔ ViewModels Mappers** (Services MAY Use)
**Purpose**: Transform between different domain model types when needed

```csharp
// Models/Mappers/OrderDomainMappers.cs
public static class OrderDomainMappers
{
    // Command → ViewModel (for creation)
    public static OrderViewModel ToViewModel(this CreateOrderCommand command) => new()
    {
        UserId = command.UserId,
        Items = command.Items.Select(i => new OrderItemViewModel
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        ShippingAddress = command.ShippingAddress,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
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
├── Entities/                       # Database entities (EF Core)
│   └── Mappers/                    # Entity ↔ Domain mapping extensions
├── Data/                          # EF Core context & migrations
│   ├── Configurations/            # Entity configurations
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

#### Layer Responsibilities

**1. API Layer (Controllers + Request/Response Models)**
- Handle HTTP concerns (validation, serialization, status codes)
- Transform API requests to domain commands using mappers
- Transform domain results to API responses using mappers  
- Manage correlation IDs and HTTP context
- **NO business logic or manual logging**

**2. Domain Layer (Services + Commands/Queries/Results/ViewModels)**
- Implement all business logic and domain rules
- Accept domain commands and queries
- Return domain results with business outcomes
- Handle business validation and calculations
- Orchestrate calls to repositories and connectors
- **Pure business logic - no HTTP or database concerns**

**3. Data Layer (Repositories + Entities)**
- Handle data persistence and retrieval
- Accept and return Domain ViewModels (never expose Entities)
- Transform domain objects to/from entities using internal mappers
- Implement database transactions and concurrency
- Optimize database queries and operations
- **Only data access - no business logic**
- **Encapsulate all Entity concerns within repositories**

#### Complete Data Flow Examples (MANDATORY PATTERN)

**API Request to Domain Command:**
```csharp
// API Layer - OrdersController.cs
[HttpPost]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    var userId = GetUserId();
    var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    
    // 🔄 API → Domain transformation
    var command = request.ToDomain(userId, correlationId);
    
    // Business logic handled by service
    var result = await ordersService.CreateOrderAsync(command);
    
    // 🔄 Domain → API transformation  
    var response = result.ToPresentation();
    return CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Order.Id }, response);
}
```

**Domain Service Processing:**
```csharp
// Domain Layer - OrdersService.cs
public async Task<Result<OrderResult>> CreateOrderAsync(CreateOrderCommand command)
{
    // Pure business logic - no HTTP concerns
    var order = new Order
    {
        UserId = command.UserId,
        OrderNumber = GenerateOrderNumber(),
        Status = OrderStatus.Draft,
        TotalAmount = command.Items.Sum(i => i.UnitPrice * i.Quantity)
    };

    // Data persistence via repository
    var savedOrder = await orderRepository.CreateAsync(order);
    
    // 🔄 Entity → Domain transformation
    return Result<OrderResult>.Success(new OrderResult
    {
        Order = savedOrder.ToDomain(),
        CorrelationId = command.CorrelationId
    });
}
```

**Data Layer Persistence:**
```csharp
// Data Layer - OrderRepository.cs
public async Task<Order> CreateAsync(Order domainOrder)
{
    // 🔄 Domain → Entity transformation
    var entity = domainOrder.ToEntity();
    
    context.Orders.Add(entity);
    await context.SaveChangesAsync();
    
    // 🔄 Entity → Domain transformation
    return entity.ToDomain();
}
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

#### **Repository Pattern Benefits**
- **Domain-focused**: Repositories work with business objects (ViewModels)
- **Encapsulation**: Entity mapping logic is hidden inside repositories
- **Testability**: Easy to mock with domain models
- **Consistency**: All repositories follow the same pattern
- **Clean Services**: Services don't need to know about entities

### Dependency Injection Registration (MANDATORY)
Each service **MUST** register all clean architecture components:

```csharp
// Program.cs - Clean Architecture Registration

// 🔷 Domain Services (Business Logic Layer)
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// 🔷 Repository Pattern (Data Access Layer)
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 🔷 Connectors (Gateway only - for external service calls)
builder.Services.AddScoped<IAuthServerConnector, AuthServerConnector>();
// Connectors for external service calls - removed Orders, now uses Orders service

// 🔷 Infrastructure Services
builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();
builder.Services.AddScoped<IDateTimeService, DateTimeService>();

// 🔷 Database Context
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

## Architecture Essentials

### BFF Pattern (MANDATORY)
- **All external clients MUST access through Gateway BFF (port 5002)**
- **Direct AuthServer/Orders access is PROHIBITED**
- Gateway routes: `/auth/*` → AuthServer, `/orders/*` → Orders

### Service URLs
```bash
# External (for frontend)
Frontend: http://localhost:5173
Gateway:  http://localhost:5002  # Main API endpoint

# Internal (Docker containers)
AuthServer: http://authserver:8080
Orders:   http://orders:8080
```

### Default Login Credentials  

#### Test Users Available
The system automatically seeds two test users with different permission levels:

##### Admin User (Full Access)
- **Email**: `admin@example.com`
- **Password**: `Admin123!`
- **Role**: Admin
- **Permissions**: Full admin access with all scopes including `admin.manage`, `admin.users`, `admin.roles`

##### Regular User (Standard Access)
- **Email**: `user@example.com`
- **Password**: `User123!`
- **Role**: User  
- **Permissions**: Standard user access with `orders.read`, `orders.write`, `profile.read`, `profile.write` scopes

#### Quick Test Login Dropdown
The React frontend includes a convenient test user dropdown in development mode:
- Located on the login page for easy access
- Auto-fills credentials when a test user is selected
- Only visible in development environment (`VITE_APP_ENV=development`)
- Provides quick switching between admin and regular user accounts

## Key Coding Standards

### Controller Validation (MANDATORY)
**ALWAYS use `[ApiController]` attribute and NEVER do manual ModelState validation**:
```csharp
// ✅ Good - Automatic validation with [ApiController]
[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // No manual ModelState.IsValid check needed!
        // ASP.NET Core automatically returns 400 for invalid models
        var userId = GetUserId();
        // ... business logic only
    }
}

// ❌ Bad - Manual validation (redundant with [ApiController])
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    if (!ModelState.IsValid)  // ❌ Don't do this
    {
        return BadRequest(ModelState);
    }
    // ... business logic
}
```

### Logging Architecture (MANDATORY)
**NEVER use custom manual logging - ALWAYS rely on middleware and filters**:
```csharp
// ✅ Good - No custom logging, middleware handles it
public class OrdersService(OrdersDbContext context) : IOrdersService
{
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, string userId)
    {
        // Pure business logic - no logging statements
        var order = new Order { UserId = userId, /* ... */ };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return Result<Order>.Success(order);
    }
}

// ✅ Good - Clean controller without logging
[ApiController]
public class OrdersController(IOrdersService ordersService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // No _logger.LogInformation calls - middleware logs HTTP requests
        var result = await ordersService.CreateOrderAsync(request, GetUserId());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

// ❌ Bad - Manual logging (conflicts with middleware)
public class OrdersService(OrdersDbContext context, ILogger<OrdersService> logger) : IOrdersService
{
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, string userId)
    {
        logger.LogInformation("Creating order for user {UserId}", userId); // ❌ Don't do this
        var order = new Order { UserId = userId };
        logger.LogInformation("Order created: {OrderId}", order.Id); // ❌ Don't do this
        return Result<Order>.Success(order);
    }
}
```

**Why avoid custom logging:**
- **Middleware handles HTTP logging** (requests, responses, correlation IDs)
- **Structured logging** is configured globally via Serilog
- **Cleaner code** focused on business logic
- **Consistent logging format** across all services
- **Better performance** (less logging overhead in business logic)

### File Organization (MANDATORY)
**ALWAYS use one class per file** - no exceptions:
```csharp
// ✅ Good - Each class in separate file
Result.cs           // public class Result<T>
ResultVoid.cs       // public class Result  
Error.cs            // public record Error
LoginRequest.cs     // public record LoginRequest
AuthResponse.cs     // public record AuthResponse

// ❌ Bad - Multiple classes in one file
Result.cs with Result<T>, Result, Error classes all together
```

### Model Organization (MANDATORY)
**ALL models MUST follow strict folder organization - NO EXCEPTIONS**:

#### **MANDATORY Folder Structure for Models**
```csharp
// ✅ CORRECT - Each model type in proper location
Models/Requests/CreateOrderRequest.cs     // API input contracts
Models/Requests/UpdateOrderRequest.cs     // API input contracts
Models/Responses/OrderResponse.cs         // API output contracts
Models/Responses/ErrorResponse.cs         // API error contracts

Models/Commands/CreateOrderCommand.cs     // Domain service inputs
Models/Commands/UpdateOrderCommand.cs     // Domain service inputs
Models/Queries/OrderQuery.cs              // Domain service queries
Models/Queries/OrderListQuery.cs          // Domain service queries

Models/Results/OrderResult.cs             // Domain service outputs
Models/Results/OrderListResult.cs         // Domain service outputs
Models/ViewModels/OrderViewModel.cs       // Domain business objects
Models/ViewModels/OrderItemViewModel.cs   // Domain business objects

Models/Enums/OrderStatus.cs               // Domain enumerations
Models/Mappers/OrderMappers.cs            // API ↔ Domain mapping

Entities/OrderEntity.cs                   // Database entities
Entities/OrderItemEntity.cs               // Database entities
Entities/Mappers/OrderEntityMappers.cs    // Entity ↔ Domain mapping

// ❌ WRONG - Models defined inline (NEVER do this)
public class OrdersController : ControllerBase
{
    // Controller actions...
    
    // ❌ NEVER define models inside controllers
    public class CreateOrderRequest { /* ... */ }
}

// ❌ WRONG - Models at bottom of files
public class OrdersService : IOrdersService
{
    // Service logic...
}

// ❌ NEVER append models at bottom of service files
public class UpdateOrderStatusCommand  // ❌ WRONG LOCATION
{
    public OrderStatus Status { get; set; }
}
```

#### **Model Type Definitions (MANDATORY)**

**🔷 Commands** (`Models/Commands/`)
- **Purpose**: Input contracts for domain services
- **Usage**: Services accept Commands as parameters
- **Naming**: `{Action}{Entity}Command.cs` (e.g., `CreateOrderCommand.cs`)

```csharp
// Models/Commands/CreateOrderCommand.cs
public record CreateOrderCommand
{
    public required string UserId { get; init; }
    public required List<OrderItemCommand> Items { get; init; }
    public required string ShippingAddress { get; init; }
    public required string CorrelationId { get; init; }
}
```

**🔷 Queries** (`Models/Queries/`)
- **Purpose**: Query contracts for domain services
- **Usage**: Services accept Queries for read operations
- **Naming**: `{Entity}Query.cs` or `{Entity}ListQuery.cs`

```csharp
// Models/Queries/OrderQuery.cs
public record OrderQuery
{
    public required int Id { get; init; }
    public required string UserId { get; init; }
    public required string CorrelationId { get; init; }
}
```

**🔷 Results** (`Models/Results/`)
- **Purpose**: Output contracts from domain services
- **Usage**: Services return Results containing ViewModels
- **Naming**: `{Entity}Result.cs`

```csharp
// Models/Results/OrderResult.cs
public record OrderResult
{
    public required OrderViewModel Order { get; init; }
    public required string CorrelationId { get; init; }
}
```

**🔷 ViewModels** (`Models/ViewModels/`)
- **Purpose**: Domain business objects (rich models)
- **Usage**: Represent business entities in domain layer
- **Naming**: `{Entity}ViewModel.cs`

```csharp
// Models/ViewModels/OrderViewModel.cs
public record OrderViewModel
{
    public int Id { get; init; }
    public required string UserId { get; init; }
    public required string OrderNumber { get; init; }
    public required OrderStatus Status { get; init; }
    public required decimal TotalAmount { get; init; }
    public required List<OrderItemViewModel> Items { get; init; }
    public required string ShippingAddress { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

#### **CRITICAL RULES (NO EXCEPTIONS)**
- **Request/Response models** → `Models/Requests/` and `Models/Responses/`
- **Domain inputs** → `Models/Commands/` and `Models/Queries/`
- **Domain outputs** → `Models/Results/` and `Models/ViewModels/`
- **Database entities** → `Entities/`
- **Mapping logic** → `Models/Mappers/` and `Entities/Mappers/`
- **NEVER define models inline** in controllers, services, or repositories
- **NEVER append models** at the bottom of class files
- **Each model gets its own file** with proper namespace and naming
- **One class per file** - no multiple classes in single file

### Backend: Primary Constructors (.NET 8)
```csharp
// ✅ ALWAYS use primary constructors for DI
public class MyService(ILogger<MyService> logger, IConfiguration config)
{
    public void DoWork() => logger.LogInformation("Value: {Value}", config["Key"]);
}
```

### Frontend: shadcn/ui + TypeScript
```typescript
// ✅ Component pattern
import { Button } from '@/components/ui/button'

export function MyComponent() {
  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Dashboard</CardTitle>
      </CardHeader>
      <CardContent>
        <Button variant="default">Action</Button>
      </CardContent>
    </Card>
  )
}
```

### Forms: React Hook Form + Zod
```typescript
// ✅ Type-safe form validation
const schema = z.object({
  email: z.string().email(),
  password: z.string().min(8)
})

type FormValues = z.infer<typeof schema>

const form = useForm<FormValues>({
  resolver: zodResolver(schema)
})
```

## Project Structure

```
src/
├── backend/
│   ├── AuthServer/          # OAuth2 server
│   ├── Gateway/             # BFF proxy
│   └── Orders/              # Orders API
└── client/                  # React frontend
    ├── src/
    │   ├── components/ui/   # shadcn/ui components
    │   ├── features/        # Feature components
    │   ├── contexts/        # React contexts
    │   ├── lib/             # Utilities
    │   └── pages/           # Route components
    └── tests/               # Playwright tests

docs/                        # Detailed documentation
├── ARCHITECTURE.md          # Backend patterns & BFF rules
├── FRONTEND.md             # React patterns & shadcn/ui
├── DEPLOYMENT.md           # Docker & environments
├── DEVELOPMENT.md          # Setup & troubleshooting
├── LOGGING_IMPLEMENTATION.md # Serilog configuration & structured logging
└── SIGNOZ_APM_SETUP.md     # SigNoz APM integration & monitoring
```

## Common Development Tasks

### Testing Different User Permission Levels

#### Using the Frontend Dropdown (Recommended)
1. Navigate to the login page (`http://localhost:5173/login`)
2. Use the "Quick Test Login" dropdown to select:
   - **Admin User** - Full admin access with all permissions
   - **Regular User** - Standard user access with limited permissions
3. Credentials are automatically filled in when you select a user
4. Click "Sign In" to test the selected user's access level

#### Manual Testing
You can also manually enter credentials:
```bash
# Admin user
Email: admin@example.com
Password: Admin123!

# Regular user  
Email: user@example.com
Password: User123!
```

#### API Testing via curl
```bash
# Test admin user login
curl -X POST http://localhost:5002/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}'

# Test regular user login
curl -X POST http://localhost:5002/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"User123!"}'
```

### API Configuration
Frontend connects to Gateway BFF:
```typescript
// src/client/src/lib/api.ts
const BASE_URL = 'http://localhost:5002'; // Gateway BFF endpoint
```

### Authentication Flow
```typescript
// Protected route pattern
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()
  
  if (isLoading) return <LoadingSpinner />
  if (!isAuthenticated) return <Navigate to="/sign-in" replace />
  
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>
}
```

### Database Operations
```bash
# Apply EF migrations
cd src/backend/AuthServer && dotnet ef database update

# Create new migration  
dotnet ef migrations add MigrationName
```

## Key Configuration Files

- **AuthServer/appsettings.Scopes.json**: OAuth2 clients and scopes
- **Gateway/appsettings.json**: YARP routing configuration
- **Orders/appsettings.json**: Authorization policies
- **src/client/src/lib/api.ts**: Frontend API configuration

## Environment Variables

### Development (Docker)
```bash
# AuthServer
ConnectionStrings__DefaultConnection="Host=postgres;Database=AuthServer;..."
Jwt__Issuer="https://localhost:5000"

# Gateway  
Auth__Authority="http://authserver:8080"
Cors__AllowedOrigins__0="http://localhost:5173"

# Frontend
VITE_API_URL="http://localhost:5002"
```

## Testing

### E2E Testing with Playwright
```bash
# Run tests
npx playwright test

# Debug mode
npx playwright test --ui

# Test specific feature
npx playwright test tests/auth.spec.ts
```

### Component Testing
```typescript
// React Testing Library pattern
test('displays validation errors', async () => {
  render(<LoginForm />)
  fireEvent.click(screen.getByRole('button', { name: 'Sign In' }))
  await waitFor(() => {
    expect(screen.getByText('Invalid email')).toBeInTheDocument()
  })
})
```

## Troubleshooting Quick Fixes

### Authentication Issues
1. Clear localStorage: `localStorage.clear()`
2. Reset database: `docker-compose down -v`
3. Check CORS headers in Network tab

### Docker Issues
1. Port conflicts: `lsof -i :5002` then `kill -9 <PID>`
2. Clean rebuild: `docker-compose down --rmi all -v && docker-compose up --build`
3. Container logs: `docker-compose logs gateway -f`

### Frontend Issues  
1. Clear cache: `rm -rf node_modules .vite && pnpm install`
2. Restart TypeScript: Ctrl+Shift+P → "TypeScript: Restart TS Server"
3. Check API endpoint in Network tab

## Documentation References

For detailed information, see:
- **docs/ARCHITECTURE.md**: Backend patterns, BFF rules, service communication
- **docs/FRONTEND.md**: React patterns, shadcn/ui usage, testing strategies
- **docs/DEPLOYMENT.md**: Docker configurations, environments, monitoring  
- **docs/DEVELOPMENT.md**: Setup instructions, troubleshooting, debugging
- **docs/LOGGING_IMPLEMENTATION.md**: Serilog configuration, structured logging, correlation IDs
- **docs/SIGNOZ_APM_SETUP.md**: SigNoz APM integration, OpenTelemetry configuration, monitoring dashboards

## AI Debugging Recommendations

The current setup is optimized for AI assistance:
- **Backend in Docker**: Consistent, isolated environment
- **Frontend local**: Direct file access, hot reload, easy debugging
- **Port mapping**: Services accessible on localhost
- **Structured docs**: Specific guides for different development aspects

This configuration allows AI assistants to easily read/modify frontend code while ensuring backend consistency.