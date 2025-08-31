# Enterprise Serilog Implementation

## Overview

This document outlines the enterprise-grade Serilog logging implementation for the Identity System. The implementation provides structured logging, correlation tracking, and HTTP request/response monitoring across all services using built-in Serilog capabilities and middleware.

## Architecture

### Core Components

1. **Correlation ID System** - Tracks requests across service boundaries
2. **Request/Response Logging** - HTTP traffic monitoring with filtering  
3. **Structured JSON Logging** - Consistent log format for parsing
4. **Multi-destination Logging** - Console + File outputs with rotation
5. **Built-in Serilog Features** - Leverages Serilog's native request logging

### Service Structure

```
src/backend/
├── Shared/Logging/           # Shared logging components
│   ├── Services/             # Correlation ID service
│   ├── Middleware/           # HTTP middleware components
│   └── Extensions/           # Configuration extensions
├── AuthServer/               # OAuth2/OIDC authentication server
├── Gateway/                  # BFF (Backend for Frontend) proxy
└── ServiceA/                 # Protected resource API
```

## Implementation Details

### 1. Correlation ID System

**Purpose**: Track individual requests across all services for debugging and monitoring.

**Components**:
- `ICorrelationIdService` - Interface for correlation ID management
- `CorrelationIdService` - Implementation with HTTP context integration
- `CorrelationIdMiddleware` - Automatic correlation ID generation/propagation

**Features**:
- Automatic generation of UUID-based correlation IDs
- Header-based propagation (`X-Correlation-ID`)
- Context enrichment for all log entries
- Frontend integration with sessionStorage

### 2. Request/Response Logging

**Purpose**: Monitor HTTP traffic using custom middleware and built-in Serilog request logging.

**Built-in Serilog Request Logging**:
- Leverages `UseSerilogRequestLogging()` for automatic HTTP request logging
- Enriches logs with request host, scheme, remote IP, and user agent
- Provides standardized request/response timing and status codes

**Custom Middleware Features**:
- Configurable path exclusions (health checks, static files)
- Optional request/response body logging with size limits
- Performance monitoring with slow request detection
- Sensitive header filtering (Authorization, Cookie, etc.)
- Response status code categorization

**Configuration**:
```json
{
  "RequestResponseLogging": {
    "LogRequestBody": false,
    "LogResponseBody": false,
    "MaxBodySizeToLog": 4096,
    "SlowRequestThresholdMs": 5000
  }
}
```

### 3. Structured Logging Configuration

**Log Levels by Environment**:
- **Development**: Debug level with detailed Microsoft logs
- **Production**: Information level with Warning+ for Microsoft components

**Log Destinations**:
- **Console**: JSON formatted for container logs
- **File**: Daily rotating files with 30-day retention, 100MB size limit

**Enrichment**:
- Environment name
- Machine name
- Process ID and Thread ID
- Correlation ID (via middleware)

## Service-Specific Features

### AuthServer (Port 5000)
- **Standard ASP.NET Core Logging**: Authentication and user management events
- **Entity Framework Logging**: Database operations (configurable level)
- **OpenIddict Integration**: OAuth2/OpenID Connect flows via framework logs

### Gateway (Port 5002)
- **YARP Logging**: Reverse proxy operations and routing
- **Authentication Logging**: JWT validation results
- **Health Check Logging**: Service availability monitoring
- **CORS Logging**: Cross-origin request handling

### ServiceA (Port 5003)
- **Authorization Logging**: Policy-based access control decisions
- **API Logging**: Endpoint access and business logic operations
- **Performance Logging**: Request timing and resource usage

## Frontend Integration

### Correlation ID Support
- Automatic generation using `crypto.randomUUID()`
- SessionStorage persistence across page loads
- HTTP header propagation (`X-Correlation-ID`)
- Development console logging

### Error Handling Enhancement
- Correlation ID display in error messages (development only)
- Enhanced error tracking for support scenarios
- API request/response logging in browser console

**Example Usage**:
```typescript
import { correlationId } from './lib/api';

// Generate new correlation ID
const newId = correlationId.generate();

// Get current correlation ID
const currentId = correlationId.getCurrent();

// Clear correlation ID (e.g., on logout)
correlationId.clear();
```

## Configuration Examples

### Production Configuration
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning",
        "Yarp": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/service-.json",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 104857600,
          "rollOnFileSizeLimit": true,
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithEnvironmentName", "WithMachineName", "WithProcessId", "WithThreadId", "WithCorrelationId"]
  }
}
```

### Development Configuration
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore": "Information",
        "System": "Warning"
      }
    }
  },
  "RequestResponseLogging": {
    "LogRequestBody": true,
    "LogResponseBody": true,
    "MaxBodySizeToLog": 8192,
    "SlowRequestThresholdMs": 2000
  }
}
```

## Log Output Examples

