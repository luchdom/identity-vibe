# Docker Support for Identity Solution

This document provides instructions for running the Identity Solution using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose
- At least 4GB of available RAM
- At least 10GB of available disk space

## Quick Start

### 1. Development Environment

To run the entire system in development mode with hot reloading:

```bash
# Start all services with development configuration
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Or run in detached mode
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d
```

### 2. Production Environment

To run the system in production mode:

```bash
# Start all services with production configuration
docker-compose -f docker-compose.prod.yml up --build -d
```

### 3. Basic Development

To run with basic Docker Compose (no hot reloading):

```bash
docker-compose up --build
```

## Services

The Docker Compose setup includes the following services:

### 1. PostgreSQL Database
- **Image**: `postgres:15-alpine`
- **Port**: `5432`
- **Database**: `AuthServer`
- **Credentials**: 
  - Username: `postgres`
  - Password: `postgres` (configurable via environment variable)

### 2. AuthServer
- **Port**: `5000` (HTTP), `5001` (HTTPS)
- **Dependencies**: SQL Server
- **Features**: 
  - OpenIddict authentication server
  - ASP.NET Core Identity
  - User registration and management
  - Password reset functionality

### 3. Orders
- **Port**: `5002` (HTTP), `5003` (HTTPS)
- **Dependencies**: AuthServer
- **Features**:
  - Resource API with JWT validation
  - Dynamic authorization policies
  - Scope-based access control

## Environment Variables

### Database Configuration
- `POSTGRES_PASSWORD`: PostgreSQL password (default: `postgres`)

### Application Configuration
- `ASPNETCORE_ENVIRONMENT`: Environment setting (Development/Production)
- `ASPNETCORE_URLS`: Application URLs

## Port Mapping

| Service | Internal Port | External Port | Protocol |
|---------|---------------|---------------|----------|
| PostgreSQL | 5432 | 5432 | TCP |
| AuthServer | 80 | 5000 | HTTP |
| AuthServer | 443 | 5001 | HTTPS |
| Orders | 80 | 5002 | HTTP |
| Orders | 443 | 5003 | HTTPS |

## Development vs Production

### Development Mode
- Hot reloading enabled
- Source code mounted as volumes
- Development certificates
- Detailed logging
- Fast iteration cycle

### Production Mode
- Optimized builds
- No source code mounting
- Production certificates (configure separately)
- Optimized logging
- Better performance

## Docker Commands

### Building Images
```bash
# Build all services
docker-compose build

# Build specific service
docker-compose build authserver
docker-compose build servicea
```

### Running Services
```bash
# Start all services
docker-compose up

# Start in detached mode
docker-compose up -d

# Start specific service
docker-compose up authserver

# Start with specific configuration
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Stopping Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Stop and remove images
docker-compose down --rmi all
```

### Viewing Logs
```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs authserver
docker-compose logs servicea

# Follow logs in real-time
docker-compose logs -f
```

### Database Management
```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U postgres -d AuthServer

# Backup database
docker-compose exec postgres pg_dump -U postgres AuthServer > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres -d AuthServer < backup.sql
```

## Testing with Docker

### 1. Register a User
```bash
curl -X POST http://localhost:5000/api/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "confirmPassword": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 2. Get Access Token
```bash
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=user@example.com&password=Password123!&client_id=web-client&client_secret=secret&scope=orders.read orders.write"
```

### 3. Access Orders
```bash
curl -X GET http://localhost:5002/api/data \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Troubleshooting

### Common Issues

1. **Port Already in Use**
   ```bash
   # Check what's using the port
   netstat -ano | findstr :5000
   
   # Stop the conflicting service or change ports in docker-compose.yml
   ```

2. **Database Connection Issues**
   ```bash
   # Check if PostgreSQL is running
   docker-compose ps postgres
   
   # Check PostgreSQL logs
   docker-compose logs postgres
   ```

3. **Build Failures**
   ```bash
   # Clean Docker cache
   docker system prune -a
   
   # Rebuild without cache
   docker-compose build --no-cache
   ```

4. **Permission Issues (Linux/Mac)**
   ```bash
   # Fix volume permissions
   sudo chown -R $USER:$USER .
   ```

### Health Checks

```bash
# Check service health
docker-compose ps

# Check specific service health
docker-compose exec authserver curl -f http://localhost/health || exit 1
```

## Production Deployment

### 1. Environment Variables
Create a `.env` file for production:
```env
POSTGRES_PASSWORD=YourProductionPassword123!
ASPNETCORE_ENVIRONMENT=Production
```

### 2. SSL Certificates
For production, mount SSL certificates:
```yaml
volumes:
  - /path/to/ssl/certs:/https:ro
```

### 3. Database Backup
Set up automated database backups:
```bash
# Create backup script
docker-compose exec postgres pg_dump -U postgres AuthServer > "backup_$(date +%Y%m%d_%H%M%S).sql"
```

### 4. Monitoring
Add monitoring services to docker-compose.prod.yml:
```yaml
services:
  prometheus:
    image: prom/prometheus
    # ... configuration
  
  grafana:
    image: grafana/grafana
    # ... configuration
```

## Security Considerations

1. **Change Default Passwords**: Always change the default PostgreSQL password
2. **Use Environment Variables**: Store sensitive data in environment variables
3. **Network Security**: Use Docker networks to isolate services
4. **SSL/TLS**: Configure proper SSL certificates for production
5. **Regular Updates**: Keep Docker images updated
6. **Backup Strategy**: Implement regular database backups

## Performance Optimization

1. **Resource Limits**: Set appropriate CPU and memory limits
2. **Caching**: Use Redis for session caching
3. **Database Optimization**: Configure PostgreSQL for containerized environments
4. **Load Balancing**: Use nginx or similar for load balancing in production 