# Development Setup and Troubleshooting

This document covers detailed development setup, common issues, and troubleshooting steps.

<prerequisites>
- .NET 8 SDK
- Node.js 18+ and pnpm
- Docker and Docker Compose
- Git
</prerequisites>

<setup_instructions>
<code_example language="bash">
# Clone repository
git clone <repository-url>
cd identity

# Setup backend
dotnet restore ArchZ.sln
dotnet build ArchZ.sln

# Setup frontend
cd src/client
pnpm install
cd ../..
</code_example>
</setup_instructions>

<development_workflow>
<environment_options>
#### Option 1: Recommended for AI Development
<code_example language="bash">
# Backend services in Docker
docker-compose up --build postgres authserver gateway servicea

# Frontend locally (separate terminal)
cd src/client
pnpm dev
</code_example>

#### Option 2: Full Local Development
<code_example language="bash">
# Start PostgreSQL only
docker-compose up postgres

# Terminal 1: AuthServer
cd src/backend/AuthServer
dotnet run

# Terminal 2: Gateway  
cd src/backend/Gateway
dotnet run

# Terminal 3: Orders
cd src/backend/Orders
dotnet run

# Terminal 4: Frontend
cd src/client
pnpm dev
</code_example>

#### Option 3: Full Docker
<code_example language="bash">
# Everything in Docker
docker-compose up --build
</code_example>
</environment_options>

<service_urls>
### Development URLs
- **Frontend**: http://localhost:5173 (Vite dev server)
- **Gateway**: http://localhost:5002 (BFF - Main API endpoint)
- **AuthServer**: http://localhost:5000 (OAuth2/OIDC)
- **Orders**: http://localhost:5003 (Resource API)
- **PostgreSQL**: localhost:5432

### Docker Internal URLs (for service-to-service communication)
- **AuthServer**: http://authserver:8080
- **Gateway**: http://gateway:8080
- **Orders**: http://orders:8080
- **PostgreSQL**: postgres:5432
</service_urls>

<common_development_tasks>
### Database Operations
<code_example language="bash">
# Reset database completely
docker-compose down -v
docker-compose up --build postgres authserver

# Apply EF migrations manually
cd src/backend/AuthServer
dotnet ef database update

# Create new migration
dotnet ef migrations add AddNewFeature

# View migration history
dotnet ef migrations list
</code_example>

### Frontend Development
<code_example language="bash">
# Install new package
cd src/client
pnpm add package-name

# Add shadcn/ui component
npx shadcn@latest add button

# Run tests
pnpm test              # Unit tests
npx playwright test    # E2E tests
npx playwright test --ui  # E2E tests with UI

# Build for production
pnpm build
</code_example>

### Backend Development
<code_example language="bash">
# Add NuGet package
cd src/backend/AuthServer
dotnet add package PackageName

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run tests
dotnet test ArchZ.sln

# Watch for changes
dotnet watch run
</code_example>
</common_development_tasks>
</development_workflow>

<troubleshooting>
### Authentication Issues

#### "401 Unauthorized" errors
1. Check token expiration:
<code_example language="javascript">
// In browser console
const token = localStorage.getItem('accessToken')
console.log(JSON.parse(atob(token.split('.')[1])))
</code_example>

2. Verify token refresh logic:
<code_example language="bash">
# Check refresh token endpoint
curl -X POST http://localhost:5002/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"your-refresh-token"}'
</code_example>

3. Check CORS configuration:
<code_example language="bash">
# Browser Network tab should show CORS headers
Access-Control-Allow-Origin: http://localhost:5173
Access-Control-Allow-Credentials: true
</code_example>

#### Database Connection Issues
<code_example language="bash">
# Test database connection
docker-compose exec postgres psql -U postgres -d AuthServer -c "SELECT 1;"

# Check database exists
docker-compose exec postgres psql -U postgres -c "\l"

