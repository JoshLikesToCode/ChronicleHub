# API Endpoints Reference

Complete reference for all ChronicleHub API endpoints.

## Base URL

- **Development**: `http://localhost:5000`
- **Docker**: `http://localhost:8080`
- **Production**: Your deployed URL

## Authentication

**Write operations** (POST, PUT, PATCH, DELETE) require an API key.

**Read operations** (GET) are publicly accessible.

**Header:** `X-Api-Key: your-api-key-here`

See [Authentication Guide](authentication.md) for details.

## Events API

### Create Event

Create a new activity event.

**Endpoint:** `POST /api/events`

**Authentication:** Required (API Key)

**Request Body:**
```json
{
  "Type": "string",           // Event type/category (required)
  "Source": "string",         // Event source system (required)
  "TimestampUtc": "datetime", // Event timestamp (optional, defaults to now)
  "Payload": {                // Event data (required, any JSON object)
    "key": "value"
  }
}
```

**Response:** `201 Created`
```json
{
  "Id": "guid",
  "TenantId": "guid",
  "UserId": "guid",
  "Type": "string",
  "Source": "string",
  "TimestampUtc": "datetime",
  "Payload": {
    "key": "value"
  },
  "CreatedAtUtc": "datetime",
  "ReceivedAtUtc": "datetime",
  "ProcessingDurationMs": 23.5
}
```

**Example:**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-chronicle-hub-key-12345" \
  -d '{
    "Type": "user_login",
    "Source": "WebApp",
    "TimestampUtc": "2025-12-13T10:30:00Z",
    "Payload": {
      "userId": "user-12345",
      "loginMethod": "email",
      "ipAddress": "192.168.1.100"
    }
  }'
```

**Validation Rules:**
- `Type` must not be empty
- `Source` must not be empty
- `TimestampUtc` cannot be more than 1 day in the future
- `Payload` must not be null

**Error Responses:**
- `400 Bad Request` - Validation failed
- `401 Unauthorized` - Missing or invalid API key

---

### Get Event by ID

Retrieve a specific event by its ID.

**Endpoint:** `GET /api/events/{id}`

**Authentication:** Not required

**Path Parameters:**
- `id` (guid) - Event ID

**Response:** `200 OK`
```json
{
  "Id": "guid",
  "TenantId": "guid",
  "UserId": "guid",
  "Type": "string",
  "Source": "string",
  "TimestampUtc": "datetime",
  "Payload": {
    "key": "value"
  },
  "CreatedAtUtc": "datetime",
  "ReceivedAtUtc": "datetime",
  "ProcessingDurationMs": null
}
```

**Example:**
```bash
curl http://localhost:5000/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Error Responses:**
- `404 Not Found` - Event does not exist

---

### Query Events

Query events with filtering and pagination.

**Endpoint:** `GET /api/events`

**Authentication:** Not required

**Query Parameters:**
- `page` (int, optional) - Page number (default: 1)
- `pageSize` (int, optional) - Items per page (default: 20, max: 100)
- `type` (string, optional) - Filter by event type
- `source` (string, optional) - Filter by source
- `startDate` (datetime, optional) - Filter events after this date
- `endDate` (datetime, optional) - Filter events before this date

**Response:** `200 OK`
```json
{
  "Items": [
    {
      "Id": "guid",
      "Type": "string",
      "Source": "string",
      "TimestampUtc": "datetime",
      "Payload": {},
      "CreatedAtUtc": "datetime"
    }
  ],
  "Page": 1,
  "PageSize": 20,
  "TotalCount": 150
}
```

**Examples:**
```bash
# Get first page (20 items)
curl http://localhost:5000/api/events

# Get page 2 with 50 items
curl http://localhost:5000/api/events?page=2&pageSize=50

# Filter by type
curl http://localhost:5000/api/events?type=user_login

# Filter by date range
curl "http://localhost:5000/api/events?startDate=2025-12-01&endDate=2025-12-31"

# Combine filters
curl "http://localhost:5000/api/events?type=purchase&source=MobileApp&page=1&pageSize=10"
```

