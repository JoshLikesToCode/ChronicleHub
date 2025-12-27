# Docker Deployment Guide

This guide covers deploying ChronicleHub using Docker containers.

## Prerequisites

- [Docker](https://www.docker.com/get-started) installed and running

## Quick Start

### Building the Image

```bash
# From the project root
docker build -t chroniclehub-api .
```

The build process:
1. Uses multi-stage build for optimal image size
2. Restores dependencies in a separate layer (cached)
3. Compiles the application in Release mode
4. Creates final runtime image based on `mcr.microsoft.com/dotnet/aspnet:8.0`
5. Runs as non-root user (`appuser`) for security

### Running the Container

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/chroniclehub.db" \
  -e Jwt__Secret="development-secret-key-minimum-32-chars-long" \
  -e Jwt__ExpiresInMinutes=60 \
  -e Swagger__Enabled=true \
  -v $(pwd)/data:/data \
  chroniclehub-api
```

**Access the API:** http://localhost:8080

**Access Swagger UI:** http://localhost:8080/swagger

## Configuration

All configuration is done via environment variables. See [Configuration Guide](../configuration.md) for details.

### Common Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Database connection | `Data Source=/data/chroniclehub.db` |
| `Jwt__Secret` | JWT signing secret (required) | `$(openssl rand -base64 48)` |
| `Jwt__ExpiresInMinutes` | Access token lifetime | `15` (prod), `60` (dev) |
| `Jwt__RefreshTokenLifetimeDays` | Refresh token lifetime | `7` (prod), `30` (dev) |
| `Swagger__Enabled` | Enable Swagger UI | `true` or `false` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development`, `Production` |

### Example: SQLite with Persistent Volume

```bash
docker run -d \
  --name chroniclehub-api \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/chroniclehub.db" \
  -e Jwt__Secret="$(openssl rand -base64 48)" \
  -e Jwt__ExpiresInMinutes=15 \
  -e Jwt__RefreshTokenLifetimeDays=7 \
  -e Swagger__Enabled=false \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v chroniclehub-data:/data \
  --restart unless-stopped \
  chroniclehub-api
```

### Example: PostgreSQL Database

```bash
docker run -d \
  --name chroniclehub-api \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres-host;Database=chroniclehub;Username=app;Password=secret" \
  -e Jwt__Secret="$(openssl rand -base64 48)" \
  -e Jwt__ExpiresInMinutes=15 \
  -e Jwt__RefreshTokenLifetimeDays=7 \
  -e Swagger__Enabled=false \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --restart unless-stopped \
  chroniclehub-api
```

## Docker Compose

Create a `docker-compose.yml` file for local development:

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=chroniclehub;Username=app;Password=dev123
      - Jwt__Secret=development-secret-key-minimum-32-chars-long-do-not-use-in-prod
      - Jwt__ExpiresInMinutes=60
      - Jwt__RefreshTokenLifetimeDays=30
      - Swagger__Enabled=true
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - postgres
    volumes:
      - ./logs:/app/logs

  postgres:
    image: postgres:16-alpine
    environment:
      - POSTGRES_DB=chroniclehub
      - POSTGRES_USER=app
      - POSTGRES_PASSWORD=dev123
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  postgres-data:
```

Run with:
```bash
docker-compose up -d
```

## Health Checks

The Docker image includes health check endpoints:

- **Liveness**: `GET /health/live` - Returns 200 if the application is running
- **Readiness**: `GET /health/ready` - Returns 200 if the app can accept traffic (database connected)

Add health checks to your Docker run command:

```bash
docker run -d \
  --name chroniclehub-api \
  --health-cmd="curl -f http://localhost:8080/health/live || exit 1" \
  --health-interval=30s \
  --health-timeout=5s \
  --health-retries=3 \
  -p 8080:8080 \
  chroniclehub-api
```

## Viewing Logs

```bash
# Follow logs
docker logs -f chroniclehub-api

# View last 100 lines
docker logs --tail 100 chroniclehub-api

# View logs with timestamps
docker logs -t chroniclehub-api
```

Logs are output in structured JSON format (Serilog with CompactJsonFormatter).

## Troubleshooting

### Container Exits Immediately

Check logs:
```bash
docker logs chroniclehub-api
```

Common issues:
- Missing required environment variables (Jwt__Secret)
- Invalid database connection string
- Port 8080 already in use

### Database Connection Errors

If using SQLite, ensure the volume mount is correct:
```bash
docker run -v $(pwd)/data:/data ...
```

If using PostgreSQL, verify:
- Database server is accessible from container
- Connection string is correct
- Database exists and user has permissions

### Permission Denied Errors

The container runs as non-root user `appuser`. Ensure mounted volumes have correct permissions:

```bash
# For SQLite data directory
mkdir -p data
chmod 777 data  # Or use specific UID/GID matching container
```

## Production Considerations

### Security

1. **Never expose Swagger in production**:
   ```bash
   -e Swagger__Enabled=false
   ```

2. **Use strong JWT secrets**:
   ```bash
   -e Jwt__Secret="$(openssl rand -base64 48)"
   ```

3. **Use environment-specific JWT token lifetimes**:
   ```bash
   # Production (short-lived tokens)
   -e Jwt__ExpiresInMinutes=15
   -e Jwt__RefreshTokenLifetimeDays=7

   # Development (longer for convenience)
   -e Jwt__ExpiresInMinutes=60
   -e Jwt__RefreshTokenLifetimeDays=30
   ```

4. **Run with read-only root filesystem** (if using PostgreSQL):
   ```bash
   --read-only \
   --tmpfs /tmp \
   --tmpfs /app/logs
   ```

### Resource Limits

Set memory and CPU limits:

```bash
docker run -d \
  --memory="512m" \
  --memory-swap="512m" \
  --cpus="0.5" \
  chroniclehub-api
```

### Logging

For production, consider log aggregation:

**Using Docker logging driver:**
```bash
docker run -d \
  --log-driver=json-file \
  --log-opt max-size=10m \
  --log-opt max-file=3 \
  chroniclehub-api
```

**Or use a log aggregation service** (Splunk, ELK, CloudWatch, etc.)

## Next Steps

- [Kubernetes Deployment](kubernetes.md) - Deploy to Kubernetes clusters
- [Production Deployment](production.md) - Production best practices
- [Configuration Guide](../configuration.md) - Detailed configuration options
