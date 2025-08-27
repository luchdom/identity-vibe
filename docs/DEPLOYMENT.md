# Deployment and Environment Guide

This document covers Docker configurations, environments, and deployment strategies.

## Docker Configurations

### Development Setup (Recommended for AI Debugging)
```bash
# Backend services in Docker, frontend local
docker-compose up --build postgres authserver gateway servicea

# Frontend runs locally for easier debugging
cd src/client && pnpm dev
```

**Benefits of this approach:**
- **Backend Consistency**: Isolated, reproducible environment
- **Frontend Flexibility**: Hot reload, easy file access for AI
- **Port Mapping**: Services accessible on localhost
- **Easy Debugging**: AI can read/modify frontend code directly

### Full Docker Development
```bash
# All services including frontend in Docker
docker-compose up --build
```

### Production Docker
```bash
# Production optimized build
docker-compose -f docker-compose.prod.yml up --build -d
```

## Port Mapping

### Docker Services
- **PostgreSQL**: `5432` (internal) -> `localhost:5432` (host)
- **AuthServer**: `8080` (internal) -> `localhost:5000` (host)
- **Gateway**: `8080` (internal) -> `localhost:5002` (host)
- **ServiceA**: `8080` (internal) -> `localhost:5003` (host)
- **Client** (when in Docker): `80` (internal) -> `localhost:3000` (host)

### Local Development
- **Frontend** (pnpm dev): `localhost:5173`
- **All backend services**: Access via Docker port mappings above

## Environment Variables

### AuthServer Environment Variables
```bash
# Database
ConnectionStrings__DefaultConnection="Host=postgres;Database=AuthServer;Username=postgres;Password=postgres"

# JWT Configuration  
Jwt__Issuer="https://localhost:5000"
Jwt__Audience="api"
Jwt__SecretKey="your-secret-key-min-32-chars"

# OpenIddict
OpenIddict__SigningKey="development-signing-key"
OpenIddict__EncryptionKey="development-encryption-key"
```

### Gateway Environment Variables
```bash
# Authentication
Auth__Authority="http://authserver:8080"
Auth__RequireHttpsMetadata="false"

# CORS
Cors__AllowedOrigins__0="http://localhost:5173"
Cors__AllowedOrigins__1="http://localhost:3000"

# YARP Configuration
ReverseProxy__Clusters__authserver__Destinations__destination1__Address="http://authserver:8080/"
ReverseProxy__Clusters__servicea__Destinations__destination1__Address="http://servicea:8080/"
```

### ServiceA Environment Variables
```bash
# Authentication
Auth__AuthenticationProviders__AuthServer__Authority="http://authserver:8080"
Auth__AuthenticationProviders__AuthServer__RequireHttpsMetadata="false"
```

### Frontend Environment Variables
```bash
# API Configuration
VITE_API_URL="http://localhost:5002"
VITE_APP_VERSION="1.0.0"
VITE_ENABLE_DEV_TOOLS="true"
```

## Docker Compose Files

### docker-compose.yml (Base)
- Defines all services with development configurations
- Uses development environment variables
- Enables hot reloading where possible

### docker-compose.dev.yml (Development Override)
```yaml
# Additional development-specific configurations
services:
  authserver:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./src/backend/AuthServer:/app
  
  gateway:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./src/backend/Gateway:/app
```

### docker-compose.prod.yml (Production)
```yaml
# Production optimizations
services:
  authserver:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__SecretKey=${JWT_SECRET_KEY}
    restart: unless-stopped
  
  gateway:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

## Database Management

### PostgreSQL in Docker
```bash
# Connect to database
docker-compose exec postgres psql -U postgres -d AuthServer

# Backup database
docker-compose exec postgres pg_dump -U postgres AuthServer > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres AuthServer < backup.sql

# Reset database (removes all data)
docker-compose down -v
docker-compose up --build postgres
```

### Entity Framework Migrations
```bash
# Apply migrations (run from AuthServer directory)
cd src/backend/AuthServer
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Reset database with migrations
dotnet ef database drop
dotnet ef database update
```

## Container Management

### Useful Commands
```bash
# View running containers
docker-compose ps

# View logs for specific service
docker-compose logs authserver
docker-compose logs gateway -f  # follow logs

# Restart specific service
docker-compose restart gateway

# Rebuild and restart specific service
docker-compose up --build gateway

# Clean reset (removes volumes and data)
docker-compose down -v
docker-compose up --build

# Remove all containers and images
docker-compose down --rmi all -v
```

### Troubleshooting

#### Port Conflicts
```bash
# Check what's using a port
netstat -tulpn | grep :5002
lsof -i :5002

# Kill process using port
kill -9 $(lsof -t -i:5002)
```

#### Container Health Checks
```bash
# Check container health
docker-compose ps
docker inspect identity-gateway-1

# View container resources
docker stats

# Execute commands in running container
docker-compose exec gateway bash
docker-compose exec postgres psql -U postgres
```

## Security Considerations

### Development vs Production

**Development (Relaxed Security):**
- HTTP allowed for internal communication
- Development signing keys
- Relaxed CORS policies
- Detailed error messages
- Debug logging enabled

**Production (Enhanced Security):**
- HTTPS required for all communication
- Strong, unique signing/encryption keys
- Restrictive CORS policies
- Generic error messages
- Structured logging only

### Key Management
```bash
# Generate secure keys for production
openssl rand -base64 32  # JWT Secret Key
openssl rand -base64 32  # OpenIddict Signing Key
openssl rand -base64 32  # OpenIddict Encryption Key
```

### Environment-Specific Configurations

#### Development
- Uses development certificates
- Database auto-creation enabled
- Detailed error pages
- Hot reloading enabled

#### Docker
- Container-to-container communication
- Internal DNS resolution (authserver, gateway, servicea)
- Volume mounts for development
- Network isolation

#### Production
- Production certificates required
- Database migrations must be run manually
- Error logging to external services
- Health checks enabled
- Resource limits configured

## Monitoring and Logging

### Container Logs
```bash
# View all service logs
docker-compose logs

# Follow logs for specific service
docker-compose logs -f gateway

# View logs with timestamps
docker-compose logs -t authserver
```

### Application Metrics
- Health check endpoints: `/health`
- Ready check endpoints: `/health/ready`
- Metrics endpoints: `/metrics` (if configured)

### Database Monitoring
```sql
-- Monitor active connections
SELECT count(*) FROM pg_stat_activity;

-- View current queries
SELECT pid, now() - pg_stat_activity.query_start AS duration, query 
FROM pg_stat_activity 
WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes';
```

## Backup and Recovery

### Database Backup Strategy
```bash
# Automated backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker-compose exec postgres pg_dump -U postgres AuthServer > backups/backup_$DATE.sql

# Keep only last 7 days of backups
find backups/ -name "backup_*.sql" -mtime +7 -delete
```

### Configuration Backup
- Backup `appsettings.json` files
- Backup `docker-compose.yml` configurations
- Backup SSL certificates
- Version control all configuration files

## Performance Optimization

### Docker Performance
```yaml
# Production optimizations in docker-compose.prod.yml
services:
  authserver:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
    restart: unless-stopped
```

### Database Performance
```sql
-- Optimize PostgreSQL for production
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET maintenance_work_mem = '64MB';
```