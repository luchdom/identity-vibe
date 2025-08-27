# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 authentication and authorization system implementing OAuth2/OpenID Connect using OpenIddict. It consists of backend services and a React frontend:

**Backend Services (src/backend/ folder):**
- **AuthServer**: OpenIddict-based authentication server with ASP.NET Core Identity
- **Gateway**: YARP-based BFF (Backend for Frontend) service with authentication handling
- **ServiceA**: Resource API with JWT validation and dynamic authorization policies

**Frontend Application (src/client/ folder):**
- **React App**: Modern React 18 application with TypeScript, Tailwind CSS, and authentication flow

## Common Development Commands

### Building and Running

```bash
# Build entire solution
dotnet build IdentitySolution.sln

# Run AuthServer (from src/backend/AuthServer directory)
cd src/backend/AuthServer
dotnet run

# Run Gateway (from src/backend/Gateway directory)
cd src/backend/Gateway
dotnet run

# Run ServiceA (from src/backend/ServiceA directory)  
cd src/backend/ServiceA
dotnet run

# Run React frontend (from src/client directory)
cd src/client
npm start
```

### Docker Development

```bash
# Development environment with hot reloading
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Production environment
docker-compose -f docker-compose.prod.yml up --build -d

# Basic Docker setup
docker-compose up --build

# For debugging with AI assistance (recommended)
# This runs all backend services in Docker while keeping the React frontend local for easier debugging
# Benefits: AI can easily read/modify frontend code, backend runs in isolated containers with consistent environment
docker-compose up --build postgres authserver gateway servicea

# Then run the React frontend separately for better debugging:
cd src/client && pnpm dev

# Alternative: Run only database in Docker, services locally (for full debugging control)
docker-compose up --build postgres
# Then manually start each service:
# cd src/backend/AuthServer && dotnet run
# cd src/backend/Gateway && dotnet run  
# cd src/backend/ServiceA && dotnet run
# cd src/client && pnpm dev
```

### React Frontend Development

```bash
# Install dependencies
cd src/client
pnpm install

# Start development server (runs on http://localhost:5173)
pnpm dev

# Build for production
pnpm build

# Run tests
pnpm test

# Add shadcn/ui components
npx shadcn@latest add [component-name]

# Run Playwright tests
npx playwright test

# Run Playwright tests in UI mode
npx playwright test --ui
```

### Database Management

```bash
# Connect to PostgreSQL container
docker-compose exec postgres psql -U postgres -d AuthServer

# Backup database
docker-compose exec postgres pg_dump -U postgres AuthServer > backup.sql

# Apply Entity Framework migrations (from AuthServer directory)
cd src/backend/AuthServer
dotnet ef database update
```

## Architecture and Key Components

### AuthServer Architecture
- **OpenIddict Server**: OAuth2/OpenID Connect server with JWT token issuance
- **ASP.NET Core Identity**: User management with password policies
- **Entity Framework Core**: Data access with PostgreSQL
- **Scope Configuration**: Dynamic client and scope management via `appsettings.Scopes.json`
- **Default Authentication Flows**: Password flow, Client Credentials flow, Refresh Token flow

### Gateway Architecture
- **YARP Reverse Proxy**: Routes requests to AuthServer and ServiceA
- **BFF Authentication Controller**: Handles login, refresh, and user info
- **JWT Bearer Authentication**: Validates tokens from AuthServer
- **CORS Configuration**: Supports frontend applications

### ServiceA Architecture
- **JWT Bearer Authentication**: Token validation from AuthServer
- **Dynamic Authorization Service**: Policy registration based on configuration
- **Multi-issuer Support**: Can validate tokens from multiple authentication providers
- **Scope-based Access Control**: Authorization policies defined in `appsettings.json`

### React Frontend Architecture
- **React 18**: Modern React with functional components and hooks
- **TypeScript**: Full type safety throughout the application
- **Tailwind CSS**: Utility-first CSS framework for responsive design
- **React Router**: Client-side routing with protected routes
- **Axios**: HTTP client with automatic token management via interceptors
- **Context API**: Global authentication state management
- **Authentication Flow**: Login, register, dashboard with automatic token refresh
- **shadcn/ui**: Modern component library built on Radix UI primitives
- **Playwright**: End-to-end testing framework for web applications
- **Context7**: AI-powered documentation and code examples integration

