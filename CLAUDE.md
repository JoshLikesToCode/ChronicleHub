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

**Supported Databases:**
- **SQLite**: Default for local development and demos
- **PostgreSQL**: Recommended for production deployments
- **SQL Server**: Supported for Azure SQL or on-premise deployments

**Migration Strategies:**

ChronicleHub supports three migration strategies, controlled via configuration:

1. **Startup Migrations** (Default for Development)
   - Migrations run automatically when the application starts
   - Enabled by default (`Database:RunMigrationsOnStartup=true`)
   - **Use for**: Local development, demos, single-instance deployments
   - **Avoid for**: Production with multiple replicas (race condition risk)

2. **Init Container** (Recommended for Production)
   - Migrations run in an init container before app containers start
   - Prevents race conditions within a pod
   - Enabled via Helm: `database.migrations.initContainer.enabled=true`
   - **Use for**: Production deployments with auto-scaling
   - See `helm/chroniclehub/values-postgres-initcontainer.yaml`

3. **Job-Based Migrations** (Best for Complex Migrations)
   - Migrations run as a separate Kubernetes Job (Helm hook)
   - Runs once before install/upgrade
   - Better visibility and manual control
   - Enabled via Helm: `database.migrations.job.enabled=true`
   - **Use for**: Production with complex migrations, manual control needed
   - See `helm/chroniclehub/values-postgres-job.yaml` or `values-sqlserver-aks.yaml`

**Configuration:**
- Startup migrations: `Database:RunMigrationsOnStartup` (default: true)
- Connection string: `ConnectionStrings:DefaultConnection` (or Kubernetes secret)
- See Program.cs:142-158 for migration logic

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

### Error Handling (RFC 9457 Problem Details)
The API implements RFC 9457 Problem Details for HTTP APIs for consistent error responses:
- **Global Middleware**: `ProblemDetailsExceptionMiddleware` catches all exceptions and converts them to Problem Details responses
- **Content-Type**: All error responses use `application/problem+json`
- **Domain Exceptions**: Located in `ChronicleHub.Domain/Exceptions/`
  - `NotFoundException` → 404 Not Found
  - `ValidationException` → 400 Bad Request (with validation errors)
  - `ConflictException` → 409 Conflict
  - `UnauthorizedException` → 401 Unauthorized
  - `ForbiddenException` → 403 Forbidden
- **Factory Methods**: Use `ProblemDetailsFactory` in `ChronicleHub.Application/ProblemDetails/` to create responses
- **Environment-Aware**: Development mode includes stack traces and exception details; Production mode shows generic messages
- **Extensions**: Problem Details can include custom extension members (e.g., `resourceName`, `resourceKey`, validation `errors`)

To throw a domain exception from a controller:
```csharp
throw new NotFoundException("ActivityEvent", id);
```

The middleware automatically converts this to:
```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Not Found",
  "status": 404,
  "detail": "ActivityEvent with key '...' was not found.",
  "instance": "/api/events/...",
  "resourceName": "ActivityEvent",
  "resourceKey": "..."
}
```

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

### Kubernetes (Minikube)
```bash
# Start minikube with Docker driver
minikube start --driver=docker

# Build image inside minikube's Docker daemon
minikube image build -t chroniclehub-api:latest .

# Deploy all Kubernetes resources
kubectl apply -f k8s/

# Check deployment status
kubectl get pods
kubectl get all

# View logs
kubectl logs -l app=chroniclehub --tail=50 -f

# Get service URL (keeps tunnel open)
minikube service chroniclehub-api --url

# Alternative: Port-forward
kubectl port-forward service/chroniclehub-api 8080:8080

# Rebuild after code changes
minikube image build -t chroniclehub-api:latest .
kubectl rollout restart deployment/chroniclehub-api
kubectl rollout status deployment/chroniclehub-api

# Cleanup
kubectl delete -f k8s/
minikube stop
minikube delete  # Optional: removes everything
```

