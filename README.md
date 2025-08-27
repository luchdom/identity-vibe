# .NET 8 Authentication and Authorization System with Modern React Frontend

This project implements a complete full-stack authentication and authorization system using .NET 8, OpenIddict, JWT tokens, YARP reverse proxy, and a modern React frontend. It consists of four main components:

1. **AuthServer** - OpenIddict-based authentication server with ASP.NET Core Identity
2. **Gateway** - YARP-based Backend for Frontend (BFF) service with authentication handling  
3. **ServiceA** - Resource API that validates tokens and enforces authorization policies
4. **React Frontend** - Modern React 18 application with TypeScript, shadcn/ui, and comprehensive authentication flow

## Project Structure

```
identity/
├── AuthServer/                 # Authentication Server
│   ├── Configuration/
│   │   ├── ScopeConfiguration.cs
│   │   ├── UserScopes.cs
│   │   ├── ServiceClients.cs
│   │   └── ServiceClientConfig.cs
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   ├── AuthorizationController.cs
│   │   └── ScopeController.cs
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── AppUser.cs
│   │   ├── AppRole.cs
│   │   └── [other Identity entities]
│   ├── Models/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── ForgotPasswordRequest.cs
│   │   ├── ResetPasswordRequest.cs
│   │   └── AccountResponse.cs
│   ├── Services/
│   │   ├── ScopeConfigurationService.cs
│   │   └── DatabaseSeeder.cs
│   ├── appsettings.json
│   ├── appsettings.Scopes.json
│   └── Program.cs
├── Gateway/                    # BFF Gateway Service
│   ├── Controllers/
│   │   └── AuthController.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Docker.json
│   ├── Dockerfile
│   └── Program.cs
├── ServiceA/                   # Resource API
│   ├── Configuration/
│   │   ├── AuthConfiguration.cs
│   │   ├── AuthenticationProviders.cs
│   │   ├── UserIdentity.cs
│   │   ├── ServiceIdentity.cs
│   │   ├── AuthorizationPolicies.cs
│   │   └── PolicyConfig.cs
│   ├── Controllers/
│   │   └── DataController.cs
│   ├── Services/
│   │   └── AuthorizationService.cs
│   ├── appsettings.json
│   └── Program.cs
├── src/client/                 # React Frontend Application
│   ├── public/                 # Static assets
│   ├── src/
│   │   ├── components/         # Reusable React components
│   │   │   ├── ui/             # shadcn/ui components
│   │   │   ├── ModeToggle.tsx  # Theme toggle component
│   │   │   └── ProtectedRoute.tsx
│   │   ├── contexts/           # React contexts
│   │   │   ├── AuthContext.tsx # Authentication state management
│   │   │   └── ThemeContext.tsx# Theme state management
│   │   ├── hooks/              # Custom React hooks
│   │   ├── lib/                # Utility functions
│   │   │   └── api.ts          # Axios HTTP client
│   │   ├── pages/              # Page components
│   │   │   ├── LoginPage.tsx   # Login with form validation
│   │   │   ├── RegisterPage.tsx # User registration
│   │   │   ├── DashboardPage.tsx # Protected dashboard
│   │   │   └── ErrorPage.tsx   # 404 and error pages
│   │   ├── App.tsx             # Main app component with routing
│   │   └── main.tsx            # React app entry point
│   ├── tests/                  # Playwright E2E tests
│   ├── components.json         # shadcn/ui configuration
│   ├── tailwind.config.js      # Tailwind CSS configuration
│   ├── playwright.config.ts    # Playwright test configuration
│   ├── package.json            # Frontend dependencies
│   └── vite.config.ts          # Vite build configuration
├── .editorconfig
├── .gitignore
├── CLAUDE.md
├── DOCKER.md
├── README.md
├── docker-compose.yml
├── docker-compose.dev.yml
├── docker-compose.prod.yml
└── IdentitySolution.sln
```

## Features

### AuthServer
- **OpenIddict** for OAuth2 and OpenID Connect server
- **ASP.NET Core Identity** for user management
- **Password Flow** for user authentication
- **Client Credentials Flow** for service-to-service authentication
- **JWT Token** issuance with claims
- **User Registration** and account management
- **Password Reset** functionality
- **Database** storage using Entity Framework Core with PostgreSQL
- **Database Seeding** with default admin user and roles
- **CORS** configuration
- **Scope Management** API endpoints

