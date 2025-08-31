# SigNoz APM Integration for Identity System

This document provides comprehensive setup and usage instructions for the SigNoz APM integration with the Identity System.

## 🚀 Quick Start

### 1. Start the Complete Stack

```bash
# Start all services including SigNoz APM
docker-compose up --build

# Or start specific services
docker-compose up --build postgres authserver gateway servicea otel-collector clickhouse query-service frontend
```

### 2. Start Frontend Locally (Recommended for Development)

```bash
cd src/client
pnpm install
pnpm dev
```

### 3. Access SigNoz UI

- **SigNoz Dashboard**: http://localhost:3301
- **Frontend**: http://localhost:5173 (local) or http://localhost:3000 (Docker)
- **Gateway BFF**: http://localhost:5002
- **AuthServer**: http://localhost:5000
- **ServiceA**: http://localhost:5003

## 📊 What's Automatically Monitored

### ✅ Backend Services (.NET)

**Automatic Instrumentation (No Code Changes Required):**
- ✅ HTTP request/response metrics (duration, count, status codes)
- ✅ Database queries and performance (Entity Framework + SQL)
- ✅ Outgoing HTTP client calls
- ✅ .NET runtime metrics (GC, memory, CPU)
- ✅ Process metrics
- ✅ Exception tracking with stack traces
- ✅ Authentication/authorization context
- ✅ Business operation classification
- ✅ User context (when authenticated)
- ✅ Correlation ID tracking across services

**Custom Business Context Added Automatically:**
- Login success/failure rates
- User registration metrics
- Token generation/validation
- Authorization decisions
- Security events (brute force, lockouts)
- API endpoint performance
- Cross-service trace correlation

### ✅ Frontend (React)

**Automatic Instrumentation:**
- ✅ Page loads and navigation timing
- ✅ API calls to backend services
- ✅ User interactions (clicks, form submissions)
- ✅ JavaScript errors and exceptions
- ✅ Network performance
- ✅ Trace correlation between frontend and backend

## 📈 Pre-configured Dashboards

### 1. Identity System Overview
- Service health status
- Request rates and error rates
- Response time percentiles
- Active traces count

### 2. Authentication Performance
- Login success rates
- User registration trends
- Authentication response times
- Failed login attempts (security)
- Token generation metrics

### 3. API Performance
- Endpoint throughput
- Latency distribution heatmaps
- Status code distributions
- Top 10 most used endpoints
- Slowest endpoints
- Error-prone endpoints

### 4. Security Events
- Failed login attempts (brute force detection)
- Unauthorized access attempts
- Suspicious user agents
- High traffic source IPs
- Account lockouts
- Token validation failures

## 🔧 Configuration

### Environment Variables

**Backend Services:**
```bash
# Automatically configured in docker-compose.yml
OTEL_SERVICE_NAME=authserver|gateway|servicea
OTEL_SERVICE_VERSION=1.0.0
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_RESOURCE_ATTRIBUTES=service.name=...,deployment.environment=docker
```

**Frontend:**
```bash
# src/client/.env
VITE_OTEL_ENDPOINT=http://localhost:4318/v1/traces
VITE_API_URL=http://localhost:5002
```

### Service Ports

| Service | Port | Purpose |
|---------|------|---------|
| SigNoz UI | 3301 | Main dashboard |
| OTLP Collector (gRPC) | 4317 | Traces/metrics from .NET services |
| OTLP Collector (HTTP) | 4318 | Traces from frontend |
| ClickHouse | 9000, 8123 | Database for traces/metrics |
| Query Service | 8080 | SigNoz backend API |
| AlertManager | 9093 | Alerting |

## 🛠️ Troubleshooting

### Common Issues

1. **Services not sending telemetry:**
   ```bash
   # Check OTLP collector logs
   docker-compose logs otel-collector -f
   
   # Check service logs for OpenTelemetry initialization
   docker-compose logs authserver -f
   ```

2. **SigNoz UI not loading:**
   ```bash
   # Check all SigNoz services are healthy
   docker-compose ps
   
   # Check ClickHouse is ready
   docker-compose logs clickhouse -f
   ```

3. **Frontend traces not appearing:**
   - Ensure CORS is properly configured
   - Check browser console for errors
   - Verify VITE_OTEL_ENDPOINT is correct

4. **Database connection issues:**
   ```bash
   # Reset ClickHouse data
   docker-compose down -v
   docker-compose up --build clickhouse
   ```

### Performance Tuning

