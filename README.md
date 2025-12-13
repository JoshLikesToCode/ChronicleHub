# ChronicleHub

**A cloud-native event-sourced analytics platform for capturing and analyzing user activity.**

ChronicleHub is a production-ready .NET 8 API that ingests activity events from multiple sources, automatically computes derived statistics, and provides real-time analytics through RESTful endpoints. Built with Clean Architecture principles and designed for scalability.

## Features

- **Event Ingestion** - Capture user activity events with flexible JSON payloads
- **Automatic Statistics** - Real-time computation of daily and category statistics
- **Multi-Tenant Ready** - Built-in tenant and user isolation (authentication pending)
- **API Key Authentication** - Secure endpoints with configurable API keys
- **Request Validation** - FluentValidation with comprehensive error handling
- **Response Metadata** - Automatic timing and processing metrics
- **Swagger Documentation** - Interactive API explorer in development mode
- **Docker Support** - Containerized deployment ready for cloud platforms

## Technology Stack

- **.NET 8** - Latest LTS framework with native AOT support
- **ASP.NET Core** - High-performance web API framework
- **Entity Framework Core** - ORM with SQLite (dev) and SQL Server (prod) support
- **FluentValidation** - Strongly-typed validation rules
- **Clean Architecture** - Maintainable domain-driven design
- **Docker** - Container-based deployment

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)
- [Docker](https://www.docker.com/get-started) (for containerized deployment)

### Running Locally

```bash
# Clone the repository
git clone https://github.com/yourusername/ChronicleHub.git
cd ChronicleHub

# Restore dependencies and build
dotnet build

# Run the API
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# The API will start on http://localhost:5000
```

**Access Swagger UI:** Navigate to http://localhost:5000/swagger

### Running with Docker

```bash
# Build the Docker image
docker build -t chroniclehub-api .

# Run the container
docker run -p 8080:8080 chroniclehub-api

# The API will be available at http://localhost:8080
```

**Access Swagger UI:** Navigate to http://localhost:8080/swagger

## API Usage

### Authentication

All API endpoints (except Swagger) require an API key via the `X-Api-Key` header.

**Development API Key:** `dev-chronicle-hub-key-12345`

Configure in `appsettings.Development.json`:
```json
{
  "ApiKey": {
    "DevKey": "dev-chronicle-hub-key-12345"
  }
}
```

### Using Swagger UI

1. Navigate to http://localhost:5000/swagger
2. Click the **Authorize** button (lock icon)
3. Enter API key: `dev-chronicle-hub-key-12345`
4. Click **Authorize** and close the dialog
5. All endpoints are now accessible - expand and try them!

### Example: Creating an Event

**Request:**
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

**Response:**
```json
{
  "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Type": "user_login",
  "Source": "WebApp",
  "TimestampUtc": "2025-12-13T10:30:00Z",
  "Payload": {
    "userId": "user-12345",
    "loginMethod": "email",
    "ipAddress": "192.168.1.100"
  },
  "CreatedAtUtc": "2025-12-13T10:30:15Z",
  "ReceivedAtUtc": "2025-12-13T10:30:15Z",
  "ProcessingDurationMs": 23.5
}
```

### Example: Getting Daily Statistics

**Request:**
```bash
curl -X GET http://localhost:5000/api/stats/daily/2025-12-13 \
  -H "X-Api-Key: dev-chronicle-hub-key-12345"
```

**Response:**
```json
{
  "Data": {
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "UserId": "00000000-0000-0000-0000-000000000000",
    "Date": "2025-12-13",
    "TotalEvents": 42,
    "CategoryBreakdown": [
      { "Category": "user_login", "EventCount": 15 },
      { "Category": "page_view", "EventCount": 20 },
      { "Category": "purchase", "EventCount": 7 }
    ]
  },
  "Error": null,
  "Metadata": {
    "RequestDurationMs": 12.3,
    "TimestampUtc": "2025-12-13T10:35:00Z"
  }
}
```

## Sample Events

The `/samples` directory contains example event JSON files:

- **user-login-event.json** - User authentication event
- **purchase-event.json** - E-commerce transaction event
- **page-view-event.json** - Web analytics event

See [samples/README.md](samples/README.md) for detailed usage instructions.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/events` | Create a new event |
| GET | `/api/events/{id}` | Get event by ID |
| GET | `/api/events?page=1&pageSize=10` | Query events with filtering/paging |
| GET | `/api/stats/daily/{date}` | Get daily statistics for a date |

## Architecture Decisions

### Clean Architecture

ChronicleHub follows Clean Architecture principles with clear separation of concerns:

- **Domain Layer** - Core entities (ActivityEvent, DailyStats, CategoryStats) with no external dependencies
- **Application Layer** - Business logic and service interfaces
- **Infrastructure Layer** - Data access (EF Core), external services, and concrete implementations
- **API Layer** - Controllers, DTOs, validation, and middleware

**Benefits:**
- Testable business logic isolated from infrastructure
- Database-agnostic domain model
- Easy to swap implementations (e.g., SQLite → SQL Server)
- Clear dependency flow prevents coupling

### Event Sourcing Pattern

Events are stored as immutable records, and statistics are derived automatically:

1. **Event Ingestion** - POST to `/api/events` stores event in `ActivityEvents` table
2. **Statistics Computation** - `StatisticsService` updates `DailyStats` and `CategoryStats`
3. **Query Statistics** - GET from `/api/stats/daily/{date}` retrieves aggregated data

**Benefits:**
- Complete audit trail of all activity
- Derived statistics can be recomputed if needed
- Time-series analytics and trend analysis ready
- Foundation for event replay and CQRS patterns

### Database Strategy

- **Development:** SQLite for zero-configuration local development
- **Production:** SQL Server via EF Core migrations (connection string swap)
- **Auto-Migration:** Database is created/updated automatically on startup

### Validation Approach

Using **FluentValidation** instead of Data Annotations:

- Strongly-typed validation rules
- Reusable and composable validators
- Clear separation from domain models
- Easy to test validation logic independently

**Validation Rules:**
- `Type` and `Source` must not be empty
- `TimestampUtc` cannot be more than 1 day in the future
- `Payload` cannot be null or undefined

### API Design

- **RESTful** - Standard HTTP methods and status codes
- **Consistent Responses** - All responses wrapped in `ApiResponse<T>` with metadata
- **Request Timing** - Middleware tracks processing duration automatically
- **Error Handling** - Structured error responses with codes and details

## Project Structure

```
ChronicleHub/
├── src/
│   ├── ChronicleHub.Domain/         # Core entities and domain logic
│   ├── ChronicleHub.Application/    # Application services and interfaces
│   ├── ChronicleHub.Infrastructure/ # Data access and external services
│   └── ChronicleHub.Api/            # Web API, controllers, DTOs
├── tests/
│   ├── ChronicleHub.Domain.Tests/
│   ├── ChronicleHub.Api.Tests/
│   └── ChronicleHub.Api.IntegrationTests/
├── samples/                         # Sample event JSON files
├── CLAUDE.md                        # AI-assisted development guide
├── Reasoning.md                     # Development decision log
└── README.md                        # This file
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/ChronicleHub.Api.IntegrationTests/
```

**Current Test Coverage:** 78 tests passing (11 domain + 52 API + 15 integration)

## Development

See [CLAUDE.md](CLAUDE.md) for:
- Detailed architecture documentation
- Common development commands
- Database migration instructions
- Docker build and deployment
- Coding conventions and patterns

See [Reasoning.md](Reasoning.md) for development decision history.

## Roadmap

- [ ] JWT-based authentication with tenant/user claims
- [ ] Real-time statistics via SignalR
- [ ] Time-series analytics endpoints
- [ ] Event replay and reprocessing
- [ ] Grafana dashboards
- [ ] Horizontal scaling with Redis caching
- [ ] Background job processing with Hangfire

## Contributing

This project is developed using AI-assisted workflows with [Claude Code](https://claude.ai/code). See `CLAUDE.md` for development context.

## License

MIT License - see LICENSE file for details
