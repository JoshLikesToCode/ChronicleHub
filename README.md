# ChronicleHub

ChronicleHub is a cloud-native .NET 8 analytics API that ingests user activity events and processes them.

## Technology Stack

- .NET 8 / ASP.NET Core
- Entity Framework Core
- SQLite (development) / SQL Server (production)
- Clean Architecture pattern
- Docker containerization

## Development Approach

This project is developed using modern AI-assisted workflows with [Claude Code](https://claude.ai/code). The `CLAUDE.md` file provides architectural context and development guidance for enhanced productivity and consistency.

## Getting Started

```bash
# Run the API
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Access Swagger UI (Development mode)
# Navigate to http://localhost:5000/swagger
```

See `CLAUDE.md` for detailed architecture documentation and development commands