### Gateway (BFF)
- **YARP Reverse Proxy** for routing requests to backend services
- **JWT Bearer Authentication** with token validation
- **BFF Authentication Controller** for login, refresh, and user management
- **CORS** configuration for frontend applications
- **Health Check** endpoint
- **Request Transformation** and header forwarding
- **Authorization Policy** enforcement on routes

### ServiceA
- **JWT Bearer** authentication with token validation
- **Dynamic Authorization Policies** based on appsettings.json
- **Scope-based** access control (internal.servicea.read, internal.servicea.create, etc.)
- **Multi-issuer** support for token validation
- **CORS** configuration
- **Token Information** endpoint for debugging

### React Frontend
- **React 18** with modern functional components and hooks
- **TypeScript** for full type safety throughout the application
- **Vite** for fast development and optimized production builds
- **shadcn/ui** component library built on Radix UI primitives
- **Tailwind CSS** for utility-first responsive styling
- **React Router** for client-side routing with protected routes
- **React Hook Form** with Zod validation for type-safe forms
- **Authentication Flow** with automatic token refresh and persistence
- **Dark/Light Theme** support with system preference detection
- **Responsive Design** optimized for desktop and mobile devices
- **Playwright** for comprehensive end-to-end testing
- **Context7 Integration** for AI-powered documentation access

## Configuration

### AuthServer Configuration
The AuthServer uses a configuration-driven approach for scopes and clients defined in `appsettings.Scopes.json`:

#### User Scopes
- **Default Scopes**: `openid`, `profile`, `email`
- **Role-based Scopes**:
  - **Admin**: All ServiceA scopes + admin scopes
  - **User**: `data.read data.write`
  - **Service**: ServiceA CRUD scopes

#### Service Clients
1. **web-client** (Password Flow)
   - Client ID: `web-client`
   - Client Secret: `secret`
   - Scopes: `openid`, `profile`, `email`, `data.read data.write`
   - Grant Types: Password, Refresh Token

2. **ServiceA** (Client Credentials Flow)
   - Client ID: `ServiceA`
   - Client Secret: `servicea-secret`
   - Scopes: `internal.servicea.read`, `internal.servicea.create`, `internal.servicea.update`, `internal.servicea.delete`
   - Grant Types: Client Credentials

3. **mobile-app** (Password Flow)
   - Client ID: `mobile-app`
   - Client Secret: `mobile-secret`
   - Scopes: `openid`, `profile`, `email`, `data.read data.write`, `offline_access`
   - Grant Types: Password, Refresh Token

4. **gateway-bff** (Password + Client Credentials Flow)
   - Client ID: `gateway-bff`
   - Client Secret: `gateway-secret`
   - Scopes: `openid`, `profile`, `email`, `data.read data.write`, `offline_access`
   - Grant Types: Password, Refresh Token, Client Credentials

### ServiceA Authorization Policies
ServiceA uses dynamic authorization policies defined in `appsettings.json`:

```json
{
  "Auth": {
    "AuthenticationProviders": {
      "UserIdentity": {
        "Authority": "https://localhost:5000",
        "AuthorizationPolicies": {
          "UserIdentity": {
            "Scopes": [ "data.read data.write" ]
          }
        }
      },
      "ServiceIdentity": {
        "Authority": "https://localhost:5000",
        "AuthorizationPolicies": {
          "ServiceIdentityRead": { "Scopes": [ "internal.servicea.read" ] },
          "ServiceIdentityCreate": { "Scopes": [ "internal.servicea.create" ] },
          "ServiceIdentityUpdate": { "Scopes": [ "internal.servicea.update" ] },
          "ServiceIdentityDelete": { "Scopes": [ "internal.servicea.delete" ] }
        }
      }
    }
  }
}
```

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL (or use Docker)
- Node.js 18+ with pnpm (for frontend)
- Visual Studio 2022 or VS Code

### Option 1: Docker (Recommended)

The easiest way to run the entire system is using Docker Compose:

```bash
# Development with hot reloading
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Production
docker-compose -f docker-compose.prod.yml up --build -d

# Basic setup
docker-compose up --build
```

