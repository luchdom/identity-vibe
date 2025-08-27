# CLAUDE.md

This file provides essential guidance to Claude Code when working with this Identity System codebase.

## Project Overview

**.NET 8 OAuth2/OpenID Connect authentication system** with React frontend:

**Backend Services:**
- **AuthServer** (5000): OpenIddict OAuth2/OIDC server with ASP.NET Core Identity
- **Gateway** (5002): YARP-based BFF proxy with authentication handling  
- **ServiceA** (5003): Resource API with JWT validation and authorization

**Frontend:**
- **React App** (5173): TypeScript + Tailwind CSS + shadcn/ui components

## Quick Start Commands

### Recommended Development Setup (AI-Friendly)
```bash
# Backend services in Docker (consistent environment)
docker-compose up --build postgres authserver gateway servicea

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

## Architecture Essentials

### BFF Pattern (MANDATORY)
- **All external clients MUST access through Gateway BFF (port 5002)**
- **Direct AuthServer/ServiceA access is PROHIBITED**
- Gateway routes: `/auth/*` → AuthServer, `/data/*` → ServiceA

### Service URLs
```bash
# External (for frontend)
Frontend: http://localhost:5173
Gateway:  http://localhost:5002  # Main API endpoint

# Internal (Docker containers)
AuthServer: http://authserver:8080
ServiceA:   http://servicea:8080
```

### Default Login Credentials  
- **Email**: `admin@example.com`
- **Password**: `Admin123!`

## Key Coding Standards

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
│   └── ServiceA/            # Resource API
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
└── DEVELOPMENT.md          # Setup & troubleshooting
```

## Common Development Tasks

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
- **ServiceA/appsettings.json**: Authorization policies
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

## AI Debugging Recommendations

The current setup is optimized for AI assistance:
- **Backend in Docker**: Consistent, isolated environment
- **Frontend local**: Direct file access, hot reload, easy debugging
- **Port mapping**: Services accessible on localhost
- **Structured docs**: Specific guides for different development aspects

This configuration allows AI assistants to easily read/modify frontend code while ensuring backend consistency.