---

## Statistics API

### Get Daily Statistics

Retrieve aggregated statistics for a specific date.

**Endpoint:** `GET /api/stats/daily/{date}`

**Authentication:** Not required

**Path Parameters:**
- `date` (date) - Date in format `YYYY-MM-DD`

**Response:** `200 OK`
```json
{
  "Data": {
    "TenantId": "guid",
    "UserId": "guid",
    "Date": "date",
    "TotalEvents": 42,
    "CategoryBreakdown": [
      {
        "Category": "user_login",
        "EventCount": 15
      },
      {
        "Category": "page_view",
        "EventCount": 20
      },
      {
        "Category": "purchase",
        "EventCount": 7
      }
    ]
  },
  "Error": null,
  "Metadata": {
    "RequestDurationMs": 12.3,
    "TimestampUtc": "datetime"
  }
}
```

**Example:**
```bash
curl http://localhost:5000/api/stats/daily/2025-12-13
```

**Error Responses:**
- `404 Not Found` - No statistics found for this date

---

## Health Check Endpoints

### Liveness Probe

Check if the application is running.

**Endpoint:** `GET /health/live`

**Authentication:** Not required

**Response:** `200 OK`
```json
{
  "status": "Healthy",
  "timestamp": "datetime"
}
```

**Usage:** Kubernetes liveness probe to restart unhealthy pods.

---

### Readiness Probe

Check if the application is ready to accept traffic (database connected).

**Endpoint:** `GET /health/ready`

**Authentication:** Not required

**Response:** `200 OK` (healthy)
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "datetime"
}
```

**Response:** `503 Service Unavailable` (unhealthy)
```json
{
  "status": "Unhealthy",
  "database": "Disconnected",
  "error": "error message",
  "timestamp": "datetime"
}
```

**Usage:** Kubernetes readiness probe to control traffic routing.

---

## Error Responses (RFC 9457)

All errors follow RFC 9457 Problem Details specification with `application/problem+json` content type.

### 400 Bad Request (Validation Error)

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/events",
  "errors": {
    "Type": ["Type is required"],
    "TimestampUtc": ["Timestamp cannot be in the future"]
  }
}
```

### 401 Unauthorized

```json
{
  "type": "https://httpstatuses.io/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "API key is required for this operation.",
  "instance": "/api/events"
}
```

### 404 Not Found

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Not Found",
  "status": 404,
  "detail": "ActivityEvent with key 'abc-123' was not found.",
  "instance": "/api/events/abc-123",
  "resourceName": "ActivityEvent",
  "resourceKey": "abc-123"
}
```

### 500 Internal Server Error

**Development:**
```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "NullReferenceException: Object reference not set...",
  "instance": "/api/events",
  "stackTrace": "..."
}
```

**Production:**
```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred processing your request.",
  "instance": "/api/events"
}
```

---

## Rate Limiting

Currently not implemented. For production deployments, consider adding rate limiting at:
- API Gateway level
- Ingress controller (e.g., NGINX `nginx.ingress.kubernetes.io/rate-limit`)
- Application middleware (e.g., AspNetCoreRateLimit)

## Versioning

API version 1 (implicit). Future versions may use:
- URL versioning: `/api/v2/events`
- Header versioning: `X-API-Version: 2`
- Query parameter: `/api/events?api-version=2`

## OpenAPI Specification

Interactive API documentation available at `/swagger` when Swagger is enabled:

```bash
# Enable Swagger
export Swagger__Enabled=true

# Access Swagger UI
open http://localhost:5000/swagger
```

**Download OpenAPI spec:**
```bash
curl http://localhost:5000/swagger/v1/swagger.json
```

## Next Steps

- [Authentication Guide](authentication.md) - API key authentication
- [Examples](examples.md) - Common usage patterns
- [Error Handling](../architecture/error-handling.md) - Detailed error response format