# Recreate database
docker-compose exec postgres psql -U postgres -c "DROP DATABASE IF EXISTS \"AuthServer\";"
docker-compose exec postgres psql -U postgres -c "CREATE DATABASE \"AuthServer\";"
</code_example>

### Docker Issues

#### Port Already in Use
<code_example language="bash">
# Find process using port
lsof -i :5002
netstat -tulpn | grep :5002

# Kill process
kill -9 <PID>

# Or use different ports in docker-compose.yml
ports:
  - "5004:8080"  # Change 5002 to 5004
</code_example>

#### Container Build Failures
<code_example language="bash">
# Clean Docker cache
docker system prune -a
docker-compose down --rmi all -v

# Rebuild from scratch
docker-compose build --no-cache
docker-compose up --build
</code_example>

#### Volume Permission Issues (Linux/macOS)
<code_example language="bash">
# Fix volume permissions
sudo chown -R $USER:$USER ./src

# Or use bind mounts in docker-compose.yml
volumes:
  - ./src/backend/AuthServer:/app:delegated
</code_example>

### Frontend Issues

#### Build Failures
<code_example language="bash">
# Clear node_modules and reinstall
cd src/client
rm -rf node_modules pnpm-lock.yaml
pnpm install

# Clear Vite cache
rm -rf .vite
pnpm dev
</code_example>

#### Hot Reload Not Working
<code_example language="typescript">
# Check Vite config for host binding
# vite.config.ts should have:
export default defineConfig({
  server: {
    host: true,  // Bind to all addresses
    port: 5173
  }
})
</code_example>

#### TypeScript Errors
<code_example language="bash">
# Restart TypeScript service in VS Code
Ctrl+Shift+P -> "TypeScript: Restart TS Server"

# Or check tsconfig.json paths
"baseUrl": ".",
"paths": {
  "@/*": ["./src/*"]
}

# Check for missing dependencies
pnpm install

# Type check without emitting files
npx tsc --noEmit

# Clear TypeScript cache
rm -rf node_modules/.cache/.tsbuildinfo
</code_example>

#### Clean Architecture Issues

##### Namespace/Import Errors After Restructuring
<code_example language="bash">
# After moving entities to Data/Entities/, update all references
find src/backend/ServiceName -name "*.cs" -exec sed -i 's/using ServiceName\.Entities;/using ServiceName.Data.Entities;/g' {} +

# Build to identify remaining issues
cd src/backend/ServiceName
dotnet build

# Common fixes for mapper references
# Update: using ServiceName.Entities.Mappers;
# To:    using ServiceName.Data.Entities.Mappers;
</code_example>

##### EF Core Configuration Errors
<code_example language="bash">
# If ApplyConfigurationsFromAssembly doesn't find configurations
# Check namespace matches assembly:
# Configurations should be in: ServiceName.Data.Configurations
# DbContext should be in: ServiceName.Data

# Test configuration loading
cd src/backend/ServiceName
dotnet ef dbcontext info --verbose

# Generate new migration to verify config
dotnet ef migrations add TestConfiguration
</code_example>

##### Clean Architecture Violations
<code_example language="csharp">
// ❌ Controller directly using Entity (WRONG)
public IActionResult Create(OrderEntity entity) // BAD

// ✅ Controller using Request/Response models (CORRECT)  
public IActionResult Create(CreateOrderRequest request) // GOOD

// ❌ Service directly using Request model (WRONG)
public Task<Result> CreateOrder(CreateOrderRequest request) // BAD

// ✅ Service using Command model (CORRECT)
public Task<Result> CreateOrder(CreateOrderCommand command) // GOOD

// ❌ Repository exposing Entity (WRONG)
public Task<OrderEntity> GetById(int id) // BAD

// ✅ Repository using ViewModel (CORRECT)
public Task<OrderViewModel> GetById(int id) // GOOD
</code_example>

### Service Communication Issues

#### Services Can't Reach Each Other
1. Check Docker network:
<code_example language="bash">
docker network ls
docker network inspect identity_default
</code_example>

