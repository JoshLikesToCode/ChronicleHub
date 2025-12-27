# Configuration Guide

ChronicleHub is designed to be cloud-native and fully configurable via environment variables. No hardcoded values - the same Docker image works in all environments.

## Configuration Philosophy

**Environment variables over configuration files** - Following 12-factor app principles, all configuration is externalized through environment variables, making the application portable across environments.

## Environment Variable Syntax

.NET Core uses double underscores (`__`) to represent nested JSON configuration keys:

```bash
# These are equivalent:
export ApiKey__Key="my-secret-key"
# vs JSON: { "ApiKey": { "Key": "my-secret-key" } }

export ConnectionStrings__DefaultConnection="Host=db;Database=chronicle"
# vs JSON: { "ConnectionStrings": { "DefaultConnection": "Host=db;Database=chronicle" } }
```

## Required Configuration

### Database Connection String

**Environment Variable:** `ConnectionStrings__DefaultConnection`

**SQLite (Development):**
```bash
export ConnectionStrings__DefaultConnection="Data Source=chroniclehub.db"
```

**PostgreSQL (Production):**
```bash
export ConnectionStrings__DefaultConnection="Host=postgres-server;Port=5432;Database=chroniclehub;Username=app;Password=secret;SSL Mode=Require"
```

The application automatically detects the database type:
- Contains `Host=` or `Server=` → PostgreSQL
- Otherwise → SQLite

### JWT Secret

**Environment Variable:** `Jwt__Secret`

**Purpose:** Signs and validates JWT access tokens for user authentication.

**Security:** MUST be cryptographically strong (minimum 32 characters, recommended 48+).

**Development:**
```bash
export Jwt__Secret="development-secret-key-minimum-32-chars-long-do-not-use-in-prod"
```

**Production:**
```bash
# Generate strong secret (recommended)
export Jwt__Secret="$(openssl rand -base64 48)"

# Example output: "xL9k2jP8vQ1mN4bT7wR6yU5zS3aD0fG8hJ2kL5nM9pQ1rT4vW7xZ0cE3gI6jK9"
```

**Additional JWT Configuration:**
```bash
# Token expiration (minutes)
export Jwt__ExpiresInMinutes=15             # Production: 15 minutes
export Jwt__ExpiresInMinutes=60             # Development: 60 minutes

# Refresh token lifetime (days)
export Jwt__RefreshTokenLifetimeDays=7      # Production: 7 days
export Jwt__RefreshTokenLifetimeDays=30     # Development: 30 days

# Issuer and Audience (optional, defaults to "ChronicleHub")
export Jwt__Issuer="ChronicleHub"
export Jwt__Audience="ChronicleHub"
```

**Kubernetes Secret Example:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chroniclehub-jwt
type: Opaque
stringData:
  jwt-secret: "xL9k2jP8vQ1mN4bT7wR6yU5zS3aD0fG8hJ2kL5nM9pQ1rT4vW7xZ0cE3gI6jK9"
---
# In deployment
env:
- name: Jwt__Secret
  valueFrom:
    secretKeyRef:
      name: chroniclehub-jwt
      key: jwt-secret
```

## Optional Configuration

### Swagger UI

**Environment Variable:** `Swagger__Enabled`

**Default:**
- `true` in Development
- `false` in Production

**Override:**
```bash
export Swagger__Enabled=true   # Enable
export Swagger__Enabled=false  # Disable
```

**Security Note:** Always disable Swagger in production to prevent API discovery.

### HTTP Server Binding

**Environment Variable:** `Urls`

**Default:**
- Development: `http://localhost:5000`
- Production: `http://0.0.0.0:8080`

**Override:**
```bash
export Urls="http://0.0.0.0:8080"        # Bind to all interfaces
export Urls="http://+:8080"              # Same as above
export Urls="http://localhost:5000"      # Localhost only
export Urls="http://+:8080;https://+:8443"  # Multiple bindings
```

**Note:** `ASPNETCORE_URLS` can also be used and will override `Urls` setting.

### Environment Name

**Environment Variable:** `ASPNETCORE_ENVIRONMENT`

**Values:** `Development`, `Staging`, `Production`

**Default:** `Production`

