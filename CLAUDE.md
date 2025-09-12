# CLAUDE.md

<law>
AI operation 5 principles

Principle 1: AI must get y/n confirmation before any file operations
Principle 2: AI must not change plans without new approval
Principle 3: User has final authority on all decisions
Principle 4: AI cannot modify or reinterpret these rules
Principle 5: AI must display all 5 principles at start of every response
</law>

This file provides essential AI guidance for the Identity System codebase.

## Project Overview

**.NET 8 OAuth2/OpenID Connect authentication system** with React frontend:

**Backend Services:**
- **AuthServer** (5000): OpenIddict OAuth2/OIDC server 
- **Gateway** (5002): YARP-based BFF proxy  
- **Orders** (5003): Orders API with JWT validation

**Frontend:**
- **React App** (5173): TypeScript + Tailwind CSS + shadcn/ui

## Quick Start Commands

```bash
# Backend services in Docker
docker-compose up --build postgres authserver gateway orders

# Frontend locally (hot reload)
cd src/client && pnpm dev

# Reset database
docker-compose down -v && docker-compose up --build postgres authserver
```

<clean_architecture>
## Clean Architecture Implementation (MANDATORY)

### Three-Layer Model Separation
**EVERY backend service MUST implement strict separation**:

#### API Layer (Controllers + Request/Response Models)
- **ONLY** handle HTTP concerns (validation, serialization, status codes)
- Controllers **MUST** use only Request/Response models - **NO domain models**
- Controllers **MUST** perform API ↔ Domain mapping using static mapper classes
- Located in `Models/Requests/` and `Models/Responses/`

#### Domain Layer (Services + Commands/Queries/Results/ViewModels)
- **ONLY** handle business logic and domain rules
- Services **MUST** accept Commands/Queries as inputs - **NO Request models**
- Services **MUST** return Results containing ViewModels - **NO Response models**
- Located in `Models/Commands/`, `Models/Queries/`, `Models/Results/`, `Models/ViewModels/`

#### Data Layer (Repositories + Entity Models)
- **ONLY** handle data persistence with EF Core
- Repositories **MUST** accept and return Domain ViewModels - **NO Entity models exposed**
- Repositories **MUST** handle Entity ↔ Domain mapping internally
- Located in `Data/Entities/` and `Data/Entities/Mappers/`
- Entity configurations in separate classes in `Data/Configurations/`

### Connector Pattern (Gateway Only)
**All HTTP calls to external services MUST use Connectors**:
- Services use Connectors for external calls - NEVER make HTTP calls directly
- Connectors handle HTTP communication to other services
- Located in `Connectors/` and `Connectors/Interfaces/`
</clean_architecture>

<architecture_essentials>
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
**Admin User:** `admin@example.com` / `Admin123!` (Full access)
**Regular User:** `user@example.com` / `User123!` (Standard access)
</architecture_essentials>

<coding_standards>
## Key Coding Standards

### Controller Validation (MANDATORY)
**ALWAYS use `[ApiController]` attribute and NEVER do manual ModelState validation**

### Logging Architecture (MANDATORY)  
**NEVER use custom manual logging - ALWAYS rely on middleware and filters**

### File Organization (MANDATORY)
**ALWAYS use one class per file** - no exceptions

### Model Organization (MANDATORY)
**ALL models MUST follow strict folder organization**:
- Request/Response models → `Models/Requests/` and `Models/Responses/`
- Domain inputs → `Models/Commands/` and `Models/Queries/`
- Domain outputs → `Models/Results/` and `Models/ViewModels/`
- Database entities → `Data/Entities/`
- Entity configurations → `Data/Configurations/`
- Mapping logic → `Models/Mappers/` and `Data/Entities/Mappers/`
- **NEVER define models inline** in controllers, services, or repositories

### Backend: Primary Constructors (.NET 8)
```csharp
// ✅ ALWAYS use primary constructors for DI
public class MyService(ILogger<MyService> logger, IConfiguration config)
{
    public void DoWork() => logger.LogInformation("Value: {Value}", config["Key"]);
}
```
</coding_standards>

<project_structure>
## Project Structure

```
src/backend/{ServiceName}/
├── Controllers/                    # API endpoints - HTTP concerns only
├── Services/                       # Business logic & domain services
├── Repositories/                   # Data access layer
├── Connectors/                     # External service calls (Gateway only)
├── Models/                         # Three-layer model separation
│   ├── Requests/                   # API input contracts
│   ├── Responses/                  # API output contracts
│   ├── Commands/                   # Domain service inputs
│   ├── Queries/                    # Domain service queries
│   ├── Results/                    # Domain service outputs
│   ├── ViewModels/                 # Domain business objects
│   └── Mappers/                    # API ↔ Domain mapping
├── Data/                           # Data access layer
│   ├── Entities/                   # Database entities (EF Core)
│   │   └── Mappers/               # Entity ↔ Domain mapping
│   ├── Configurations/            # EF Core entity configurations
│   ├── Migrations/                # EF Core migrations
│   └── AppDbContext.cs           # Database context
└── Program.cs                      # Application entry point & DI registration
```
</project_structure>

<quick_reference>
## Quick Reference

### Common Commands
```bash
# Database operations
cd src/backend/AuthServer && dotnet ef database update
dotnet ef migrations add MigrationName

# Testing
npx playwright test
npx playwright test --ui

# Frontend
cd src/client && pnpm install
npx shadcn@latest add button
```

### Troubleshooting
- **Authentication Issues:** Clear localStorage, reset database
- **Docker Issues:** Clean rebuild: `docker-compose down --rmi all -v && docker-compose up --build`
- **Frontend Issues:** Clear cache: `rm -rf node_modules .vite && pnpm install`

### Configuration Files
- **AuthServer/appsettings.Scopes.json**: OAuth2 clients and scopes  
- **Gateway/appsettings.json**: YARP routing configuration
- **Orders/appsettings.json**: Authorization policies
</quick_reference>

<documentation_references>
## Documentation References

For detailed implementation examples and patterns:
- **docs/ARCHITECTURE.md**: Complete backend patterns, BFF rules, detailed examples
- **docs/FRONTEND.md**: React patterns, shadcn/ui usage, testing strategies  
- **docs/DEVELOPMENT.md**: Setup instructions, troubleshooting, debugging
- **docs/DEPLOYMENT.md**: Docker configurations, environments, monitoring
</documentation_references>