### Built-in Serilog Request Log
```json
{
  "@t": "2024-01-15T10:30:01.000Z",
  "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
  "@l": "Information",
  "RequestMethod": "POST",
  "RequestPath": "/account/login",
  "StatusCode": 200,
  "Elapsed": 156.7822,
  "RequestHost": "localhost:5000",
  "RequestScheme": "http",
  "RemoteIpAddress": "127.0.0.1",
  "UserAgent": "Mozilla/5.0...",
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware"
}
```

### Application Log with Correlation ID
```json
{
  "@t": "2024-01-15T10:30:00.000Z",
  "@mt": "User logged in: {Email}",
  "@l": "Information",
  "Email": "user@example.com",
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "SourceContext": "AuthServer.Controllers.AccountController",
  "EnvironmentName": "Development",
  "MachineName": "DEV-MACHINE",
  "ProcessId": 1234,
  "ThreadId": 5
}
```

### Custom Request/Response Log
```json
{
  "@t": "2024-01-15T10:30:01.200Z",
  "@mt": "HTTP Response: {StatusCode} for {Method} {Path} | Duration: {ElapsedMilliseconds}ms",
  "@l": "Information",
  "StatusCode": 200,
  "Method": "POST",
  "Path": "/account/login",
  "ElapsedMilliseconds": 156,
  "ContentLength": 45,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "SourceContext": "Shared.Logging.Middleware.RequestResponseLoggingMiddleware"
}
```

## Security Considerations

### Data Protection
- **No Sensitive Data Logging**: Passwords, tokens, and PII are never logged
- **Header Filtering**: Authorization and authentication headers are redacted
- **Body Logging**: Disabled by default, configurable for development
- **Standard Framework Logging**: Relies on ASP.NET Core's built-in security practices

### Log Security
- **Structured Format**: Prevents log injection attacks
- **Configurable Levels**: Reduce verbose logging in production
- **Retention Policies**: Automatic log cleanup to manage storage
- **Access Control**: Log files should have restricted file permissions

## Performance Considerations

### Optimizations
- **Asynchronous Logging**: Non-blocking log operations via Serilog
- **Built-in Efficiency**: Leverages Serilog's optimized request logging
- **Configurable Filtering**: Skip logging for health checks and static files
- **Size Limits**: Request/response body logging with configurable limits
- **Background Processing**: Log writing doesn't impact request processing

### Monitoring
- **Slow Request Detection**: Configurable threshold for performance alerts
- **Memory Usage**: Process-level metrics in log enrichment
- **File Rotation**: Prevents disk space issues with automatic cleanup

## Troubleshooting

### Common Issues

1. **Missing Correlation IDs**: Ensure middleware is registered before authentication
2. **Large Log Files**: Check retention and size limit configuration
3. **Performance Impact**: Disable body logging in production
4. **Duplicate Logs**: Verify both built-in and custom middleware aren't conflicting

### Log Analysis

Use structured log queries to analyze issues:
```json
# Find all requests for a specific correlation ID
{ "CorrelationId": "550e8400-e29b-41d4-a716-446655440000" }

# Find all authentication events
{ "SourceContext": "*AccountController" }

# Find slow requests
{ "@mt": "*Slow Request*" }

# Find HTTP requests by status code
{ "StatusCode": 500 }
```

## Architecture Benefits

### Simplified Approach
- **Built-in Features**: Leverages Serilog's native request logging capabilities
- **Minimal Custom Code**: Focus on correlation ID and specific filtering needs
- **Framework Integration**: Works seamlessly with ASP.NET Core logging
- **Standard Patterns**: Uses established logging practices and conventions

### Maintainability
- **Fewer Dependencies**: Reduced custom logging services to maintain
- **Framework Updates**: Benefits from Serilog and ASP.NET Core improvements
- **Clear Separation**: Built-in logs vs. application-specific logs
- **Standard Configuration**: Uses conventional Serilog configuration patterns

## Future Enhancements

### Planned Features
- **OpenTelemetry Integration**: Distributed tracing support
- **External Log Sinks**: ELK Stack, Splunk, or Azure Monitor
- **Metrics Collection**: Application performance metrics via built-in features
- **Health Check Integration**: Standardized health monitoring

### Configuration Management
- **Environment Variables**: Override configuration for containerized deployments
- **Dynamic Configuration**: Runtime log level adjustments via Serilog
- **Feature Flags**: Toggle logging features without deployment

## Conclusion

This simplified enterprise-grade logging implementation provides:
- **Complete Request Traceability** through correlation IDs
- **Built-in HTTP Logging** using Serilog's optimized middleware
- **Performance Insights** through request/response timing
- **Structured Data** for easy parsing and analysis
- **Production-Ready Configuration** with proper filtering and rotation
- **Development-Friendly Features** with enhanced debugging capabilities
- **Minimal Maintenance Overhead** by leveraging framework capabilities

The implementation follows .NET 8 best practices, uses modern C# patterns, and provides a solid foundation for monitoring and debugging the Identity System while maintaining simplicity and leveraging proven Serilog features.