### Key Configuration Files
- `AuthServer/appsettings.Scopes.json`: Client and scope definitions
- `Gateway/appsettings.json`: YARP routing and authentication configuration
- `ServiceA/appsettings.json`: Authorization policies and authentication providers
- All services support environment-specific configurations (Development, Docker, Production)

### Authentication Flow Types
1. **Password Flow**: User authentication with username/password (clients: web-client, mobile-app, gateway-bff)
2. **Client Credentials Flow**: Service-to-service authentication (clients: ServiceA, gateway-bff)
3. **Refresh Token Flow**: Token renewal for long-lived sessions

### Database Seeding
AuthServer automatically creates:
- Default admin user: `admin@example.com` / `Admin123!`
- OpenIddict entities (clients, scopes, applications)
- Database schema via Entity Framework

### CORS Configuration
Both services use dynamic CORS configuration from `appsettings.json` with `Cors:AllowedOrigins` arrays.

## Default Service Endpoints

### AuthServer (localhost:5000)
- `/connect/token` - OAuth2 token endpoint
- `/account/register` - User registration
- `/account/login` - User authentication
- `/account/profile` - User profile management
- `/account/forgot-password` - Password reset initiation
- `/account/reset-password` - Password reset completion
- `/scope/*` - Client and scope management

### ServiceA (localhost:5001)
- `/data` - CRUD operations with scope-based authorization
- `/data/user` - User-specific data endpoints
- `/data/info` - Token information endpoint

### Gateway (localhost:5002)
- `/auth/login` - BFF authentication endpoint
- `/auth/refresh` - Token refresh endpoint
- `/auth/user` - Current user information
- `/auth/logout` - BFF logout endpoint
- `/account/*` - Proxied AuthServer account endpoints
- `/scope/*` - Proxied AuthServer scope endpoints
- `/connect/*` - Proxied AuthServer OAuth endpoints
- `/data/*` - Proxied ServiceA data endpoints

### React Frontend (localhost:5173)
- `/login` - User login page
- `/register` - User registration page
- `/dashboard` - Protected dashboard with user info and API data
- `/` - Redirects to dashboard (protected)
- `*` - All other routes redirect to dashboard

## Docker Port Mapping
- AuthServer: `5000` (HTTP)
- Gateway: `5002` (HTTP)
- ServiceA: `5003` (HTTP) in Docker
- PostgreSQL: `5432`

## Key Dependencies

### Backend Dependencies
- **OpenIddict 7.0.0**: OAuth2/OpenID Connect server implementation
- **ASP.NET Core Identity**: User management and authentication
- **YARP 2.1.0**: Reverse proxy for Gateway BFF service
- **Entity Framework Core**: Data access with PostgreSQL provider
- **JWT Bearer Authentication**: Token validation middleware

### Frontend Dependencies
- **React 18**: Modern React framework with concurrent features
- **TypeScript**: Static type checking for JavaScript
- **Vite**: Fast build tool and development server
- **Tailwind CSS**: Utility-first CSS framework
- **shadcn/ui**: Component library built on Radix UI and Tailwind CSS
- **React Hook Form**: Performant forms with easy validation
- **Zod**: TypeScript-first schema validation
- **Axios**: Promise-based HTTP client
- **React Router**: Declarative routing for React
- **Sonner**: Toast notification library
- **Lucide React**: Modern icon library
- **Playwright**: End-to-end testing framework

## Development Notes
- Uses development certificates for JWT signing (change for production)
- Database auto-creation on first run
- All secrets and passwords should be changed for production environments
- CORS configured for local development URLs

## Coding Standards

