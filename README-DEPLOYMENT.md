# Environment-Specific Deployment Guide

This project supports multiple environments (development, staging, production) with environment-specific URL configuration.

## Environment Files

Copy the appropriate environment file to `.env` before deployment:

```bash
# Development
cp .env.development .env

# Staging
cp .env.staging .env
# Edit .env with your staging domain URLs

# Production
cp .env.production .env
# Edit .env with your production domain URLs
```

## URL Configuration

The system uses these environment variables:

- `EXTERNAL_AUTH_URL`: AuthServer URL accessible from browsers
- `EXTERNAL_GATEWAY_URL`: Gateway BFF URL accessible from browsers
- `EXTERNAL_CLIENT_URL`: Frontend app URL accessible from browsers
- `INTERNAL_*_URL`: Internal Docker network URLs (set automatically)

## Deployment Commands

### Development
```bash
# With hot reload and development settings
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

### Production
```bash
# With production optimizations
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up --build
```

## Environment Examples

### Development URLs
```bash
EXTERNAL_AUTH_URL=https://localhost:5000
EXTERNAL_GATEWAY_URL=http://localhost:5002
EXTERNAL_CLIENT_URL=http://localhost:5173
```

### Production URLs
```bash
EXTERNAL_AUTH_URL=https://auth.yourdomain.com
EXTERNAL_GATEWAY_URL=https://api.yourdomain.com
EXTERNAL_CLIENT_URL=https://app.yourdomain.com
```

## Client Configuration

OAuth client redirect URIs are automatically configured based on environment variables:

- Web Admin SPA: `${EXTERNAL_CLIENT_URL}/callback`
- Gateway BFF: `${EXTERNAL_GATEWAY_URL}/signin-oidc`
- Mobile App: `com.dummy.mobile://callback`

## SSL/TLS Configuration

For production, configure SSL certificates in your `.env`:

```bash
ASPNETCORE_Kestrel__Certificates__Default__Path=/path/to/cert.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=your-cert-password
```

## Database

Update `POSTGRES_PASSWORD` in your `.env` file for each environment.

## Troubleshooting

1. **Redirect URI Mismatch**: Check that `EXTERNAL_CLIENT_URL` matches your frontend domain
2. **CORS Issues**: Verify `EXTERNAL_GATEWAY_URL` is correctly set
3. **SSL Issues**: Ensure certificates are properly configured for HTTPS URLs