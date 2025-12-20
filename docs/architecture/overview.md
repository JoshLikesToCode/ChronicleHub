# Architecture Overview

ChronicleHub is built using Clean Architecture principles with a focus on maintainability, testability, and production readiness.

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Applications                      │
│  (Web Apps, Mobile Apps, Backend Services, IoT Devices)     │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP/HTTPS
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                      Load Balancer                           │
│              (Ingress / Cloud Load Balancer)                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   ChronicleHub API (Pods)                    │
│                                                              │
│  ┌────────────┐  ┌──────────────┐  ┌───────────────────┐   │
│  │ Middleware │  │ Controllers  │  │ Validation &      │   │
│  │  - Auth    │  │  - Events    │  │ Error Handling    │   │
│  │  - Logging │  │  - Stats     │  │ (FluentValidation)│   │
│  │  - Timing  │  │  - Health    │  │ (RFC 9457)        │   │
│  └────┬───────┘  └──────┬───────┘  └─────────┬─────────┘   │
│       │                 │                     │             │
│       └─────────────────┴─────────────────────┘             │
│                         │                                    │
│                         ▼                                    │
│              ┌──────────────────────┐                        │
│              │  Application Layer   │                        │
│              │  - Services          │                        │
│              │  - Business Logic    │                        │
│              └──────────┬───────────┘                        │
│                         │                                    │
│                         ▼                                    │
│              ┌──────────────────────┐                        │
│              │  Infrastructure      │                        │
│              │  - EF Core DbContext │                        │
│              │  - Repositories      │                        │
│              └──────────┬───────────┘                        │
└──────────────────────────┼──────────────────────────────────┘
                           │
                           ▼
                  ┌────────────────────┐
                  │    Database        │
                  │ (PostgreSQL/SQLite)│
                  └────────────────────┘
```

## Clean Architecture Layers

ChronicleHub follows the Clean Architecture pattern with strict dependency rules:

### 1. Domain Layer (`ChronicleHub.Domain`)

**Purpose:** Core business entities and domain logic

**Dependencies:** None (pure C#)

**Contains:**
- Entities: `ActivityEvent`, `DailyStats`, `CategoryStats`
- Domain Exceptions: `NotFoundException`, `ValidationException`, etc.
- Value Objects (future)

**Key Principles:**
- No dependencies on external frameworks
- Private setters for encapsulation
- Factory methods for entity creation
- Domain events (future)

**Example:**
```csharp
public class ActivityEvent
{
    private ActivityEvent() { } // EF Core

    public ActivityEvent(Guid tenantId, Guid userId, string type, string source)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        Type = type;
        Source = source;
        // ...
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; }
    // ...
}
```

### 2. Application Layer (`ChronicleHub.Application`)

**Purpose:** Application business rules and use cases

**Dependencies:** Domain

**Contains:**
- Service Interfaces
- DTOs (future)
- ProblemDetails Factory
- Application Exceptions

**Key Principles:**
- Orchestrates domain objects
- Defines interfaces that Infrastructure implements
- Contains application-specific business rules

### 3. Infrastructure Layer (`ChronicleHub.Infrastructure`)

**Purpose:** External concerns and framework implementations

**Dependencies:** Domain, Application, EF Core, Npgsql

**Contains:**
- `ChronicleHubDbContext` (EF Core)
- Entity Configurations
- Database Migrations
- Service Implementations (`StatisticsService`)

**Key Principles:**
- Implements interfaces defined in Application
- Database-specific code isolated here
- Easy to swap implementations

### 4. API Layer (`ChronicleHub.Api`)

**Purpose:** HTTP API and presentation concerns

**Dependencies:** All other layers

**Contains:**
- Controllers
- Request/Response DTOs (Contracts)
- Middleware (Auth, Logging, Error Handling)
- Validators (FluentValidation)
- Startup Configuration

**Key Principles:**
- Thin controllers (delegate to services)
- Input validation
- DTO mapping
- HTTP-specific concerns only

## Dependency Flow

```
API → Infrastructure → Application → Domain
         ↓                 ↓
    (implements)      (defines)
```

**The Dependency Rule:**
- Inner layers don't know about outer layers
- Domain doesn't reference any other layer
- Application doesn't reference Infrastructure or API
- Infrastructure and API can reference all inner layers

**Benefits:**
- Testable business logic (mock infrastructure)
- Database-agnostic domain
- Easy to swap implementations
- Clear separation of concerns

## Event Sourcing Pattern

ChronicleHub implements event sourcing for analytics:

### Data Flow

```
1. Event Ingestion
   POST /api/events
   ↓
2. Store Immutable Event
   INSERT INTO ActivityEvents
   ↓
3. Update Statistics
   StatisticsService.UpdateStatisticsAsync()
   ↓
