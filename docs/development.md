# Development Guide

Guide for developers working on ChronicleHub.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- IDE: [Visual Studio](https://visualstudio.microsoft.com/), [Rider](https://www.jetbrains.com/rider/), or [VS Code](https://code.visualstudio.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional)

## Getting Started

```bash
# Clone the repository
git clone https://github.com/JoshLikesToCode/ChronicleHub.git
cd ChronicleHub

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Access at http://localhost:5000/swagger
```

## Project Structure

```
ChronicleHub/
├── src/
│   ├── ChronicleHub.Domain/          # Core business entities
│   ├── ChronicleHub.Application/     # Application services
│   ├── ChronicleHub.Infrastructure/  # Data access, EF Core
│   └── ChronicleHub.Api/            # Web API, controllers
├── tests/
│   ├── ChronicleHub.Domain.Tests/
│   ├── ChronicleHub.Application.Tests/
│   ├── ChronicleHub.Api.Tests/
│   └── ChronicleHub.Api.IntegrationTests/
├── docs/                            # Documentation
├── k8s/                             # Kubernetes manifests
└── samples/                         # Sample event files
```

## Common Development Tasks

### Running Locally

```bash
# Development mode (SQLite, Swagger enabled)
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Watch mode (hot reload)
dotnet watch --project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Specific environment
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName \
  --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj \
  --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Apply migrations
dotnet ef database update \
  --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj \
  --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Remove last migration
dotnet ef migrations remove \
  --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj \
  --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj

# Generate SQL script
dotnet ef migrations script \
  --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj \
  --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj \
  --output migration.sql
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/ChronicleHub.Api.Tests/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Watch mode
dotnet watch test --project tests/ChronicleHub.Api.Tests/

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Building for Production

```bash
# Release build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Run published app
cd publish
dotnet ChronicleHub.Api.dll
```

## Coding Conventions

### Domain Entities

Use private setters and constructors:

```csharp
public class ActivityEvent
{
    // Private for EF Core
    private ActivityEvent() { }

    // Public factory method
    public ActivityEvent(string type, string source, object payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Source = source;
        // ...
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; }
}
```

### Controllers

Keep controllers thin, delegate to services:

```csharp
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> CreateEvent(
        [FromBody] CreateEventRequest request)
    {
        var result = await _eventService.CreateEventAsync(request);
        return CreatedAtAction(nameof(GetEvent), new { id = result.Id }, result);
    }
}
```

### Error Handling

Use domain exceptions:

```csharp
// Good
if (event == null)
    throw new NotFoundException("ActivityEvent", id);

// Bad
if (event == null)
    return NotFound();
```

### Validation

Use FluentValidation:

```csharp
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Type is required");

        RuleFor(x => x.Payload)
            .NotNull()
            .WithMessage("Payload cannot be null");
    }
}
```

### Async/Await

Use async throughout:

```csharp
// Good
public async Task<Event> GetEventAsync(Guid id)
{
    return await _dbContext.Events.FindAsync(id);
}

// Bad
public Event GetEvent(Guid id)
{
    return _dbContext.Events.Find(id);
}
```

## Testing Guidelines

### Unit Tests

Test business logic in isolation:

```csharp
[Fact]
public void ActivityEvent_Constructor_SetsProperties()
{
    // Arrange
    var type = "test_event";
    var source = "test_source";

    // Act
    var event = new ActivityEvent(Guid.Empty, Guid.Empty, type, source);

    // Assert
    Assert.Equal(type, event.Type);
    Assert.Equal(source, event.Source);
}
```

### Integration Tests

Test full HTTP pipeline:

```csharp
public class EventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EventsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEvent_ValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Type = "test",
            Source = "test",
            Payload = new { test = true }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

## Debugging

### Visual Studio

1. Set `ChronicleHub.Api` as startup project
2. Press F5 to debug
3. Set breakpoints in controllers/services

### VS Code

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/ChronicleHub.Api/bin/Debug/net8.0/ChronicleHub.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/ChronicleHub.Api",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

### Logging

Logs are written to console in JSON format:

```bash
# View logs
dotnet run | jq .

# Filter by level
dotnet run | jq 'select(.@l == "Error")'

# Follow correlation ID
dotnet run | jq 'select(.CorrelationId == "abc-123")'
```

## Adding New Features

### 1. Add Domain Entity

```csharp
// src/ChronicleHub.Domain/Entities/NewEntity.cs
public class NewEntity
{
    private NewEntity() { }

    public NewEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
}
```

### 2. Add EF Core Configuration

```csharp
// src/ChronicleHub.Infrastructure/Persistence/Configurations/NewEntityConfiguration.cs
builder.Entity<NewEntity>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).IsRequired();
});
```

### 3. Create Migration

```bash
dotnet ef migrations add AddNewEntity \
  --project src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj \
  --startup-project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### 4. Add Controller

```csharp
// src/ChronicleHub.Api/Controllers/NewController.cs
[ApiController]
[Route("api/[controller]")]
public class NewController : ControllerBase
{
    // Implementation
}
```

### 5. Add Tests

```csharp
// tests/ChronicleHub.Api.Tests/Controllers/NewControllerTests.cs
public class NewControllerTests
{
    // Unit tests
}
```

## Troubleshooting

### Build Errors

```bash
# Clean build
dotnet clean
dotnet build

# Restore packages
dotnet restore
```

### Database Issues

```bash
# Delete database and recreate
rm chroniclehub.db
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

### Port Already in Use

```bash
# Change port
export Urls="http://localhost:5001"
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

## Resources

- [CLAUDE.md](../CLAUDE.md) - AI development guide
- [Reasoning.md](../Reasoning.md) - Decision history
- [Architecture Overview](architecture/overview.md) - System design
- [API Documentation](api/endpoints.md) - Endpoint reference

## Contributing

See project README for contribution guidelines.
