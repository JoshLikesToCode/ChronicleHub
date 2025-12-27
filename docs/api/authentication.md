# Authentication Guide

ChronicleHub implements a **dual authentication system** combining JWT Bearer tokens for user interactions and API Key authentication for service-to-service event ingestion.

## Table of Contents

- [Authentication Model](#authentication-model)
- [JWT Bearer Authentication](#jwt-bearer-authentication)
- [API Key Authentication](#api-key-authentication)
- [Multi-Tenancy](#multi-tenancy)
- [Authorization Policies](#authorization-policies)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)

## Authentication Model

### JWT Bearer Tokens (User/Interactive Endpoints)

**Used for:**
- Authentication endpoints (`/api/auth/*`)
- Querying events (`GET /api/events`, `GET /api/events/{id}`)
- Statistics (`GET /api/stats/*`)
- Admin operations
- UI-backed features

**Authentication Flow:**
1. Register user account: `POST /api/auth/register`
2. Login to get access token: `POST /api/auth/login`
3. Use access token in `Authorization: Bearer {token}` header
4. Refresh token when expired: `POST /api/auth/refresh`

### API Key Authentication (Service-to-Service)

**Used for:**
- Event ingestion (`POST /api/events`)
- Automated data collection
- External system integrations
- Webhooks

**Authentication Flow:**
1. Create API key for tenant (via admin API or direct database)
2. Include API key in `X-Api-Key` header
3. Events automatically tagged with tenant ID

### Public Endpoints (No Authentication Required)

**Health checks:**
- `GET /health/live`
- `GET /health/ready`

## JWT Bearer Authentication

### Registration

Create a new user account and tenant:

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123",
    "firstName": "John",
    "lastName": "Doe",
    "tenantName": "Acme Corporation"
  }'
```

**Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": null,
  "expiresAt": "2025-12-27T22:00:00Z",
  "user": {
    "id": "user-guid-here",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  },
  "tenant": {
    "id": "tenant-guid-here",
    "name": "Acme Corporation",
    "slug": "acme-corporation",
    "role": "Owner"
  }
}
```

**Notes:**
- Refresh token is returned as HttpOnly, Secure, SameSite=Strict cookie
- User automatically becomes Owner of the new tenant
- Password requirements: min 8 characters, uppercase, lowercase, digit

### Login

Authenticate with existing credentials:

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123",
    "tenantId": null
  }'
```

**Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-12-27T22:00:00Z",
  "user": { ... },
  "tenant": { ... }
}
```

**Notes:**
- If `tenantId` is null, user's first tenant is selected
- If user belongs to multiple tenants, specify `tenantId` to select which one
- Refresh token stored in HttpOnly cookie

### Using Access Tokens

Include the access token in the Authorization header:

```bash
curl -X GET http://localhost:5000/api/events \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Access Token Claims:**
- `sub` - User ID
- `email` - User email
- `tid` - Tenant ID (for tenant isolation)
- `role` - User's role in the tenant (Owner, Admin, Member)
- `jti` - Unique token identifier

**Token Expiration:**
- **Production**: 15 minutes
- **Development**: 60 minutes

### Refreshing Tokens

When access token expires, refresh it:

```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Cookie: refreshToken=..."
```

**Response:**
```json
{
  "success": true,
  "accessToken": "new-access-token-here",
  "expiresAt": "2025-12-27T22:15:00Z"
}
```

**Notes:**
- Refresh token is automatically sent via cookie
- Old refresh token is revoked (token rotation)
- New refresh token issued as cookie
- Refresh token lifetime: 7 days (production) / 30 days (development)

### Logout

Revoke refresh token:

```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Cookie: refreshToken=..."
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully logged out"
}
```

**Notes:**
- Refresh token is revoked in database
- Cookie is deleted
- Access token remains valid until expiration (short-lived by design)

## API Key Authentication

### Creating API Keys

API keys are tenant-scoped and must be created programmatically:

**C# Example:**
```csharp
var apiKeyService = serviceProvider.GetRequiredService<IApiKeyService>();
var (apiKey, plaintextKey) = await apiKeyService.CreateApiKeyAsync(
    tenantId: tenantGuid,
    name: "Production API Key",
    expiresAt: DateTime.UtcNow.AddYears(1)
);

Console.WriteLine($"API Key: {plaintextKey}");
// Store plaintextKey securely - it won't be retrievable later
```

**Key Format:**
```
ch_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0
```

**Properties:**
- **Prefix**: `ch_live_` for easy identification
- **Hash**: SHA256 hash stored in database (never plaintext)
- **Expiration**: Optional expiration date
- **Usage Tracking**: Last used timestamp recorded
- **Revocation**: Can be revoked at any time

### Using API Keys

Include API key in `X-Api-Key` header:

```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: ch_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0" \
  -d '{
    "type": "user_action",
    "source": "mobile-app",
    "timestampUtc": "2025-12-27T20:00:00Z",
    "payload": {
      "action": "button_click",
      "button_id": "signup"
    }
  }'
```

**Notes:**
- API keys can ONLY be used for `POST /api/events`
- Events are automatically tagged with the API key's tenant ID
- User ID is set to `Guid.Empty` (service account pattern)
- API keys cannot query events or access other endpoints

### Revoking API Keys

```csharp
await apiKeyService.RevokeApiKeyAsync(apiKeyId);
```

**Effects:**
- Key is marked as inactive
- Revocation timestamp recorded
- Future requests with this key return 401 Unauthorized

## Multi-Tenancy

### Tenant Isolation

All data is automatically isolated by tenant:

**Database Level:**
- Global query filters on `ActivityEvent`, `DailyStats`, `CategoryStats`
- Filters automatically apply `WHERE TenantId = @currentTenantId`

**Application Level:**
- `TenantResolutionMiddleware` extracts tenant ID from JWT claims
- Sets tenant context on `DbContext` for the request
- All queries automatically filtered

**Benefits:**
- Prevents cross-tenant data leaks
- No manual WHERE clauses needed
- Defense-in-depth security

### User-Tenant Membership

Users can belong to multiple tenants with different roles:

**Roles:**
- **Owner** - Full control, can manage users and settings
- **Admin** - Manage data and users
- **Member** - Read and create events

**Example:**
```
User: alice@example.com
├─ Tenant: Acme Corp (Role: Owner)
├─ Tenant: Beta Inc (Role: Admin)
└─ Tenant: Gamma LLC (Role: Member)
```

**Switching Tenants:**
```bash
# Login specifying tenant ID
curl -X POST /api/auth/login -d '{
  "email": "alice@example.com",
  "password": "SecurePass123",
  "tenantId": "beta-inc-tenant-guid"
}'
```

## Authorization Policies

### Available Policies

1. **RequireAuthentication**
   - Requires valid JWT token
   - No specific role or tenant required

2. **RequireTenantMembership**
   - Requires valid JWT with tenant ID claim
   - User must be a member of the tenant
   - Used on: `GET /api/events`, `GET /api/stats/*`

3. **RequireAdminRole**
   - Requires Admin or Owner role
   - Used for user management endpoints

4. **RequireOwnerRole**
   - Requires Owner role
   - Used for tenant management endpoints

5. **ApiKeyOnly**
   - Requires API key authentication
   - JWT tokens not accepted
   - Used on: `POST /api/events`

### Policy Application

**Controller Level:**
```csharp
[Authorize(Policy = AuthPolicies.RequireTenantMembership)]
public class StatsController : ControllerBase { ... }
```

**Endpoint Level:**
```csharp
[HttpPost]
[Authorize(AuthenticationSchemes = "ApiKey")]
public async Task<IActionResult> CreateEvent(...) { ... }
```

## Configuration

### JWT Settings

**appsettings.json:**
```json
{
  "Jwt": {
    "Secret": "your-secret-key-minimum-32-characters-long",
    "Issuer": "ChronicleHub",
    "Audience": "ChronicleHub",
    "ExpiresInMinutes": 15,
    "RefreshTokenLifetimeDays": 7
  }
}
```

**Environment Variables:**
```bash
export Jwt__Secret="your-secret-key-minimum-32-characters-long"
export Jwt__ExpiresInMinutes=15
export Jwt__RefreshTokenLifetimeDays=7
```

**Kubernetes Secret:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chroniclehub-jwt
type: Opaque
stringData:
  jwt-secret: "your-secret-key-minimum-32-characters-long"
```

**Generating Secure JWT Secrets:**
```bash
# Using OpenSSL (recommended)
openssl rand -base64 48

# Using Python
python3 -c "import secrets; print(secrets.token_urlsafe(48))"

# Using PowerShell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

## Security Best Practices

### ✅ DO

1. **Use Strong JWT Secrets**
   ```bash
   # Minimum 32 characters, recommended 48+
   openssl rand -base64 48
   ```

2. **Store Secrets Securely**
   - Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
   - Never commit secrets to version control
   - Use environment variables or Kubernetes secrets

3. **Rotate Refresh Tokens**
   - Automatic rotation on each refresh
   - Prevents token replay attacks
   - Old tokens immediately revoked

4. **Use HTTPS in Production**
   - TLS/SSL required for token security
   - Cookies marked as Secure
   - HSTS headers recommended

5. **Short-lived Access Tokens**
   - 15 minutes in production
   - Limits exposure if token leaked
   - Refresh tokens for longevity

6. **Monitor Authentication**
   ```csharp
   _logger.LogWarning("Failed login attempt for {Email} from {IP}",
       email, httpContext.Connection.RemoteIpAddress);
   ```

7. **API Key Rotation**
   ```csharp
   // Create new key
   var (newKey, plaintext) = await apiKeyService.CreateApiKeyAsync(...);

   // Grace period for old key
   await Task.Delay(TimeSpan.FromDays(7));

   // Revoke old key
   await apiKeyService.RevokeApiKeyAsync(oldKeyId);
   ```

### ❌ DON'T

1. **Don't Use Weak Secrets**
   ```bash
   # Bad examples:
   "secret"
   "12345"
   "password"
   ```

2. **Don't Share API Keys**
   - One key per service/environment
   - Track usage by key
   - Revoke compromised keys immediately

3. **Don't Log Sensitive Data**
   ```csharp
   // Wrong
   _logger.LogInformation($"API Key: {apiKey}");
   _logger.LogInformation($"Password: {password}");

   // Right
   _logger.LogInformation("API key validated for tenant {TenantId}", tenantId);
   ```

4. **Don't Store Tokens in Local Storage**
   ```javascript
   // Wrong - vulnerable to XSS
   localStorage.setItem('accessToken', token);

   // Right - use memory or secure storage
   const token = useState(null);
   ```

5. **Don't Skip HTTPS**
   - Tokens transmitted over HTTP are vulnerable
   - Man-in-the-middle attacks possible
   - Always use TLS in production

## Error Responses

### 401 Unauthorized - Missing Token

```json
{
  "type": "https://httpstatuses.io/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authorization header is missing or invalid.",
  "instance": "/api/events"
}
```

### 401 Unauthorized - Invalid API Key

```json
{
  "type": "https://httpstatuses.io/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid or expired API key.",
  "instance": "/api/events"
}
```

### 403 Forbidden - Insufficient Permissions

```json
{
  "type": "https://httpstatuses.io/403",
  "title": "Forbidden",
  "status": 403,
  "detail": "User does not have permission to access this resource.",
  "instance": "/api/stats/daily/2025-12-27"
}
```

## Swagger UI Authentication

### JWT Bearer Tokens

1. Click **Authorize** button (🔒)
2. Enter: `Bearer {your-access-token}`
3. Click **Authorize**
4. Click **Close**

### API Keys

1. Click **Authorize** button (🔒)
2. In **X-Api-Key** section, enter your API key
3. Click **Authorize**
4. Click **Close**

Both authentication methods can be used simultaneously in Swagger.

## Troubleshooting

### "Authorization header is missing"

**Problem:** JWT endpoint called without Authorization header

**Solution:**
```bash
curl -H "Authorization: Bearer {token}" ...
```

### "Invalid or expired API key"

**Problem:** API key is wrong, expired, or revoked

**Solution:**
1. Verify key format starts with `ch_live_`
2. Check expiration date in database
3. Verify key hasn't been revoked
4. Generate new key if needed

### "Token has expired"

**Problem:** Access token exceeded its lifetime

**Solution:**
```bash
# Refresh the token
curl -X POST /api/auth/refresh
```

### Cross-tenant access denied

**Problem:** Trying to access data from different tenant

**Solution:**
- Login to correct tenant
- Verify `tid` claim in JWT matches resource tenant
- Check tenant membership with `GET /api/auth/me`

### API key can't query events

**Problem:** Using API key for `GET /api/events`

**Solution:**
- API keys only work for `POST /api/events`
- Use JWT token for queries:
  ```bash
  curl -H "Authorization: Bearer {token}" /api/events
  ```

## Implementation Details

**Authentication Handlers:**
- `JwtBearerHandler` - Built-in ASP.NET Core JWT validation
- `ApiKeyAuthenticationHandler` - Custom API key validation

**Services:**
- `TokenService` - JWT generation and validation
- `ApiKeyService` - API key creation, validation, revocation
- `AuthenticationService` - User registration, login, refresh, logout

**Middleware:**
- `TenantResolutionMiddleware` - Extracts tenant context from JWT claims

**Database Tables:**
- `AspNetUsers` - User accounts (ASP.NET Core Identity)
- `Tenants` - Tenant organizations
- `UserTenants` - User-tenant membership with roles
- `ApiKeys` - Tenant-scoped API keys (SHA256 hashed)
- `RefreshTokens` - Refresh tokens (SHA256 hashed)

## Next Steps

- [API Endpoints](endpoints.md) - Complete API reference
- [Examples](examples.md) - Authenticated request examples
- [Configuration Guide](../configuration.md) - Detailed configuration
- [Architecture Overview](../architecture/overview.md) - System architecture