### Helm Deployment (Minikube) - RECOMMENDED
Helm provides a streamlined, production-ready deployment with one command:

```bash
# Prerequisites: Install Helm (if not already installed)
# Visit https://helm.sh/docs/intro/install/ or use your package manager

# Start minikube with Docker driver
minikube start --driver=docker

# Build image inside minikube's Docker daemon
minikube image build -t chroniclehub-api:latest .

# Install ChronicleHub with Helm (one command deployment!)
helm install chroniclehub ./helm/chroniclehub

# Check deployment status
kubectl get all -l app.kubernetes.io/name=chroniclehub
kubectl get pods -l app.kubernetes.io/name=chroniclehub

# View logs
kubectl logs -l app.kubernetes.io/name=chroniclehub --tail=50 -f

# Test the API (port-forward to access locally)
kubectl port-forward svc/chroniclehub 8080:8080

# Test health endpoint
curl http://localhost:8080/health/ready

# Upgrade after code changes
minikube image build -t chroniclehub-api:latest .
helm upgrade chroniclehub ./helm/chroniclehub

# Check upgrade status
kubectl rollout status deployment/chroniclehub

# Uninstall
helm uninstall chroniclehub

# Cleanup minikube
minikube stop
minikube delete  # Optional: removes everything
```

**Helm Chart Features:**
- Production-ready defaults (Production environment, proper health checks)
- Multiple database migration strategies (startup, init container, job)
- Multi-database support (SQLite, PostgreSQL, SQL Server)
- Configurable replicas, resources, and environment variables
- Liveness probe at `/health/live`
- Readiness probe at `/health/ready` (checks DB connectivity)
- Service Account creation
- Ingress support (disabled by default, can be enabled in values.yaml)

**Customizing the Deployment:**
Edit `helm/chroniclehub/values.yaml` to customize:
- Replica count
- Resource limits/requests
- Database configuration and migration strategy
- Environment variables
- Persistence settings
- Health check intervals
- Ingress configuration

Or override values via command line:
```bash
helm install chroniclehub ./helm/chroniclehub \
  --set replicaCount=3 \
  --set resources.limits.memory=1Gi \
  --set ingress.enabled=true
```

### Production Deployment with PostgreSQL/SQL Server

For production deployments on AKS or other cloud platforms, use one of the provided production values files:

**PostgreSQL with Init Container (Recommended):**
```bash
# Create database secret first
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Host=YOUR_POSTGRES_HOST;Database=chroniclehub;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require"

# Deploy with init container migration strategy
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-initcontainer.yaml
```

**PostgreSQL with Job-based Migrations:**
```bash
# Create database secret first
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Host=YOUR_POSTGRES_HOST;Database=chroniclehub;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require"

# Deploy with job migration strategy (runs as Helm hook)
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-job.yaml
```

**Azure SQL Server on AKS:**
```bash
# Create database secret first
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Server=YOURSERVER.database.windows.net;Database=chroniclehub;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True"

# Deploy with job migration strategy
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-sqlserver-aks.yaml
```

**Migration Strategy Comparison:**

| Strategy | When to Use | Pros | Cons |
|----------|-------------|------|------|
| **Startup** (default) | Local dev, demos | Simple, automatic | Race conditions with multiple replicas |
| **Init Container** | Production, auto-scaling | No race conditions per pod, automatic | Runs on every pod start |
| **Job** | Production, complex migrations | Runs once, better visibility, manual control | Requires Helm, more complex troubleshooting |

**Configuration Options:**
- `database.migrations.runOnStartup`: Enable/disable startup migrations (default: true)
- `database.migrations.initContainer.enabled`: Enable init container strategy (default: false)
- `database.migrations.job.enabled`: Enable job-based strategy (default: false)
- `database.connectionStringSecretName`: Use Kubernetes secret for DB credentials (recommended)
- `database.connectionString`: Inline connection string (not recommended for production)

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