For detailed Docker instructions, see [DOCKER.md](DOCKER.md).

### Option 2: Local Development

### Running the Backend Services

1. **Start the AuthServer**:
   ```bash
   cd src/backend/AuthServer
   dotnet run
   ```
   The AuthServer will run on `http://localhost:5000`

2. **Start the Gateway**:
   ```bash
   cd src/backend/Gateway
   dotnet run
   ```
   The Gateway will run on `http://localhost:5002`

3. **Start ServiceA**:
   ```bash
   cd src/backend/ServiceA
   dotnet run
   ```
   ServiceA will run on `http://localhost:5001`

### Running the Frontend Application

4. **Start the React Frontend**:
   ```bash
   cd src/client
   pnpm install
   pnpm dev
   ```
   The React application will run on `http://localhost:5173`

### Frontend Development Commands

```bash
# Install dependencies
cd src/client
pnpm install

# Start development server with hot reloading
pnpm dev

# Build for production
pnpm build

# Preview production build
pnpm preview

# Run tests
pnpm test

# Add shadcn/ui components
npx shadcn@latest add [component-name]

# Run Playwright E2E tests
npx playwright test

# Run Playwright tests in UI mode
npx playwright test --ui

# Run Playwright tests with debugging
npx playwright test --debug
```

### Database Setup
The AuthServer will automatically create the database and seed the OpenIddict entities and a default admin user on first run.

**Default Admin User**:
- Email: `admin@example.com`
- Password: `Admin123!`

## Testing the System

### Frontend Testing

The React application provides a complete user interface for authentication:

1. **Login Page**: Navigate to `http://localhost:5173/login`
   - Pre-filled with admin credentials: `admin@example.com` / `Admin123!`
   - Supports theme switching (Light/Dark/System)
   - Form validation with TypeScript and Zod

2. **Registration Page**: Navigate to `http://localhost:5173/register`
   - Complete user registration form with validation
   - Automatic redirect to dashboard after successful registration

3. **Dashboard**: Protected route at `http://localhost:5173/dashboard`
   - Displays user information and system statistics
   - Theme toggle and logout functionality in header
   - Responsive design for mobile and desktop

4. **E2E Testing with Playwright**:
   ```bash
   cd src/client
   npx playwright test
   ```

### API Testing

### 1. User Registration

**Direct to AuthServer**:
```bash
curl -X POST http://localhost:5000/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "confirmPassword": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Via Gateway**:
```bash
curl -X POST http://localhost:5002/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "confirmPassword": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 2. Get Token for User (Password Flow)

**Direct to AuthServer**:
```bash
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@example.com&password=Admin123!&client_id=web-client&client_secret=secret&scope=data.read data.write"
```

**Via Gateway BFF**:
```bash
curl -X POST http://localhost:5002/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Admin123!"
  }'
```

**Response**:
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

### 3. Forgot Password

**Request**:
```bash
curl -X POST https://localhost:5000/api/account/forgot-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com"
  }'
```

### 4. Reset Password

**Request**:
```bash
curl -X POST https://localhost:5000/api/account/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "token": "RESET_TOKEN_FROM_FORGOT_PASSWORD",
    "newPassword": "NewPassword123!",
    "confirmPassword": "NewPassword123!"
  }'
```

### 5. Get Token for Service (Client Credentials Flow)

**Request**:
```bash
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=ServiceA&client_secret=servicea-secret&scope=internal.servicea.read internal.servicea.create"
```

### 6. Access ServiceA Endpoints

**Direct to ServiceA**:
```bash
# Get Data (requires internal.servicea.read scope)
curl -X GET http://localhost:5001/data \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Get User Data (requires data.read data.write scope)
curl -X GET http://localhost:5001/data/user \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Create Data (requires internal.servicea.create scope)
curl -X POST http://localhost:5001/data \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "\"New Data Item\""
```

**Via Gateway**:
```bash
# Get Data through Gateway (requires internal.servicea.read scope)
curl -X GET http://localhost:5002/data \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Get User Data through Gateway (requires data.read data.write scope)
curl -X GET http://localhost:5002/data/user \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Create Data through Gateway (requires internal.servicea.create scope)
curl -X POST http://localhost:5002/data \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "\"New Data Item via Gateway\""
```