2. Test internal connectivity:
<code_example language="bash">
# From inside gateway container
docker-compose exec gateway curl http://authserver:8080/health
</code_example>

3. Verify environment variables:
<code_example language="bash">
# Check environment in running container
docker-compose exec gateway env | grep -i auth
</code_example>

#### YARP Routing Issues
Check Gateway configuration:
<code_example language="json">
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "authserver",
        "Match": {
          "Path": "/auth/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "authserver": {
        "Destinations": {
          "destination1": {
            "Address": "http://authserver:8080/"
          }
        }
      }
    }
  }
}
</code_example>

#### JWT and Authentication Debugging
<code_example language="bash">
# Debug JWT token contents
TOKEN="your-jwt-here"
echo $TOKEN | cut -d. -f2 | base64 -d | jq .

# Test token validation manually
curl -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  http://localhost:5002/auth/user

# Check token expiration
node -e "
const token = 'your-jwt-here';
const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64'));
console.log('Expires:', new Date(payload.exp * 1000));
console.log('Current:', new Date());
"

# Verify OAuth2 scopes
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=gateway-bff&client_secret=gateway-secret&scope=orders.read"
</code_example>

#### Service Startup and Health Issues  
<code_example language="bash">
# Check all services are running
docker-compose ps

# View logs for failed services
docker-compose logs authserver --tail 50
docker-compose logs gateway --tail 50  
docker-compose logs orders --tail 50

# Test health endpoints
curl http://localhost:5000/health  # AuthServer
curl http://localhost:5002/health  # Gateway
curl http://localhost:5003/health  # Orders

# Check database connection from services
docker-compose exec authserver curl http://localhost:8080/health
docker-compose exec postgres pg_isready -U postgres

# Verify environment configuration
docker-compose exec gateway env | grep -E "(AUTH|DATABASE|CORS)"
</code_example>

#### Clean Architecture Data Flow Issues
<code_example language="bash">
# Debug mapping issues
# 1. Check if mappers are being called correctly
# Add logging to mapper classes:

# 2. Verify layer separation
# Controllers should only use Requests/Responses
# Services should only use Commands/Queries/Results/ViewModels  
# Repositories should only use ViewModels internally

# 3. Common mapping errors to check:
# - Missing mapper method
# - Wrong direction (Entity->Domain vs Domain->Entity)
# - Null reference in nested mappings
# - Incorrect property mapping

# Test individual layers:
cd src/backend/ServiceName.Tests
dotnet test --filter "MapperTests"
dotnet test --filter "RepositoryTests" 
dotnet test --filter "ServiceTests"
</code_example>

### Performance Issues

#### Slow Database Queries
<code_example language="sql">
-- Enable query logging in PostgreSQL
ALTER SYSTEM SET log_statement = 'all';
SELECT pg_reload_conf();

-- View slow queries
SELECT query, mean_exec_time, calls 
FROM pg_stat_statements 
ORDER BY mean_exec_time DESC;
</code_example>

#### Frontend Bundle Size
<code_example language="bash">
# Analyze bundle
cd src/client
pnpm build
npx vite-bundle-analyzer dist

# Check for large dependencies
npx depcheck
</code_example>
</troubleshooting>

<development_tools>
### Recommended VS Code Extensions
- C# Dev Kit
- TypeScript Importer
- Tailwind CSS IntelliSense
- Auto Rename Tag
- Prettier
- ESLint
- Docker
- Thunder Client (API testing)

### Browser DevTools
- React Developer Tools
- Redux DevTools (if using Redux)
- Lighthouse for performance
- Network tab for API debugging

### Database Tools
- pgAdmin (web interface)
- DBeaver (desktop client)
- TablePlus (macOS)
</development_tools>

<testing_guidelines>
### Running Tests

#### Backend Testing
<code_example language="bash">
# Run all backend tests
dotnet test ArchZ.sln --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
cd src/backend/AuthServer.Tests
dotnet test --verbosity detailed

