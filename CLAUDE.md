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
- **OpenIddict 7.0.0**: OAuth2/OpenID Connect server implementation
- **ASP.NET Core Identity**: User management and authentication
- **YARP 2.1.0**: Reverse proxy for Gateway BFF service
- **Entity Framework Core**: Data access with PostgreSQL provider
- **JWT Bearer Authentication**: Token validation middleware

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