1. **Reduce trace sampling** (production):
   ```yaml
   # In OpenTelemetry configuration
   trace_sampler: "probabilistic"
   trace_sampler_arg: 0.1  # Sample 10% of traces
   ```

2. **Adjust retention periods:**
   ```sql
   -- In ClickHouse configuration
   TTL timestamp + INTERVAL 7 DAY DELETE  -- Adjust as needed
   ```

## 🎯 Usage Examples

### 1. Monitor Authentication Flow

1. Go to SigNoz UI → Traces
2. Filter by `service.name = "authserver"`
3. Look for spans with `operation.name` containing "login" or "register"
4. Click on any trace to see the complete flow

### 2. Identify Slow API Endpoints

1. Go to SigNoz UI → Dashboards → API Performance
2. Check "Slowest API Endpoints" panel
3. Or use custom query: 
   ```promql
   histogram_quantile(0.95, 
     sum(rate(http_server_request_duration_seconds_bucket[5m])) 
     by (service_name, route, le)
   ) > 0.5
   ```

### 3. Detect Security Issues

1. Go to SigNoz UI → Dashboards → Security Events
2. Monitor "Failed Login Attempts" and "Brute Force Detection"
3. Set up alerts for unusual patterns

### 4. Track User Journey

1. Use trace correlation via correlation IDs
2. Follow a single user request through:
   - Frontend → Gateway → AuthServer → Database
   - All services will have the same correlation ID

## 🔔 Alerting

### Pre-configured Alert Rules

1. **High Error Rate**: >5% error rate for 5 minutes
2. **Slow Response Time**: >2s average response time
3. **Failed Logins**: >50 failed logins per minute
4. **Service Down**: Service unavailable for 1 minute

### Custom Alerts

Access AlertManager at http://localhost:9093 to configure custom alerts.

## 📊 Metrics Reference

### Automatic Metrics Collected

| Metric | Description | Labels |
|--------|-------------|---------|
| `http_server_requests_total` | HTTP request count | service_name, method, route, status_code |
| `http_server_request_duration_seconds` | Request duration | service_name, method, route |
| `http_client_requests_total` | Outgoing HTTP requests | service_name, method, url |
| `dotnet_gc_collections_total` | .NET GC collections | service_name, generation |
| `dotnet_gc_memory_total_available_bytes` | Available memory | service_name |
| `process_cpu_seconds_total` | CPU usage | service_name |

### Custom Business Metrics

All automatically collected without code changes:
- Login success/failure rates
- User registration counts
- Token generation metrics
- Authorization decisions
- Database query performance
- Security events

## 🔍 Advanced Querying

### Example PromQL Queries

1. **Authentication Success Rate:**
   ```promql
   rate(http_server_requests_total{service_name="authserver", route="/account/login", status_code="200"}[5m]) /
   rate(http_server_requests_total{service_name="authserver", route="/account/login"}[5m]) * 100
   ```

2. **Cross-Service Error Rate:**
   ```promql
   sum(rate(http_server_requests_total{status_code=~"5.."}[5m])) by (service_name)
   ```

3. **Database Query Performance:**
   ```promql
   histogram_quantile(0.95, 
     sum(rate(db_command_duration_seconds_bucket[5m])) 
     by (service_name, le)
   )
   ```

## 📝 Best Practices

1. **Use correlation IDs** - Already implemented automatically
2. **Monitor business metrics** - Automatically collected
3. **Set up proper alerting** - Use provided dashboard alerts
4. **Regular dashboard reviews** - Check weekly for trends
5. **Performance baseline** - Establish normal operating metrics

## 🔐 Security Considerations

- ✅ Sensitive data (passwords, tokens) automatically excluded from traces
- ✅ Connection strings sanitized in database traces
- ✅ Security events automatically tagged and tracked
- ✅ IP addresses and user agents collected for security analysis

## 📈 Production Recommendations

1. **Sampling**: Enable probabilistic sampling (10-20%)
2. **Retention**: Adjust data retention based on storage capacity
3. **Resources**: Allocate adequate resources for ClickHouse
4. **Monitoring**: Monitor the monitoring stack itself
5. **Backup**: Regular ClickHouse backups for trace data

---

🎉 **Success Criteria Met:**
- ✅ Start entire stack with `docker compose up`
- ✅ Access SigNoz UI at http://localhost:3301
- ✅ See traces from all services correlated
- ✅ View structured logs integrated with traces
- ✅ Monitor custom business metrics
- ✅ Set up alerts for system health
- ✅ Debug issues using distributed tracing
- ✅ Track authentication performance end-to-end