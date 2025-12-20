# Authentication Guide

ChronicleHub uses API key authentication to secure write operations while keeping read operations publicly accessible.

## Authentication Model

### Public Read Access

**GET requests do not require authentication:**
- `GET /api/events`
- `GET /api/events/{id}`
- `GET /api/stats/daily/{date}`
- `GET /health/live`
- `GET /health/ready`

This allows analytics dashboards and public reporting without authentication overhead.

### Protected Write Operations

**Write operations require API key authentication:**
- `POST /api/events`
- `PUT /api/events/{id}` (if implemented)
- `PATCH /api/events/{id}` (if implemented)
- `DELETE /api/events/{id}` (if implemented)

## API Key Authentication

### Configuration

**Environment Variable:**
```bash
export ApiKey__Key="your-secret-api-key-here"
```

**Docker:**
```bash
docker run -e ApiKey__Key="your-secret-api-key-here" chroniclehub-api
```

**Kubernetes Secret:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chroniclehub-secret
type: Opaque
stringData:
  api-key: "your-secret-api-key-here"
```

### Usage

Include the API key in the `X-Api-Key` header:

```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-secret-api-key-here" \
  -d '{...}'
```

### Generating Secure API Keys

**Using OpenSSL:**
```bash
openssl rand -base64 32
# Output: 8xK9L2mN4pQ6rS8tU0vW2xY4zA6bC8dE0fG2hI4jK6l=
```

**Using PowerShell:**
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

**Using Python:**
```python
import secrets
import base64
api_key = base64.b64encode(secrets.token_bytes(32)).decode('utf-8')
print(api_key)
```

## Error Responses

### Missing API Key

**Request:**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{...}'
```

**Response:** `401 Unauthorized`
```json
{
  "type": "https://httpstatuses.io/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "API key is required for this operation.",
  "instance": "/api/events"
}
```

### Invalid API Key

**Request:**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "X-Api-Key: wrong-key" \
  -d '{...}'
```

**Response:** `401 Unauthorized`
```json
{
  "type": "https://httpstatuses.io/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid API key.",
  "instance": "/api/events"
}
```

## Security Best Practices

### ✅ DO

1. **Generate Strong Keys**
   ```bash
   # Use cryptographically secure random generation
   openssl rand -base64 32
   ```

2. **Store Securely**
   - Use secrets management (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
   - Never commit to version control
   - Use environment variables or secret stores

3. **Rotate Regularly**
   ```bash
   # Generate new key
   NEW_KEY=$(openssl rand -base64 32)

   # Update in secrets manager
   # Deploy new key
   # Invalidate old key after grace period
   ```

4. **Use Different Keys Per Environment**
   ```bash
   # Development
   export ApiKey__Key="dev-key-12345"

   # Staging
   export ApiKey__Key="$(openssl rand -base64 32)"

   # Production
   export ApiKey__Key="$(openssl rand -base64 32)"
   ```

5. **Use HTTPS in Production**
   - API keys transmitted over HTTP can be intercepted
   - Always use TLS/SSL in production

6. **Monitor API Key Usage**
   - Log authentication attempts
   - Alert on suspicious patterns
   - Track key usage by source

### ❌ DON'T

1. **Don't Use Weak Keys**
   ```bash
   # Bad examples:
   export ApiKey__Key="12345"
   export ApiKey__Key="admin"
   export ApiKey__Key="password"
   ```

2. **Don't Commit to Version Control**
   ```bash
   # Add to .gitignore:
   .env
   appsettings.Production.json
   secrets/
   ```

3. **Don't Share Keys Across Environments**
   ```bash
   # Wrong - same key everywhere
   export ApiKey__Key="shared-key-12345"
   ```

4. **Don't Log API Keys**
   ```csharp
   // Wrong
   _logger.LogInformation($"API Key: {apiKey}");

   // Right
   _logger.LogInformation("API key validated");
   ```

5. **Don't Hardcode**
   ```csharp
   // Wrong
   const string ApiKey = "hardcoded-key-12345";

   // Right
   var apiKey = configuration["ApiKey:Key"];
   ```

## Swagger UI Authentication

When using Swagger UI (`/swagger`):

1. Click the **Authorize** button (🔒 lock icon)
2. Enter your API key
3. Click **Authorize**
4. Click **Close**

All subsequent requests from Swagger will include the API key.

## Future Authentication Options

Currently under consideration for future versions:

### JWT Bearer Tokens

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Benefits:**
- Stateless authentication
- Includes user/tenant claims
- Standard OAuth 2.0 flow
- Token expiration

### Multi-Tenant Authentication

**Planned features:**
- Tenant-specific API keys
- User authentication within tenants
- Role-based access control (RBAC)
- Per-tenant rate limiting

### OAuth 2.0 / OpenID Connect

**Integration with:**
- Azure AD / Entra ID
- Auth0
- Okta
- Custom identity providers

## Implementation Details

The API key authentication is implemented in `ApiKeyAuthenticationMiddleware`:

**Location:** `src/ChronicleHub.Api/Middleware/ApiKeyAuthenticationMiddleware.cs`

**Exempt Endpoints:**
- `/health/*` - Health check endpoints
- `/swagger/*` - Swagger UI (when enabled)
- `GET` requests - Read-only operations

**Validation Flow:**
1. Check if request requires authentication (POST/PUT/PATCH/DELETE)
2. Extract `X-Api-Key` header
3. Compare with configured API key
4. Return 401 if missing or invalid
5. Continue pipeline if valid

## Troubleshooting

### "API key is required"

**Problem:** POST/PUT/PATCH/DELETE without API key

**Solution:**
```bash
# Add X-Api-Key header
curl -H "X-Api-Key: your-key" ...
```

### "Invalid API key"

**Problem:** API key doesn't match configured value

**Solution:**
```bash
# Check configured key
echo $ApiKey__Key

# Verify you're using the correct key
curl -H "X-Api-Key: $ApiKey__Key" ...
```

### Swagger requests fail with 401

**Problem:** API key not configured in Swagger UI

**Solution:**
1. Click Authorize button in Swagger
2. Enter API key
3. Click Authorize

### Different key for each environment

**Problem:** Same key being used across dev/staging/prod

**Solution:**
```bash
# Generate unique key per environment
DEV_KEY=$(openssl rand -base64 32)
STAGING_KEY=$(openssl rand -base64 32)
PROD_KEY=$(openssl rand -base64 32)

# Store in respective secrets managers
```

## Next Steps

- [API Endpoints](endpoints.md) - Complete API reference
- [Examples](examples.md) - Usage examples with authentication
- [Configuration Guide](../configuration.md) - API key configuration details
