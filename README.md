# ChronicleHub

[![CI/CD Pipeline](https://github.com/JoshLikesToCode/ChronicleHub/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/JoshLikesToCode/ChronicleHub/actions/workflows/ci-cd.yml)
[![codecov](https://codecov.io/gh/JoshLikesToCode/ChronicleHub/branch/main/graph/badge.svg)](https://codecov.io/gh/JoshLikesToCode/ChronicleHub)
[![Docker Image](https://ghcr-badge.egpl.dev/joshlikestocode/chroniclehub/latest_tag?trim=major&label=latest)](https://github.com/JoshLikesToCode/ChronicleHub/pkgs/container/chroniclehub)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> **Production-ready cloud-native analytics platform built with .NET 8 and Clean Architecture**

ChronicleHub is an event-sourced analytics API that ingests user activity events from multiple sources, automatically computes derived statistics, and provides real-time analytics through RESTful endpoints. Designed for scalability, observability, and production deployment on Kubernetes.

## ✨ Key Features

- **Event Ingestion** - Capture user activity with flexible JSON payloads
- **Real-time Statistics** - Automatic computation of daily and category aggregations
- **Clean Architecture** - Domain-driven design with clear separation of concerns
- **RFC 9457 Error Handling** - Standardized Problem Details for HTTP APIs
- **Production Observability** - Structured logging (Serilog), OpenTelemetry tracing, correlation IDs
- **Kubernetes-Ready** - Health probes, graceful shutdown, 12-factor configuration
- **Multi-Database Support** - SQLite (dev) and PostgreSQL (prod) with auto-detection
- **API Key Authentication** - Secure write operations, public read access
- **FluentValidation** - Strongly-typed request validation with detailed error messages
- **Docker & Kubernetes** - Containerized deployment with production-ready manifests
- **Helm Chart** - One-command cloud deployment with production defaults

## 🚀 Quick Start

### Run Locally (5 minutes)

```bash
# Clone and run
git clone https://github.com/JoshLikesToCode/ChronicleHub.git
cd ChronicleHub
dotnet run --project src/ChronicleHub.Api/ChronicleHub.Api.csproj
```

**Access Swagger:** http://localhost:5000/swagger

**API Key:** `dev-chronicle-hub-key-12345`

### Run with Docker

```bash
docker build -t chroniclehub-api .
docker run -p 8080:8080 chroniclehub-api
```

**Access at:** http://localhost:8080/swagger

### Run on Kubernetes with Helm (Minikube) - RECOMMENDED

```bash
minikube start --driver=docker
minikube image build -t chroniclehub-api:latest .
helm install chroniclehub ./helm/chroniclehub
kubectl port-forward svc/chroniclehub 8080:8080
```

**Access at:** http://localhost:8080/health/ready

One-command deployment with production-ready defaults, health checks, and persistence. See [Helm Deployment Guide](docs/deployment/helm.md) for details.

### Run on Kubernetes (Raw Manifests)

```bash
minikube start --driver=docker
minikube image build -t chroniclehub-api:latest .
kubectl apply -f k8s/
minikube service chroniclehub-api --url
```

See [Kubernetes Deployment Guide](docs/deployment/kubernetes.md) for details.

## 📊 Example Usage

### Create an Event

```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-chronicle-hub-key-12345" \
  -d '{
    "Type": "user_login",
    "Source": "WebApp",
    "Payload": {
      "userId": "user-123",
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
  "Payload": { "userId": "user-123", "loginMethod": "email" },
  "CreatedAtUtc": "2025-12-20T10:30:00Z",
  "ReceivedAtUtc": "2025-12-20T10:30:00Z",
  "ProcessingDurationMs": 23.5
}
```

### Query Statistics

```bash
curl http://localhost:5000/api/stats/daily/2025-12-20
```

**Response:**
```json
{
  "Data": {
    "Date": "2025-12-20",
    "TotalEvents": 42,
    "CategoryBreakdown": [
      { "Category": "user_login", "EventCount": 15 },
      { "Category": "page_view", "EventCount": 20 },
      { "Category": "purchase", "EventCount": 7 }
    ]
  }
}
```

More examples in [API Examples](docs/api/examples.md).

## 🏗️ Architecture

ChronicleHub follows **Clean Architecture** principles with strict dependency rules:

```
┌─────────────────────────────────────────┐
│              API Layer                  │  Controllers, DTOs, Middleware
│  ┌──────────────────────────────────┐  │
│  │      Infrastructure Layer        │  │  EF Core, Repositories
│  │  ┌────────────────────────────┐  │  │
│  │  │    Application Layer       │  │  │  Services, Business Logic
│  │  │  ┌──────────────────────┐  │  │  │
│  │  │  │   Domain Layer       │  │  │  │  Entities, Exceptions
│  │  │  │  (No Dependencies)   │  │  │  │
│  │  │  └──────────────────────┘  │  │  │
│  │  └────────────────────────────┘  │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Key Design Patterns:**
- **Event Sourcing** - Immutable event store with derived statistics
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Loose coupling, testable components
- **Middleware Pipeline** - Cross-cutting concerns (auth, logging, error handling)

See [Architecture Overview](docs/architecture/overview.md) for details.

## 🛠️ Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Framework** | .NET 8 | Latest LTS, high performance, AOT-ready |
| **API** | ASP.NET Core | RESTful endpoints, OpenAPI/Swagger |
| **ORM** | Entity Framework Core | Code-first migrations, LINQ queries |
| **Database** | PostgreSQL / SQLite | Production / Development |
| **Validation** | FluentValidation | Strongly-typed validation rules |
| **Logging** | Serilog | Structured JSON logging |
| **Tracing** | OpenTelemetry | Distributed tracing, APM-ready |
| **Containers** | Docker | Multi-stage builds, non-root user |
| **Orchestration** | Kubernetes + Helm | Production-grade deployment, package manager |

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **Getting Started** | |
| [Configuration Guide](docs/configuration.md) | Environment variables and settings |
| [API Endpoints](docs/api/endpoints.md) | Complete API reference |
| [API Examples](docs/api/examples.md) | Practical usage examples |
| [Authentication](docs/api/authentication.md) | API key setup and security |
| **Deployment** | |
| [Docker Deployment](docs/deployment/docker.md) | Container deployment guide |
| [Helm Deployment](docs/deployment/helm.md) | Production-ready Helm chart (recommended) |
| [Kubernetes Deployment](docs/deployment/kubernetes.md) | K8s manifests and best practices |
| **Architecture** | |
| [Architecture Overview](docs/architecture/overview.md) | System design and patterns |
| [Error Handling (RFC 9457)](docs/architecture/error-handling.md) | Standardized error responses |
| **Development** | |
| [Development Guide](docs/development.md) | Local development setup |
| [CI/CD Pipeline](docs/ci-cd.md) | GitHub Actions workflow and automation |
| [CLAUDE.md](CLAUDE.md) | AI-assisted development guide |
| [Reasoning.md](Reasoning.md) | Development decision history |

## 🎯 Production Features

### Observability

- **Structured Logging**: JSON logs with correlation IDs (Serilog)
- **Distributed Tracing**: OpenTelemetry instrumentation for ASP.NET Core and EF Core
- **Health Checks**: Liveness (`/health/live`) and readiness (`/health/ready`) probes
- **Request Tracking**: Automatic correlation ID generation and propagation
- **Performance Metrics**: Request duration tracking in response metadata

### Security

- **API Key Authentication**: Secure write operations
- **Non-Root Container**: Runs as `appuser` for security
- **Input Validation**: FluentValidation with comprehensive rules
- **Parameterized Queries**: SQL injection prevention via EF Core
- **HTTPS Ready**: TLS support for production deployments

### Scalability

- **Stateless Design**: Horizontal scaling on Kubernetes
- **Async/Await**: Non-blocking I/O throughout
- **Connection Pooling**: Efficient database connections
- **Pagination**: Large result set handling
- **Database Indexing**: Optimized query performance

### Reliability

- **Graceful Shutdown**: 30-second timeout for in-flight requests
- **Health Probes**: Kubernetes liveness and readiness checks
- **Automatic Migrations**: Database schema updates on startup
- **Error Recovery**: RFC 9457 Problem Details with context
- **Correlation IDs**: End-to-end request tracking

## 🧪 Testing

```bash
dotnet test
```

**Test Coverage:** 128 tests across all layers
- Domain: 27 tests (entities, exceptions)
- Application: 20 tests (services, Problem Details)
- API Unit: 66 tests (controllers, middleware, validators)
- Integration: 15 tests (end-to-end scenarios)

## 🗺️ API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/events` | Create event | ✅ API Key |
| GET | `/api/events/{id}` | Get event by ID | ❌ Public |
| GET | `/api/events` | Query events (paginated) | ❌ Public |
| GET | `/api/stats/daily/{date}` | Get daily statistics | ❌ Public |
| GET | `/health/live` | Liveness probe | ❌ Public |
| GET | `/health/ready` | Readiness probe | ❌ Public |

See [API Documentation](docs/api/endpoints.md) for detailed reference.

## 🔧 Configuration

All configuration via environment variables (12-factor app):

```bash
export ConnectionStrings__DefaultConnection="Host=postgres;Database=chroniclehub;..."
export ApiKey__Key="your-secret-api-key"
export Swagger__Enabled=true
export ASPNETCORE_ENVIRONMENT=Development
```

See [Configuration Guide](docs/configuration.md) for complete reference.

## 📦 Project Structure

```
ChronicleHub/
├── src/
│   ├── ChronicleHub.Domain/          # Entities, exceptions
│   ├── ChronicleHub.Application/     # Services, Problem Details
│   ├── ChronicleHub.Infrastructure/  # EF Core, repositories
│   └── ChronicleHub.Api/             # Controllers, middleware
├── tests/                            # 128 unit + integration tests
├── docs/                             # Comprehensive documentation
├── helm/                             # Helm chart for production deployment
├── k8s/                              # Raw Kubernetes manifests
└── samples/                          # Example event JSON files
```

## 🚧 Roadmap

- [ ] JWT-based authentication with multi-tenant support
- [ ] Time-series analytics endpoints
- [ ] Real-time statistics via SignalR
- [ ] Event replay and reprocessing
- [ ] Redis caching layer
- [ ] Grafana dashboards
- [ ] Background job processing (Hangfire)

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🤝 Contributing

This project demonstrates production-ready .NET development practices and is maintained as a portfolio piece. Built with AI-assisted workflows using [Claude Code](https://claude.ai/code).

---

**Built with:** .NET 8 • Clean Architecture • Event Sourcing • Kubernetes • Docker • PostgreSQL • OpenTelemetry

