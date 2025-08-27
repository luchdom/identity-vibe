# Development Setup and Troubleshooting

This document covers detailed development setup, common issues, and troubleshooting steps.

## Initial Setup

### Prerequisites
- .NET 8 SDK
- Node.js 18+ and pnpm
- Docker and Docker Compose
- Git

### Clone and Setup
```bash
# Clone repository
git clone <repository-url>
cd identity

# Setup backend
dotnet restore IdentitySolution.sln
dotnet build IdentitySolution.sln

# Setup frontend
cd src/client
pnpm install
cd ../..
```

### Development Environment Options

#### Option 1: Recommended for AI Development
```bash
# Backend services in Docker
docker-compose up --build postgres authserver gateway servicea

# Frontend locally (separate terminal)
cd src/client
pnpm dev
```

#### Option 2: Full Local Development
```bash
# Start PostgreSQL only
docker-compose up postgres

# Terminal 1: AuthServer
cd src/backend/AuthServer
dotnet run

# Terminal 2: Gateway  
cd src/backend/Gateway
dotnet run

# Terminal 3: ServiceA
cd src/backend/ServiceA
dotnet run

# Terminal 4: Frontend
cd src/client
pnpm dev
```

#### Option 3: Full Docker
```bash
# Everything in Docker
docker-compose up --build
```

## Service URLs

### Development URLs
- **Frontend**: http://localhost:5173 (Vite dev server)
- **Gateway**: http://localhost:5002 (BFF - Main API endpoint)
- **AuthServer**: http://localhost:5000 (OAuth2/OIDC)
- **ServiceA**: http://localhost:5003 (Resource API)
- **PostgreSQL**: localhost:5432

### Docker Internal URLs (for service-to-service communication)
- **AuthServer**: http://authserver:8080
- **Gateway**: http://gateway:8080
- **ServiceA**: http://servicea:8080
- **PostgreSQL**: postgres:5432

## Common Development Tasks

### Database Operations
```bash
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
```

### Frontend Development
```bash
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
```

### Backend Development
```bash
# Add NuGet package
cd src/backend/AuthServer
dotnet add package PackageName

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run tests
dotnet test IdentitySolution.sln

# Watch for changes
dotnet watch run
```

## Troubleshooting Guide

### Authentication Issues

#### "401 Unauthorized" errors
1. Check token expiration:
```javascript
// In browser console
const token = localStorage.getItem('accessToken')
console.log(JSON.parse(atob(token.split('.')[1])))
```

2. Verify token refresh logic:
```bash
# Check refresh token endpoint
curl -X POST http://localhost:5002/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"your-refresh-token"}'
```

3. Check CORS configuration:
```bash
# Browser Network tab should show CORS headers
Access-Control-Allow-Origin: http://localhost:5173
Access-Control-Allow-Credentials: true
```

#### Database Connection Issues
```bash
# Test database connection
docker-compose exec postgres psql -U postgres -d AuthServer -c "SELECT 1;"

# Check database exists
docker-compose exec postgres psql -U postgres -c "\l"

# Recreate database
docker-compose exec postgres psql -U postgres -c "DROP DATABASE IF EXISTS \"AuthServer\";"
docker-compose exec postgres psql -U postgres -c "CREATE DATABASE \"AuthServer\";"
```

### Docker Issues

#### Port Already in Use
```bash
# Find process using port
lsof -i :5002
netstat -tulpn | grep :5002

# Kill process
kill -9 <PID>

# Or use different ports in docker-compose.yml
ports:
  - "5004:8080"  # Change 5002 to 5004
```

#### Container Build Failures
```bash
# Clean Docker cache
docker system prune -a
docker-compose down --rmi all -v

# Rebuild from scratch
docker-compose build --no-cache
docker-compose up --build
```

#### Volume Permission Issues (Linux/macOS)
```bash
# Fix volume permissions
sudo chown -R $USER:$USER ./src

# Or use bind mounts in docker-compose.yml
volumes:
  - ./src/backend/AuthServer:/app:delegated
```

### Frontend Issues

#### Build Failures
```bash
# Clear node_modules and reinstall
cd src/client
rm -rf node_modules pnpm-lock.yaml
pnpm install

# Clear Vite cache
rm -rf .vite
pnpm dev
```

#### Hot Reload Not Working
```bash
# Check Vite config for host binding
# vite.config.ts should have:
export default defineConfig({
  server: {
    host: true,  // Bind to all addresses
    port: 5173
  }
})
```

#### TypeScript Errors
```bash
# Restart TypeScript service in VS Code
Ctrl+Shift+P -> "TypeScript: Restart TS Server"

# Or check tsconfig.json paths
"baseUrl": ".",
"paths": {
  "@/*": ["./src/*"]
}
```

### Service Communication Issues

#### Services Can't Reach Each Other
1. Check Docker network:
```bash
docker network ls
docker network inspect identity_default
```

2. Test internal connectivity:
```bash
# From inside gateway container
docker-compose exec gateway curl http://authserver:8080/health
```

3. Verify environment variables:
```bash
# Check environment in running container
docker-compose exec gateway env | grep -i auth
```

#### YARP Routing Issues
Check Gateway configuration:
```json
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
```

### Performance Issues

#### Slow Database Queries
```sql
-- Enable query logging in PostgreSQL
ALTER SYSTEM SET log_statement = 'all';
SELECT pg_reload_conf();

-- View slow queries
SELECT query, mean_exec_time, calls 
FROM pg_stat_statements 
ORDER BY mean_exec_time DESC;
```

#### Frontend Bundle Size
```bash
# Analyze bundle
cd src/client
pnpm build
npx vite-bundle-analyzer dist

# Check for large dependencies
npx depcheck
```

## Development Tools and Extensions

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

## Testing and Quality Assurance

### Running Tests
```bash
# Backend tests
dotnet test IdentitySolution.sln --verbosity normal

# Frontend unit tests
cd src/client
pnpm test

# E2E tests
npx playwright test

# E2E tests with UI (for debugging)
npx playwright test --ui

# Specific test file
npx playwright test tests/auth.spec.ts
```

### Code Quality
```bash
# Format code
cd src/client
pnpm format

# Lint code
pnpm lint

# Type check
pnpm type-check

# .NET format
dotnet format IdentitySolution.sln
```

### Pre-commit Hooks
```bash
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
```

## Environment-Specific Configuration

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

## Debugging Tips

### Backend Debugging
```bash
# Attach debugger in VS Code
# Use launch.json configuration

# View detailed logs
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --verbosity detailed

# Database debugging
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information
```

### Frontend Debugging
```javascript
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
```

### Docker Debugging
```bash
# Access container shell
docker-compose exec gateway bash

# View container processes
docker-compose exec gateway ps aux

# Monitor resource usage
docker stats

# View container logs with details
docker-compose logs --details gateway
```