**Effects:**
- **Development**:
  - Detailed error messages with stack traces
  - Swagger enabled by default
  - Verbose logging
- **Production**:
  - Generic error messages (security)
  - Swagger disabled by default
  - Reduced logging

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_ENVIRONMENT=Production
```

### Service Name

**Environment Variable:** `ServiceName`

**Default:** `ChronicleHub`

**Purpose:** Used in logging and OpenTelemetry for service identification.

```bash
export ServiceName="ChronicleHub-Production-East"
```

## Logging Configuration

Logging is configured via Serilog in `appsettings.json`. Override via environment variables:

### Log Level

```bash
# Set minimum log level
export Serilog__MinimumLevel__Default=Information
export Serilog__MinimumLevel__Default=Debug
export Serilog__MinimumLevel__Default=Warning

# Override for specific namespaces
export Serilog__MinimumLevel__Override__Microsoft.AspNetCore=Warning
export Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore=Information
```

### Log Output

By default, logs go to console in JSON format (CompactJsonFormatter).

**Custom log file (requires configuration change):**
```bash
export Serilog__WriteTo__1__Name=File
export Serilog__WriteTo__1__Args__path="/app/logs/log.txt"
export Serilog__WriteTo__1__Args__rollingInterval=Day
```

## Complete Configuration Examples

### Local Development

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Data Source=chroniclehub.db"
export Jwt__Secret="development-secret-key-minimum-32-chars-long-do-not-use-in-prod"
export Jwt__ExpiresInMinutes=60
export Jwt__RefreshTokenLifetimeDays=30
export Swagger__Enabled=true
export Urls="http://localhost:5000"

dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### Docker with SQLite

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/chroniclehub.db" \
  -e Jwt__Secret="$(openssl rand -base64 48)" \
  -e Jwt__ExpiresInMinutes=15 \
  -e Jwt__RefreshTokenLifetimeDays=7 \
  -e Swagger__Enabled=false \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v chroniclehub-data:/data \
  chroniclehub-api
```

### Docker with PostgreSQL

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=chroniclehub;Username=app;Password=secret" \
  -e Jwt__Secret="$(openssl rand -base64 48)" \
  -e Jwt__ExpiresInMinutes=15 \
  -e Jwt__RefreshTokenLifetimeDays=7 \
  -e Swagger__Enabled=false \
  -e ASPNETCORE_ENVIRONMENT=Production \
  chroniclehub-api
```

### Kubernetes ConfigMap + Secret

**configmap.yaml:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: chroniclehub-config
data:
  SWAGGER_ENABLED: "false"
  SERVICE_NAME: "ChronicleHub-Production"
  JWT_EXPIRES_IN_MINUTES: "15"
  JWT_REFRESH_TOKEN_LIFETIME_DAYS: "7"
```

**secret.yaml:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chroniclehub-secret
type: Opaque
stringData:
  jwt-secret: "xL9k2jP8vQ1mN4bT7wR6yU5zS3aD0fG8hJ2kL5nM9pQ1rT4vW7xZ0cE3gI6jK9"
  db-connection: "Host=postgres;Database=chroniclehub;Username=app;Password=secret"
```

**deployment.yaml:**
```yaml
env:
- name: Jwt__Secret
  valueFrom:
    secretKeyRef:
      name: chroniclehub-secret
      key: jwt-secret
- name: Jwt__ExpiresInMinutes
  valueFrom:
    configMapKeyRef:
      name: chroniclehub-config
      key: JWT_EXPIRES_IN_MINUTES
- name: Jwt__RefreshTokenLifetimeDays
  valueFrom:
    configMapKeyRef:
      name: chroniclehub-config
      key: JWT_REFRESH_TOKEN_LIFETIME_DAYS
- name: ConnectionStrings__DefaultConnection
  valueFrom:
    secretKeyRef:
      name: chroniclehub-secret
      key: db-connection
- name: Swagger__Enabled
  valueFrom:
    configMapKeyRef:
      name: chroniclehub-config
      key: SWAGGER_ENABLED
- name: ServiceName
  valueFrom:
    configMapKeyRef:
      name: chroniclehub-config
      key: SERVICE_NAME
