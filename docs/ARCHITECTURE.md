# Backend Architecture

This document details the backend architecture patterns and best practices for the Identity System.

## Service Overview

### AuthServer (Port 5000)
- **OpenIddict Server**: OAuth2/OpenID Connect with JWT token issuance
- **ASP.NET Core Identity**: User management with password policies
- **Entity Framework Core**: Data access with PostgreSQL
- **Scope Configuration**: Dynamic client/scope management via `appsettings.Scopes.json`

### Gateway (Port 5002) - BFF Pattern
- **YARP Reverse Proxy**: Routes requests to AuthServer and ServiceA
- **BFF Authentication Controller**: Handles login, refresh, and user info
- **JWT Bearer Authentication**: Validates tokens from AuthServer
- **CORS Configuration**: Supports frontend applications

### ServiceA (Port 5003)
- **JWT Bearer Authentication**: Token validation from AuthServer
- **Dynamic Authorization Service**: Policy registration based on configuration
- **Multi-issuer Support**: Can validate tokens from multiple providers
- **Scope-based Access Control**: Authorization policies in `appsettings.json`

## BFF (Backend for Frontend) Pattern

### MANDATORY Rules
- **All external client endpoints MUST be exposed through Gateway BFF**
- **Direct access to AuthServer/ServiceA is PROHIBITED for external clients**
- **Gateway acts as single entry point** for all client requests

### Routing Strategy
```csharp
// ✅ Correct - External clients access through Gateway BFF
POST /auth/login          -> Gateway -> AuthServer /connect/token
GET /auth/user           -> Gateway -> AuthServer /account/profile  
GET /data/users          -> Gateway -> ServiceA /data
POST /account/register   -> Gateway -> AuthServer /account/register
```

## Coding Standards

### Primary Constructors (.NET 8)
**ALWAYS use primary constructors** for dependency injection:

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

## Authentication Flow Types

1. **Password Flow**: User authentication (clients: web-client, mobile-app, gateway-bff)
2. **Client Credentials Flow**: Service-to-service authentication (clients: ServiceA, gateway-bff)  
3. **Refresh Token Flow**: Token renewal for long-lived sessions

## Service Communication Patterns

### Internal Service Communication
```csharp
// ✅ Service-to-service with client credentials
public class ServiceAController(IHttpClientFactory httpClientFactory)
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

### JWT Token Validation
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

### Dynamic Authorization Policies
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

## Database and Entity Framework

### Entity Configuration
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

## OpenIddict Configuration

### Scope and Client Management
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

## Key Configuration Files

- `AuthServer/appsettings.Scopes.json`: Client and scope definitions
- `Gateway/appsettings.json`: YARP routing and authentication configuration  
- `ServiceA/appsettings.json`: Authorization policies and authentication providers
- All services support environment-specific configurations (Development, Docker, Production)

## Database Seeding

AuthServer automatically creates:
- Default admin user: `admin@example.com` / `Admin123!`
- OpenIddict entities (clients, scopes, applications)
- Database schema via Entity Framework migrations

## Service Endpoints

### AuthServer (localhost:5000)
- `/connect/token` - OAuth2 token endpoint
- `/account/register` - User registration
- `/account/login` - User authentication
- `/account/profile` - User profile management
- `/scope/*` - Client and scope management

### Gateway (localhost:5002) - BFF Endpoints
- `/auth/login` - BFF authentication
- `/auth/refresh` - Token refresh
- `/auth/user` - Current user information
- `/auth/logout` - BFF logout
- `/account/*` - Proxied AuthServer endpoints
- `/data/*` - Proxied ServiceA endpoints

### ServiceA (localhost:5003)
- `/data` - CRUD operations with scope-based authorization
- `/data/user` - User-specific data endpoints
- `/data/info` - Token information endpoint