# Run tests matching pattern
dotnet test --filter "FullyQualifiedName~AuthenticationService"

# Watch mode for continuous testing
dotnet watch test
</code_example>

#### Frontend Testing
<code_example language="bash">
# Unit tests with Jest/Vitest
cd src/client
pnpm test

# Unit tests in watch mode
pnpm test:watch

# Coverage report
pnpm test:coverage

# Component testing with React Testing Library
pnpm test -- --testPathPattern=components

# Specific test file
pnpm test -- UserAuthForm.test.tsx
</code_example>

#### End-to-End Testing with Playwright
<code_example language="bash">
# Run all E2E tests
npx playwright test

# Run tests in headed mode (see browser)
npx playwright test --headed

# Debug tests with UI
npx playwright test --ui

# Run specific test file
npx playwright test tests/auth-flow.spec.ts

# Run tests in specific browser
npx playwright test --project=chromium

# Generate test reports
npx playwright show-report

# Update test screenshots (visual regression)
npx playwright test --update-snapshots
</code_example>

#### Integration Testing
<code_example language="bash">
# Test complete auth flow with curl
curl -X POST http://localhost:5002/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}'

# Test JWT validation across services
TOKEN="your-jwt-token"
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5002/orders/data

# Test service-to-service communication
docker-compose exec gateway curl http://authserver:8080/health
docker-compose exec gateway curl http://orders:8080/health

# Load testing with Apache Bench (if installed)
ab -n 100 -c 10 http://localhost:5002/auth/user
</code_example>

#### Database Testing
<code_example language="bash">
# Reset test database for consistent testing
docker-compose down -v
docker-compose up --build postgres authserver

# Run migrations in test environment
cd src/backend/AuthServer
ASPNETCORE_ENVIRONMENT=Test dotnet ef database update

# Seed test data
cd src/backend/AuthServer
dotnet run --environment=Test --seed-data

# Test database queries directly
docker-compose exec postgres psql -U postgres -d AuthServer -c "
SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"Email\" = 'admin@example.com';
"
</code_example>

### Code Quality
<code_example language="bash">
# Format code
cd src/client
pnpm format

# Lint code
pnpm lint

# Type check
pnpm type-check

# .NET format
dotnet format ArchZ.sln
</code_example>

### Pre-commit Hooks
<code_example language="bash">
# Install husky for git hooks
cd src/client
pnpm add --save-dev husky lint-staged

# Add to package.json
{
  "husky": {
    "hooks": {
      "pre-commit": "lint-staged"
    }
  },
  "lint-staged": {
    "*.{ts,tsx}": ["prettier --write", "eslint --fix"]
  }
}
</code_example>
</testing_guidelines>

<environment_configuration>
### Development Environment
- Detailed error messages
- Hot reloading enabled
- Relaxed CORS policies
- Development certificates
- Seeded test data

### Docker Environment  
- Container networking
- Internal service discovery
- Persistent volumes
- Health checks

### Production Environment
- Optimized builds
- Security headers
- SSL certificates
- Error logging
- Performance monitoring
</environment_configuration>

<debugging_tips>
### Backend Debugging
<code_example language="bash">
# Attach debugger in VS Code
# Use launch.json configuration

# View detailed logs
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --verbosity detailed

# Database debugging
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information
</code_example>

### Frontend Debugging
<code_example language="javascript">
// Debug API calls
localStorage.debug = 'axios:*'

// Debug React renders
import { unstable_trace as trace } from 'react'
trace('component-render', () => {
  // Component logic
})

// Debug performance
import { Profiler } from 'react'
function onRender(id, phase, actualDuration) {
  console.log('Profiler', { id, phase, actualDuration })
}
</code_example>

### Docker Debugging
<code_example language="bash">
# Access container shell
docker-compose exec gateway bash

# View container processes
docker-compose exec gateway ps aux

# Monitor resource usage
docker stats

# View container logs with details
docker-compose logs --details gateway
</code_example>
</debugging_tips>