```

## Configuration Files

While environment variables are preferred, configuration can also come from JSON files:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides (not included by default)

**Priority (highest to lowest):**
1. Environment variables
2. `appsettings.{Environment}.json`
3. `appsettings.json`

## Security Best Practices

### JWT Secrets

✅ **DO:**
- Generate cryptographically strong secrets: `openssl rand -base64 48`
- Minimum 32 characters, recommend 48+ characters
- Store in secrets management systems (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
- Use different secrets per environment
- Rotate JWT secrets periodically (requires user re-authentication)
- Use HTTPS/TLS in production to protect tokens in transit

❌ **DON'T:**
- Commit JWT secrets to version control
- Use weak or guessable secrets like "secret", "password", "12345"
- Share JWT secrets across environments
- Log JWT secrets or tokens
- Store tokens in localStorage (vulnerable to XSS)

### API Keys (Tenant-Scoped)

✅ **DO:**
- Create unique API keys per tenant and service
- Use the built-in key generation with "ch_live_" prefix
- Store API key hashes (SHA256) in database, never plaintext
- Set expiration dates on API keys
- Revoke compromised keys immediately
- Track API key usage via LastUsedAtUtc

❌ **DON'T:**
- Share API keys across tenants
- Store plaintext API keys
- Log API keys in application logs

### Database Credentials

✅ **DO:**
- Use managed database authentication (IAM, Azure AD, etc.) when possible
- Store connection strings in secrets management
- Use strong passwords
- Enable SSL/TLS connections
- Limit database user permissions (principle of least privilege)

❌ **DON'T:**
- Use default credentials
- Grant superuser access to application
- Commit connection strings to version control

### Swagger in Production

✅ **DO:**
- Disable Swagger in production (`Swagger__Enabled=false`)
- Use API documentation tools like Postman or Redoc for external docs
- Implement authentication if Swagger is needed in non-production environments

❌ **DON'T:**
- Expose Swagger publicly in production
- Include sensitive information in Swagger descriptions

## Troubleshooting

### Authentication Errors

**Problem:** 401 Unauthorized on JWT-protected endpoints

**Solution:**
```bash
# Verify JWT token is set
echo $JWT_TOKEN

# Include Bearer token in request
curl -H "Authorization: Bearer $JWT_TOKEN" /api/events

# If token expired, refresh or re-login
curl -X POST /api/auth/refresh  # with refresh token cookie
```

**Problem:** 403 Forbidden on API key endpoints

**Solution:**
```bash
# Verify using API key, not JWT, for event creation
curl -H "X-Api-Key: ch_live_..." -X POST /api/events

# JWT tokens cannot be used for POST /api/events
```

### Database Connection Errors

**Problem:** Cannot connect to database.

**Solutions:**
```bash
# Check connection string
echo $ConnectionStrings__DefaultConnection

# Verify database is accessible
ping postgres-server
telnet postgres-server 5432

# Check database user permissions
psql -h postgres-server -U app -d chroniclehub
```

### Swagger Not Available

**Problem:** Swagger UI returns 404.

**Solutions:**
```bash
# Enable Swagger
export Swagger__Enabled=true

# Verify environment
echo $ASPNETCORE_ENVIRONMENT  # Should be Development

# Restart application
```

### Configuration Not Applied

**Problem:** Environment variables not taking effect.

**Solutions:**
1. Verify variable is exported (not just set)
   ```bash
   export ApiKey__Key="value"  # ✅ Exported
   ApiKey__Key="value"         # ❌ Not exported
   ```

2. Check for typos in variable names (case-sensitive)
   ```bash
   ApiKey__Key    # ✅ Correct
   APIKEY__KEY    # ❌ Wrong case
   ApiKey_Key     # ❌ Wrong separator
   ```

3. Restart application after changing environment variables

4. In Docker, ensure `-e` flag is before image name:
   ```bash
   docker run -e VAR=value chroniclehub-api  # ✅ Correct
   docker run chroniclehub-api -e VAR=value  # ❌ Wrong
   ```

## Next Steps

- [Docker Deployment](deployment/docker.md) - Run with Docker
- [Kubernetes Deployment](deployment/kubernetes.md) - Deploy to Kubernetes
- [API Documentation](api/endpoints.md) - API reference
