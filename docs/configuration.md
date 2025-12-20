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

### API Key

**Environment Variable:** `ApiKey__Key`

**Purpose:** Protects write operations (POST, PUT, PATCH, DELETE). Read operations (GET) are publicly accessible.

**Development:**
```bash
export ApiKey__Key="dev-chronicle-hub-key-12345"
```

**Production:**
```bash
# Generate strong key
export ApiKey__Key="$(openssl rand -base64 32)"
```

**Usage:** Include in request header as `X-Api-Key`:
```bash
curl -H "X-Api-Key: your-api-key-here" ...
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
export ApiKey__Key="dev-chronicle-hub-key-12345"
export Swagger__Enabled=true
export Urls="http://localhost:5000"

dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### Docker with SQLite

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/chroniclehub.db" \
  -e ApiKey__Key="prod-secure-key-xyz789" \
  -e Swagger__Enabled=false \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v chroniclehub-data:/data \
  chroniclehub-api
```

### Docker with PostgreSQL

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=chroniclehub;Username=app;Password=secret" \
  -e ApiKey__Key="prod-secure-key-xyz789" \
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
```

**secret.yaml:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chroniclehub-secret
type: Opaque
stringData:
  api-key: "prod-secure-key-xyz789"
  db-connection: "Host=postgres;Database=chroniclehub;Username=app;Password=secret"
```

**deployment.yaml:**
```yaml
env:
- name: ApiKey__Key
  valueFrom:
    secretKeyRef:
      name: chroniclehub-secret
      key: api-key
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

### API Keys

✅ **DO:**
- Generate cryptographically strong keys: `openssl rand -base64 32`
- Store in secrets management systems (Azure Key Vault, AWS Secrets Manager, etc.)
- Rotate regularly
- Use different keys per environment

❌ **DON'T:**
- Commit to version control
- Use weak or guessable keys
- Share across environments
- Log API keys

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

### "API Key is required" Error

**Problem:** Missing or invalid API key for write operations.

**Solution:**
```bash
# Ensure API key is set
echo $ApiKey__Key

# Include in request
curl -H "X-Api-Key: your-api-key" ...
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
