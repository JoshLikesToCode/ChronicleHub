# Error Handling - RFC 9457 Problem Details

ChronicleHub implements [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html) Problem Details for HTTP APIs, providing standardized, machine-readable error responses.

## Why RFC 9457?

**Traditional error responses:**
```json
{
  "error": "Not found",
  "message": "Event does not exist"
}
```

**Problems:**
- No standard format
- Hard to parse programmatically
- Missing contextual information
- Inconsistent across APIs

**RFC 9457 solves this** with a standard format recognized by HTTP clients, API gateways, and monitoring tools.

## Response Format

All errors return `Content-Type: application/problem+json`:

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Not Found",
  "status": 404,
  "detail": "ActivityEvent with key 'abc-123' was not found.",
  "instance": "/api/events/abc-123"
}
```

### Standard Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | URI | Yes | URI identifying the problem type |
| `title` | string | Yes | Human-readable summary |
| `status` | int | Yes | HTTP status code |
| `detail` | string | No | Human-readable explanation |
| `instance` | URI | No | URI identifying this specific occurrence |

### Extension Members

ChronicleHub adds custom fields for additional context:

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

## Domain Exceptions

Located in `ChronicleHub.Domain/Exceptions/`:

### NotFoundException (404)

**Usage:**
```csharp
throw new NotFoundException("ActivityEvent", id);
```

**Response:**
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

### ValidationException (400)

**Usage:**
```csharp
var errors = new Dictionary<string, string[]>
{
    ["Type"] = new[] { "Type is required" },
    ["TimestampUtc"] = new[] { "Timestamp cannot be in the future" }
};
throw new ValidationException(errors);
```

**Response:**
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

### ConflictException (409)

**Usage:**
```csharp
throw new ConflictException("Event with this ID already exists");
```

**Response:**
```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Event with this ID already exists",
  "instance": "/api/events"
}
```

### UnauthorizedException (401)

**Usage:**
```csharp
throw new UnauthorizedException("API key is required");
```

**Response:**
```json
{
  "type": "https://httpstatuses.io/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "API key is required",
  "instance": "/api/events"
}
```

### ForbiddenException (403)

**Usage:**
```csharp
throw new ForbiddenException("You don't have permission to access this resource");
```

**Response:**
```json
{
  "type": "https://httpstatuses.io/403",
  "title": "Forbidden",
  "status": 403,
  "detail": "You don't have permission to access this resource",
  "instance": "/api/events"
}
```

## Global Exception Handling

The `ProblemDetailsExceptionMiddleware` catches all exceptions and converts them to Problem Details responses.

**Location:** `src/ChronicleHub.Api/Middleware/ProblemDetailsExceptionMiddleware.cs`

### Flow

```
1. Request enters pipeline
   ↓
2. Exception thrown (anywhere in pipeline)
   ↓
3. Middleware catches exception
   ↓
4. Convert to Problem Details
   ↓
5. Return application/problem+json response
```

### Environment-Specific Behavior

**Development Mode:**
```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "NullReferenceException: Object reference not set to an instance of an object",
  "instance": "/api/events",
  "stackTrace": "   at ChronicleHub.Api.Controllers.EventsController.CreateEvent(...)\n   at ..."
}
```

**Production Mode:**
```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred processing your request.",
  "instance": "/api/events"
}
```

**Why?**
- Development: Full details help debugging
- Production: Generic messages prevent information leakage

## Factory Pattern

The `ProblemDetailsFactory` creates consistent Problem Details responses:

**Location:** `src/ChronicleHub.Application/ProblemDetails/ProblemDetailsFactory.cs`

**Usage:**
```csharp
var problemDetails = ProblemDetailsFactory.Create(
    statusCode: 404,
    title: "Not Found",
    detail: "Event not found",
    instance: httpContext.Request.Path
);
```

## FluentValidation Integration

Validation errors automatically convert to RFC 9457 format:

```csharp
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("Type is required");
        RuleFor(x => x.Source).NotEmpty().WithMessage("Source is required");
        RuleFor(x => x.Payload).NotNull().WithMessage("Payload cannot be null");
        RuleFor(x => x.TimestampUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Timestamp cannot be more than 1 day in the future");
    }
}
```

**Automatic Problem Details response** when validation fails.

## Client Handling

### JavaScript

```javascript
try {
  const response = await fetch('/api/events/invalid-id');
  if (!response.ok) {
    const problem = await response.json();
    console.error(`Error ${problem.status}: ${problem.title}`);
    console.error(problem.detail);
    if (problem.errors) {
      Object.entries(problem.errors).forEach(([field, messages]) => {
        console.error(`${field}: ${messages.join(', ')}`);
      });
    }
  }
} catch (error) {
  console.error('Network error:', error);
}
```

### C#

```csharp
using System.Net.Http.Json;