### Primary Constructors (.NET 8)
- **ALWAYS use primary constructors** for service classes, controllers, and other dependency injection targets
- Primary constructors provide cleaner, more concise syntax compared to traditional constructors
- Eliminate boilerplate field declarations and constructor parameter assignments
- Example:
  ```csharp
  // ✅ Preferred - Primary constructor
  public class MyService(ILogger<MyService> logger, IConfiguration config)
  {
      public void DoWork() => logger.LogInformation("Config value: {Value}", config["Key"]);
  }
  
  // ❌ Avoid - Traditional constructor
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
- Use traditional constructors only when additional constructor logic is required beyond parameter assignment

## Backend Architecture Patterns and Best Practices

### BFF (Backend for Frontend) Pattern
- **MANDATORY**: All external client endpoints MUST be exposed through the Gateway BFF service
- **Direct Access Prohibited**: External clients should NEVER directly access AuthServer or ServiceA endpoints
- **Routing Strategy**: Gateway BFF acts as the single entry point for all client requests
- **Benefits**: Centralized authentication, request transformation, and API composition

### Service Communication Patterns

#### Gateway BFF Routing
```csharp
// ✅ Correct - External clients access through Gateway BFF
POST /auth/login          -> Gateway -> AuthServer /connect/token
GET /auth/user           -> Gateway -> AuthServer /account/profile  
GET /data/users          -> Gateway -> ServiceA /data
POST /account/register   -> Gateway -> AuthServer /account/register
```

#### Internal Service Communication
```csharp
// ✅ Service-to-service communication (Client Credentials flow)
public class ServiceAController(IHttpClientFactory httpClientFactory, IConfiguration config)
{
    public async Task<UserProfile> GetUserProfile(string userId)
    {
        var client = httpClientFactory.CreateClient("AuthServer");
        // Use client credentials token for service-to-service auth
        var response = await client.GetAsync($"/account/profile/{userId}");
        return await response.Content.ReadFromJsonAsync<UserProfile>();
    }
}
```

### Authentication and Authorization Patterns

#### JWT Token Validation
```csharp
// ✅ Preferred - Unified JWT validation across services
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
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
    }
}
```

#### Dynamic Authorization Policies
```csharp
// ✅ Configuration-driven authorization policies
public class AuthorizationService(IConfiguration config, ILogger<AuthorizationService> logger)
{
    public void RegisterPolicies(IServiceCollection services)
    {
        var authConfig = config.GetSection("Auth:AuthenticationProviders");
        foreach (var provider in authConfig.GetChildren())
        {
            RegisterProviderPolicies(services, provider);
        }
    }
    