4. Update Aggregates
   DailyStats (date-level rollup)
   CategoryStats (category breakdown)
```

### Benefits

- **Complete Audit Trail**: All events stored permanently
- **Replayability**: Can recompute statistics from raw events
- **Time Travel**: Query historical state at any point
- **Debugging**: Full event history for troubleshooting
- **Analytics**: Foundation for time-series analysis

### Trade-offs

- **Storage Growth**: Events accumulate over time (archiving needed)
- **Query Complexity**: Deriving state from events vs direct queries
- **Eventual Consistency**: Statistics may lag behind events (mitigated by synchronous updates)

## Database Strategy

### Multi-Database Support

The application automatically detects database type from connection string:

```csharp
if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
{
    options.UseNpgsql(connectionString);  // PostgreSQL
}
else
{
    options.UseSqlite(connectionString);  // SQLite
}
```

### Migration Strategy

Migrations are stored in Infrastructure layer and run automatically on startup:

```csharp
if (db.Database.GetType().Name != "InMemoryDatabase")
{
    db.Database.Migrate();
}
```

**Development:**
- SQLite for zero-configuration local dev
- Database file in project root

**Production:**
- PostgreSQL for scalability and concurrency
- Managed database service (RDS, Azure Database, etc.)

### Entity Framework Configurations

Fluent API configurations in `Infrastructure/Persistence/Configurations/`:

```csharp
builder.Entity<ActivityEvent>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Type).IsRequired();
    entity.Property(e => e.PayloadJson).IsRequired();
    entity.HasIndex(e => new { e.TenantId, e.UserId, e.TimestampUtc });
});
```

## API Design Principles

### RESTful Conventions

- **Resources**: `/api/events`, `/api/stats`
- **HTTP Methods**: GET (read), POST (create), PUT (update), DELETE (remove)
- **Status Codes**: 200 (OK), 201 (Created), 400 (Bad Request), 404 (Not Found), etc.

### Response Format

All responses wrapped with metadata:

```json
{
  "Data": { ... },
  "Error": null,
  "Metadata": {
    "RequestDurationMs": 12.3,
    "TimestampUtc": "2025-12-13T10:30:00Z"
  }
}
```

### Error Handling (RFC 9457)

See [Error Handling Documentation](error-handling.md)

## Observability

### Structured Logging (Serilog)

```json
{
  "@t": "2025-12-20T01:53:36.9820064Z",
  "@mt": "Request finished",
  "ElapsedMilliseconds": 52.0362,
  "StatusCode": 201,
  "CorrelationId": "1e29f30d-9547-494c-8cf1-7e5cd8f723c8",
  "MachineName": "pod-abc123",
  "EnvironmentName": "Production"
}
```

### OpenTelemetry

Distributed tracing for ASP.NET Core and Entity Framework Core:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ChronicleHub"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter());
```

### Correlation IDs

Every request gets a unique correlation ID for tracking across services:

```http
X-Correlation-Id: 1e29f30d-9547-494c-8cf1-7e5cd8f723c8
```

### Health Checks

- **Liveness**: `/health/live` (app running)
- **Readiness**: `/health/ready` (dependencies healthy)

## Security

### Authentication

- API key for write operations
- Public read access
- JWT tokens (future)

### Principle of Least Privilege

- Container runs as non-root user
- Database user has minimal permissions
- Read-only filesystem (where possible)

### Defense in Depth

- Input validation (FluentValidation)
- Output encoding (automatic JSON serialization)
- Parameterized queries (EF Core)
- HTTPS in production

## Scalability

### Horizontal Scaling

- Stateless API (no in-memory session)
- Database connection pooling
- Kubernetes-ready

### Performance Optimizations

- Async/await throughout
- Pagination for large result sets
- Database indexing on query paths
- Compiled queries (future)

### Future Enhancements

- Redis caching for frequently accessed data
- Read replicas for query scaling
- Event streaming (Kafka, Azure Event Hubs)
- CQRS with separate read/write databases

## Technology Choices

| Concern | Technology | Rationale |
|---------|-----------|-----------|
| **Framework** | .NET 8 | Latest LTS, high performance, cloud-native |
| **ORM** | EF Core | Code-first migrations, LINQ, testability |
| **Validation** | FluentValidation | Strongly-typed, composable, testable |
| **Logging** | Serilog | Structured logging, multiple sinks |
| **Telemetry** | OpenTelemetry | Vendor-neutral, distributed tracing |
| **Containers** | Docker | Portable, consistent environments |
| **Orchestration** | Kubernetes | Production-grade, cloud-portable |

## Next Steps

- [Error Handling](error-handling.md) - RFC 9457 Problem Details
- [Database Design](database.md) - Schema and migrations (future)
- [Deployment Guide](../deployment/kubernetes.md) - Kubernetes deployment