public async Task<Event> GetEvent(Guid id)
{
    var response = await _httpClient.GetAsync($"/api/events/{id}");

    if (!response.IsSuccessStatusCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        throw new ApplicationException($"{problem.Title}: {problem.Detail}");
    }

    return await response.Content.ReadFromJsonAsync<Event>();
}
```

### Python

```python
import requests

response = requests.get('http://localhost:5000/api/events/invalid-id')
if not response.ok:
    problem = response.json()
    print(f"Error {problem['status']}: {problem['title']}")
    print(problem['detail'])
    if 'errors' in problem:
        for field, messages in problem['errors'].items():
            print(f"{field}: {', '.join(messages)}")
```

## Testing

Example tests from `ChronicleHub.Application.Tests`:

```csharp
[Fact]
public void Create_NotFound_ReturnsCorrectProblemDetails()
{
    // Arrange
    var exception = new NotFoundException("Event", "123");

    // Act
    var problemDetails = ProblemDetailsFactory.Create(
        statusCode: 404,
        title: "Not Found",
        detail: exception.Message,
        instance: "/api/events/123"
    );

    // Assert
    Assert.Equal(404, problemDetails.Status);
    Assert.Equal("Not Found", problemDetails.Title);
    Assert.Contains("Event", problemDetails.Detail);
}
```

## Best Practices

### ✅ DO

1. **Use domain exceptions for business errors**
   ```csharp
   throw new NotFoundException("Event", id);
   ```

2. **Provide helpful detail messages**
   ```csharp
   throw new ValidationException("Email format is invalid");
   ```

3. **Include context in extension members**
   ```json
   {
     "resourceName": "Event",
     "resourceKey": "123",
     "attemptedAction": "delete"
   }
   ```

### ❌ DON'T

1. **Don't expose sensitive information**
   ```csharp
   // Bad
   throw new Exception($"Database password {password} is invalid");
   ```

2. **Don't use generic exceptions**
   ```csharp
   // Bad
   throw new Exception("Error");

   // Good
   throw new NotFoundException("Event", id);
   ```

3. **Don't return different formats**
   - Always use RFC 9457 format
   - Never mix JSON error formats

## Monitoring

Problem Details responses are logged with correlation IDs:

```json
{
  "@t": "2025-12-20T01:53:36Z",
  "@l": "Warning",
  "@mt": "Request failed with status {StatusCode}",
  "StatusCode": 404,
  "ProblemType": "NotFoundException",
  "ResourceName": "Event",
  "CorrelationId": "abc-123",
  "Path": "/api/events/456"
}
```

## References

- [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html) - Problem Details for HTTP APIs
- [HTTP Status Codes](https://httpstatuses.io/) - Used for `type` URIs
- [ProblemDetails NuGet](https://www.nuget.org/packages/Hellang.Middleware.ProblemDetails/) - Alternative implementation

## Next Steps

- [API Endpoints](../api/endpoints.md) - See error responses for each endpoint
- [Architecture Overview](overview.md) - Understand the system design
- [Development Guide](../development.md) - Add new exceptions and handlers