    private void RegisterProviderPolicies(IServiceCollection services, IConfigurationSection provider)
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
```

### Database and Entity Framework Patterns

#### Entity Configuration
```csharp
// ✅ Primary constructor for Entity Framework configurations
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, AppRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure entities using fluent API
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
```

#### Repository Pattern (Optional)
```csharp
// ✅ Generic repository with primary constructor
public class Repository<T>(AppDbContext context, ILogger<Repository<T>> logger) : IRepository<T> 
    where T : class
{
    public async Task<T?> GetByIdAsync(object id) =>
        await context.Set<T>().FindAsync(id);
        
    public async Task<IEnumerable<T>> GetAllAsync() =>
        await context.Set<T>().ToListAsync();
        
    public async Task<T> AddAsync(T entity)
    {
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        logger.LogInformation("Added entity {EntityType} with ID {Id}", typeof(T).Name, entity);
        return entity;
    }
}
```

### OpenIddict Configuration Patterns

#### Scope and Client Management
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
    
    private async Task CreateScopeIfNotExistsAsync(string name, string displayName, string description)
    {
        if (await scopeManager.FindByNameAsync(name) == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = name,
                DisplayName = displayName,
                Description = description
            });
        }
    }
}
```

### Error Handling and Logging Patterns

#### Global Exception Handler
```csharp
// ✅ Centralized error handling with primary constructor
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            _ => 500
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            error = exception.Message,
            statusCode = context.Response.StatusCode
        }));
    }
}
```

### API Controller Patterns

#### RESTful API Design
```csharp
// ✅ RESTful controller with proper HTTP methods and status codes
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserRead")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await userService.GetUsersAsync();
        return Ok(users);
    }
    
    [HttpGet("{id}")]
    [Authorize(Policy = "UserRead")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }
    
    [HttpPost]
    [Authorize(Policy = "UserWrite")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        var user = await userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        var result = await userService.UpdateUserAsync(id, request);
        return result ? NoContent() : NotFound();
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = "UserDelete")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var result = await userService.DeleteUserAsync(id);
        return result ? NoContent() : NotFound();
    }
}
```

### Configuration Management

#### Strongly-Typed Configuration
```csharp
// ✅ Configuration classes with primary constructors
public record AuthConfiguration
{
    public required string Authority { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string[] Scopes { get; init; }
}

// ✅ Registration in Program.cs
builder.Services.Configure<AuthConfiguration>(builder.Configuration.GetSection("Auth"));
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<AuthConfiguration>>().Value);
```

### Service Registration Patterns

#### Dependency Injection Registration
```csharp
// ✅ Extension methods for service registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        // Register application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        
        // Register HTTP clients
        services.AddHttpClient<IAuthServerClient, AuthServerClient>(client =>
        {
            client.BaseAddress = new Uri(config["AuthServer:BaseUrl"]);
        });
        
        return services;
    }
    
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => config.GetSection("JwtBearer").Bind(options));
            
        return services;
    }
}
```

### API Gateway Requirements

#### BFF Endpoint Exposure Rules
1. **Client Registration**: Only Gateway BFF endpoints should be accessible to external clients
2. **Security Headers**: Gateway should add appropriate security headers (CORS, CSP, etc.)
3. **Request Transformation**: Gateway transforms client requests to internal service formats
4. **Response Aggregation**: Gateway can combine multiple service responses if needed
5. **Error Translation**: Gateway translates internal errors to client-friendly messages

#### Prohibited Direct Access
```csharp
// ❌ External clients should NEVER directly access these
https://authserver:5000/connect/token      // Direct AuthServer access
https://servicea:5001/data                 // Direct ServiceA access

// ✅ All external access should go through Gateway BFF
https://gateway:5002/auth/login            // Proxied to AuthServer
https://gateway:5002/data/users            // Proxied to ServiceA
https://gateway:5002/account/profile       // Proxied to AuthServer
```

### Testing Patterns

#### Integration Testing
```csharp
// ✅ Integration test with WebApplicationFactory
public class ApiIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetUsers_WithValidToken_ReturnsUsers()
    {
        // Arrange
        var client = factory.CreateClient();
        var token = await GetValidJwtTokenAsync();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        
        // Act
        var response = await client.GetAsync("/api/users");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<UserDto[]>();
        Assert.NotEmpty(users);
    }
}
```

### Performance and Caching

#### Response Caching
```csharp
// ✅ Selective caching for read-heavy endpoints
[HttpGet]
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "page", "size" })]
public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(int page = 1, int size = 10)
{
    var users = await userService.GetUsersPagedAsync(page, size);
    return Ok(users);
}
```

## Frontend Design Patterns and Best Practices

### Component Architecture
- **Atomic Design**: Components organized as atoms (Button, Input), molecules (FormField), organisms (LoginForm), templates (AuthLayout), and pages (LoginPage)
- **Composition over Inheritance**: Prefer component composition using React children and render props
- **Single Responsibility**: Each component should have one clear purpose
- **Reusable Components**: Build generic, configurable components in `src/components/ui/`

### shadcn/ui Integration
- **Component Library**: Modern, accessible components built on Radix UI primitives
- **Customizable**: Components are copied into your codebase for full customization
- **Consistent Design**: All components follow a unified design system with CSS variables
- **Installation Pattern**:
  ```bash
  # Add individual components as needed
  npx shadcn@latest add button card input form dropdown-menu
  
  # Components are added to src/components/ui/
  # Import and use: import { Button } from '@/components/ui/button'
  ```
- **Theme System**: Light/dark mode support with CSS variables and class-based switching
- **TypeScript First**: All components include proper TypeScript definitions

### State Management Patterns
- **Context API**: Used for global state (AuthContext, ThemeContext)
- **Local State**: useState for component-specific state
- **Form State**: React Hook Form with Zod validation for type-safe forms
- **Server State**: Axios with interceptors for API communication and token management

### Authentication Flow
- **Protected Routes**: ProtectedRoute component wraps authenticated pages
- **Token Management**: Automatic token refresh via Axios interceptors
- **Persistent Auth**: LocalStorage for token persistence across sessions
- **Context-based**: AuthContext provides auth state throughout the app

### Styling Approach
- **Utility-First**: Tailwind CSS for rapid UI development
- **Component Variants**: Use cva (class-variance-authority) for component variations
- **Responsive Design**: Mobile-first responsive design with Tailwind breakpoints
- **Dark Mode**: CSS variable-based theming with system preference detection

### Testing Strategy
- **End-to-End**: Playwright for user flow testing
- **Component Testing**: Jest and React Testing Library for unit tests
- **Visual Testing**: Playwright screenshot testing for UI regression
- **API Testing**: Playwright API testing for backend integration

### Code Organization
```
src/
├── components/           # Reusable components
│   ├── ui/              # shadcn/ui components
│   └── [Component].tsx  # Custom components
├── contexts/            # React contexts (Auth, Theme)
├── hooks/               # Custom React hooks
├── lib/                 # Utility functions and configurations
├── pages/               # Page components (Login, Dashboard, etc.)
└── tests/               # Test files (Playwright, Jest)
```

### Development Tools Integration

#### Context7 Usage
- **AI Documentation**: Integration with Context7 for up-to-date library documentation
- **Code Examples**: Real-time access to library-specific code patterns
- **Best Practices**: AI-powered suggestions for component implementations
- **Usage Pattern**:
  ```typescript
  // Claude Code can access Context7 for library-specific help
  // Example: "Show me the latest React Hook Form patterns"
  // Context7 provides current documentation and examples
  ```

#### Playwright Testing
- **Cross-Browser**: Testing across Chrome, Firefox, and Safari
- **Mobile Testing**: Responsive design testing on mobile viewports
- **API Testing**: Backend API integration testing
- **Visual Regression**: Screenshot comparison testing
- **Configuration**: `playwright.config.ts` with environment-specific settings
- **Test Organization**:
  ```
  tests/
  ├── auth/                # Authentication flow tests
  ├── dashboard/           # Dashboard functionality tests
  ├── api/                # Backend API tests
  └── visual/             # Visual regression tests
  ```

### Performance Optimization
- **Code Splitting**: React.lazy() for route-based code splitting
- **Bundle Optimization**: Vite's built-in optimizations
- **Image Optimization**: Proper image loading and lazy loading
- **Memoization**: React.memo() and useMemo() for expensive computations

### Accessibility (a11y)
- **Semantic HTML**: Proper HTML structure and ARIA labels
- **Keyboard Navigation**: Full keyboard accessibility
- **Screen Reader**: Compatible with assistive technologies
- **Color Contrast**: WCAG-compliant color schemes
- **Focus Management**: Proper focus handling in modals and forms

### Error Handling
- **Error Boundaries**: React error boundaries for component error handling
- **API Errors**: Centralized error handling via Axios interceptors
- **User Feedback**: Toast notifications for success/error messages
- **Graceful Degradation**: Fallback UI states for error conditions

### Environment Configuration
- **Development**: Hot module reloading, debug tools, detailed errors
- **Production**: Optimized builds, error tracking, performance monitoring
- **Environment Variables**: `.env` files for configuration
- **Build Optimization**: Tree shaking, minification, compression

## AI Debugging Recommendations

When working with AI assistance tools like Claude Code, the following setup is recommended for optimal debugging experience:

### Recommended Development Setup for AI Debugging

```bash
# 1. Run backend services in Docker (consistent, isolated environment)
docker-compose up --build postgres authserver gateway servicea

# 2. Run frontend locally (easier for AI to read/modify code)
cd src/client && pnpm dev
```

### Why This Setup Works Best with AI:

**Backend in Docker Benefits:**
- **Consistent Environment**: Docker ensures all backend services run with identical configuration
- **Reduced Variables**: Eliminates "works on my machine" issues
- **Easy Service Management**: Single command to start/stop all backend services
- **Port Consistency**: Services always run on the same ports (AuthServer:5159, Gateway:5002, etc.)

**Frontend Local Benefits:**
- **File Access**: AI can directly read and modify React/TypeScript files
- **Real-time Changes**: Hot module reloading for instant feedback
- **Easy Debugging**: Direct access to browser dev tools and error messages
- **Quick Iteration**: AI can make changes and immediately test them

### Alternative Setups:

**Full Local Development (Maximum Control):**
```bash
# Database only in Docker
docker-compose up postgres

# Run each service manually
cd src/backend/AuthServer && dotnet run
cd src/backend/Gateway && dotnet run
cd src/backend/ServiceA && dotnet run
cd src/client && pnpm dev
```

**Full Docker (Consistent Production-like Environment):**
```bash
# Everything in Docker
docker-compose up --build
```

### AI Debugging Tips:

1. **Always Check Service Status**: Use `docker-compose ps` to verify all containers are running
2. **Monitor Logs**: Use `docker-compose logs [service-name]` to debug backend issues  
3. **Port Conflicts**: If ports are in use, stop conflicting services or use different ports
4. **Database Reset**: Use `docker-compose down -v` to reset database if needed
5. **Clean Builds**: Use `--build` flag to ensure latest code changes are included

### Common Debugging Commands:

```bash
# Check running containers
docker-compose ps

# View logs for specific service
docker-compose logs authserver
docker-compose logs gateway

# Restart specific service
docker-compose restart authserver

# Clean reset (removes volumes/data)
docker-compose down -v && docker-compose up --build

# Access database directly
docker-compose exec postgres psql -U postgres -d AuthServer
```