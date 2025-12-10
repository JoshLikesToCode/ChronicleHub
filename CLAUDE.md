# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ChronicleHub is a cloud-native .NET 8 analytics API that ingests user activity events and processes them. The solution uses a Clean Architecture pattern with four main projects:

- **ChronicleHub.Domain** - Core domain entities (ActivityEvent, DailyStats, CategoryStats)
- **ChronicleHub.Application** - Application logic layer (currently minimal)
- **ChronicleHub.Infrastructure** - Data access with EF Core and SQLite
- **ChronicleHub.Api** - ASP.NET Core Web API with REST endpoints

## Architecture

### Database Strategy
- **Development**: SQLite (default connection string: `Data Source=chroniclehub.db`)
- **Production**: SQL Server support included via EF Core migrations
- Auto-migration runs on application startup (see Program.cs:27-31)

### Domain Model
The core entity is `ActivityEvent`, which stores user activity with:
- Multi-tenant support (TenantId, UserId - currently using Guid.Empty placeholders)
- Event metadata (Type, Source, TimestampUtc)
- Flexible JSON payload stored as string
- Composite index on (TenantId, UserId, TimestampUtc)

Stats entities (DailyStats, CategoryStats) are defined but not yet implemented in the API.

### API Patterns
- Controllers are in `ChronicleHub.Api/Controllers/`
- Request/response DTOs are in `ChronicleHub.Api/Contracts/`
- Custom LINQ extension `WhereIf` is used for conditional query filtering (see ExtensionMethods/WhereIf.cs)
- JSON serialization uses property names as-is (not camelCase)

### Entity Framework
- DbContext: `ChronicleHubDbContext` in Infrastructure layer
- Migrations are in `Infrastructure/Persistence/Migrations/`
- Design-time factory at `Infrastructure/Persistence/DesignTimeDbContextFactory.cs`

## Common Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the API (from root directory)
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Run in watch mode for development
dotnet watch --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### Database Migrations
```bash
# Add a new migration (run from root directory)
dotnet ef migrations add MigrationName --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Update database manually (normally auto-runs on startup)
dotnet ef database update --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Remove last migration
dotnet ef migrations remove --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### Docker
```bash
# Build Docker image
docker build -t chroniclehub-api .

# Run container
docker run -p 8080:8080 chroniclehub-api
```

### API Testing
- Swagger UI available at `/swagger` when running in Development mode
- API endpoints are at `/api/[controller]`
- Current endpoints:
  - POST `/api/events` - Create an event
  - GET `/api/events/{id}` - Get event by ID
  - GET `/api/events` - Query events with filtering/paging

## Important Notes

### Authentication/Authorization
Authentication and authorization are currently disabled (see Program.cs:42-43). TenantId and UserId are hardcoded to Guid.Empty in controllers. This needs to be implemented before production use.

### Domain Entities
Domain entities use private setters and constructors to enforce encapsulation. When creating new entities, follow the pattern in `ActivityEvent.cs`:
- Private parameterless constructor for EF Core
- Public constructor with all required parameters
- Private setters on all properties

### JSON Handling
The API stores event payloads as JSON strings. When working with payloads:
- Accept `JsonElement` in request DTOs
- Store via `.GetRawText()`
- Parse with `JsonDocument.Parse()` when reading
- Clone JsonElement when returning: `payloadDoc.RootElement.Clone()`

### Clean Architecture Dependencies
Maintain proper dependency flow:
- Domain → (no dependencies)
- Application → Domain
- Infrastructure → Domain, Application
- Api → Domain, Application, Infrastructure