## API Endpoints

### AuthServer Endpoints (localhost:5000)
- `POST /connect/token` - Token endpoint for OAuth2 flows
- `POST /account/register` - User registration
- `POST /account/login` - User login
- `POST /account/logout` - User logout
- `POST /account/forgot-password` - Request password reset
- `POST /account/reset-password` - Reset password with token
- `GET /account/profile` - Get user profile (requires authentication)
- `GET /scope/clients` - Get all configured clients
- `GET /scope/clients/{clientId}` - Get specific client configuration
- `GET /scope/scopes` - Get all available scopes
- `GET /scope/validate-client` - Validate client credentials
- `GET /scope/validate-scope` - Validate scope for client

### Gateway Endpoints (localhost:5002)
- `GET /health` - Health check endpoint
- `POST /auth/login` - BFF login endpoint
- `POST /auth/refresh` - Token refresh endpoint
- `GET /auth/user` - Get current user information
- `POST /auth/logout` - BFF logout endpoint
- `POST /account/*` - Proxied AuthServer account endpoints
- `GET /scope/*` - Proxied AuthServer scope endpoints
- `POST /connect/*` - Proxied AuthServer OAuth endpoints
- `GET|POST|PUT|DELETE /data/*` - Proxied ServiceA data endpoints

### ServiceA Endpoints (localhost:5001)
- `GET /data` - Get all data (requires `internal.servicea.read` scope)
- `GET /data/user` - Get user-specific data (requires `data.read data.write` scope)
- `POST /data` - Create new data (requires `internal.servicea.create` scope)
- `PUT /data/{id}` - Update data (requires `internal.servicea.update` scope)
- `DELETE /data/{id}` - Delete data (requires `internal.servicea.delete` scope)
- `GET /data/info` - Get token information (requires authentication)

## Code Organization

### Modern C# Patterns
- **Primary Constructors**: All service classes and controllers use C# 12 primary constructor syntax
- **Records with Required Properties**: DTOs use `required` modifier for compile-time safety
- **File-Scoped Namespaces**: Reduced indentation with modern namespace declarations
- **Collection Expressions**: Modern `[]` syntax for arrays and collections
- **Async/Await**: Proper async patterns throughout the application

### Project Structure
- **Configuration Folder**: Root-level folder for configuration models with dedicated namespace
- **Separated Models**: Each configuration class in its own file for better maintainability
- **App Prefix**: Identity entities use concise "App" prefix instead of "Application"
- **EditorConfig**: Enforces consistent code style and modern C# preferences

### Model Categories
- **Request/Response Models**: API DTOs with validation attributes
- **Configuration Models**: Application settings and policy definitions
- **Identity Entities**: EF Core entities for user management (mutable classes)
- **Data Models**: Database context and entity configurations

## Security Features

- **JWT Token Validation** with proper signature verification
- **Scope-based Authorization** with dynamic policy registration
- **CORS Protection** with configurable allowed origins
- **HTTPS Enforcement** in production
- **Token Expiration** and lifetime validation

## Docker Support

The project includes comprehensive Docker support with multiple configurations:

### Quick Start with Docker
```bash
# Development environment with hot reloading
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Production environment
docker-compose -f docker-compose.prod.yml up --build -d
```

### Docker Services
- **PostgreSQL**: Database with persistent storage
- **AuthServer**: Authentication server with Identity
- **Gateway**: YARP-based BFF with authentication handling
- **ServiceA**: Resource API with authorization

### Docker Ports
- AuthServer: `http://localhost:5000`
- Gateway: `http://localhost:5002`
- ServiceA: `http://localhost:5003`
- PostgreSQL: `localhost:5432`

For complete Docker documentation, see [DOCKER.md](DOCKER.md).

## Development Notes

- The system uses development certificates for JWT signing
- Database is automatically created on first run
- CORS is configured for local development
- All passwords and secrets should be changed in production

## Production Considerations

1. **Use proper certificates** for JWT signing and encryption
2. **Store secrets securely** using Azure Key Vault or similar
3. **Configure proper CORS** for production domains
4. **Use HTTPS** in production
5. **Implement proper user management** and password hashing
6. **Add logging and monitoring**
7. **Configure proper database** with connection pooling 