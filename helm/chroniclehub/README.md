# ChronicleHub Helm Chart

Production-ready Helm chart for deploying ChronicleHub to Kubernetes.

## Features

- **Multiple Database Support**: SQLite, PostgreSQL, SQL Server
- **Flexible Migration Strategies**: Startup, Init Container, or Job-based migrations
- **Production-Ready Defaults**: Health checks, resource limits, autoscaling support
- **Secret Management**: Support for Kubernetes secrets for connection strings
- **Horizontal Pod Autoscaling**: Built-in HPA configuration
- **Persistent Storage**: Optional PVC for SQLite deployments

## Quick Start

### Local Development (SQLite)

```bash
# Build image in minikube
minikube start --driver=docker
minikube image build -t chroniclehub-api:latest .

# Deploy with default values (SQLite, startup migrations)
helm install chroniclehub ./helm/chroniclehub

# Access the application
kubectl port-forward svc/chroniclehub 8080:8080
```

### Production (PostgreSQL with Init Container)

```bash
# Create database secret
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Host=postgres-host;Database=chroniclehub;Username=user;Password=pass;SSL Mode=Require"

# Create JWT secret (generate strong secret for production)
kubectl create secret generic chroniclehub-jwt-secret \
  --from-literal=jwt-secret="$(openssl rand -base64 48)"

# Deploy with production values
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-initcontainer.yaml
```

## Migration Strategies

ChronicleHub supports three migration strategies:

### 1. Startup Migrations (Default)

**Best for**: Development, demos, single-instance deployments

Migrations run when the application starts. Enabled by default.

```yaml
database:
  migrations:
    runOnStartup: true
```

**Pros**: Simple, automatic
**Cons**: Race conditions with multiple replicas

### 2. Init Container (Recommended for Production)

**Best for**: Production deployments, auto-scaling

Migrations run in an init container before app pods start. No race conditions within a pod.

```yaml
database:
  migrations:
    runOnStartup: false
    initContainer:
      enabled: true
```

**Pros**: Automatic, no race conditions per pod
**Cons**: Runs on every pod start (but migrations are idempotent)

**Example deployment**:
```bash
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-initcontainer.yaml
```

### 3. Job-Based Migrations

**Best for**: Complex migrations, manual control, CI/CD pipelines

Migrations run as a separate Kubernetes Job via Helm hooks (pre-install/pre-upgrade).

```yaml
database:
  migrations:
    runOnStartup: false
    job:
      enabled: true
      backoffLimit: 5
```

**Pros**: Runs once, better visibility, manual control
**Cons**: Requires Helm, more complex troubleshooting

**Example deployment**:
```bash
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-job.yaml
```

## Database Configuration

### SQLite (Development)

Default configuration uses SQLite with persistent volume:

```yaml
database:
  provider: sqlite
  connectionString: "Data Source=/app/data/chroniclehub.db"

persistence:
  enabled: true
  size: 1Gi
```

### PostgreSQL (Production)

**Using Kubernetes Secret (Recommended)**:
```bash
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Host=postgres-host;Database=chroniclehub;Username=user;Password=pass;SSL Mode=Require"
```

```yaml
database:
  provider: postgresql
  connectionStringSecretName: "chroniclehub-db-secret"
  connectionStringSecretKey: "connectionString"

persistence:
  enabled: false  # Not needed for external database
```

**Using Inline Connection String**:
```yaml
database:
  provider: postgresql
  connectionString: "Host=postgres-host;Database=chroniclehub;Username=user;Password=pass"
```

### SQL Server / Azure SQL

**Using Kubernetes Secret (Recommended)**:
```bash
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Server=myserver.database.windows.net;Database=chroniclehub;User Id=user;Password=pass;Encrypt=True"
```

```yaml
database:
  provider: sqlserver
  connectionStringSecretName: "chroniclehub-db-secret"

persistence:
  enabled: false  # Not needed for external database
```

## Production Deployment Examples

### Azure Kubernetes Service (AKS) with Azure SQL

```bash
# Create namespace
kubectl create namespace chroniclehub

# Create database secret
kubectl create secret generic chroniclehub-db-secret \
  --namespace chroniclehub \
  --from-literal=connectionString="Server=YOURSERVER.database.windows.net;Database=chroniclehub;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True"

# Create JWT secret
kubectl create secret generic chroniclehub-jwt-secret \
  --namespace chroniclehub \
  --from-literal=jwt-secret="$(openssl rand -base64 48)"

# Deploy
helm install chroniclehub ./helm/chroniclehub \
  --namespace chroniclehub \
  -f ./helm/chroniclehub/values-sqlserver-aks.yaml

# Watch deployment
kubectl get pods -n chroniclehub -w

# Check migration job (if using job strategy)
kubectl get jobs -n chroniclehub
kubectl logs job/chroniclehub-migration -n chroniclehub
```

### PostgreSQL with Auto-Scaling

```bash
# Create database secret
kubectl create secret generic chroniclehub-db-secret \
  --from-literal=connectionString="Host=postgres.example.com;Database=chroniclehub;Username=app_user;Password=STRONG_PASSWORD;SSL Mode=Require"

# Create JWT secret
kubectl create secret generic chroniclehub-jwt-secret \
  --from-literal=jwt-secret="$(openssl rand -base64 48)"

# Deploy with custom values
helm install chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-initcontainer.yaml \
  --set replicaCount=3 \
  --set autoscaling.enabled=true \
  --set autoscaling.minReplicas=3 \
  --set autoscaling.maxReplicas=20
```

## Configuration Reference

### Database Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `database.provider` | Database type: sqlite, postgresql, sqlserver | `sqlite` |
| `database.connectionString` | Connection string (plain text, not recommended for prod) | `Data Source=/app/data/chroniclehub.db` |
| `database.connectionStringSecretName` | Name of secret containing connection string | `""` |
| `database.connectionStringSecretKey` | Key within secret | `connectionString` |

### Migration Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `database.migrations.runOnStartup` | Run migrations on app startup | `true` |
| `database.migrations.initContainer.enabled` | Enable init container migrations | `false` |
| `database.migrations.initContainer.resources` | Resource limits for init container | See values.yaml |
| `database.migrations.job.enabled` | Enable job-based migrations | `false` |
| `database.migrations.job.backoffLimit` | Max retry attempts for migration job | `3` |
| `database.migrations.job.resources` | Resource limits for migration job | See values.yaml |

### Application Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of replicas | `1` |
| `image.repository` | Container image repository | `chroniclehub-api` |
| `image.tag` | Container image tag | `latest` |
| `resources.limits.cpu` | CPU limit | `500m` |
| `resources.limits.memory` | Memory limit | `512Mi` |
| `autoscaling.enabled` | Enable HPA | `false` |
| `autoscaling.minReplicas` | Min replicas for HPA | `1` |
| `autoscaling.maxReplicas` | Max replicas for HPA | `10` |

### Persistence Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `persistence.enabled` | Enable persistent volume | `true` |
| `persistence.size` | Size of PVC | `1Gi` |
| `persistence.accessMode` | Access mode | `ReadWriteOnce` |
| `persistence.mountPath` | Mount path in container | `/app/data` |

## Upgrading

### Application Updates

```bash
# Pull latest code/image
minikube image build -t chroniclehub-api:latest .

# Upgrade deployment
helm upgrade chroniclehub ./helm/chroniclehub

# Watch rollout
kubectl rollout status deployment/chroniclehub
```

### Database Schema Changes

When upgrading with database schema changes:

**Init Container Strategy**: Migrations run automatically on pod restart
**Job Strategy**: Migrations run automatically via pre-upgrade hook
**Startup Strategy**: Migrations run on first pod start

```bash
# Upgrade with job-based migrations
helm upgrade chroniclehub ./helm/chroniclehub \
  -f ./helm/chroniclehub/values-postgres-job.yaml

# Check migration job status
kubectl get jobs -l app.kubernetes.io/component=migration
kubectl logs job/chroniclehub-migration
```

## Troubleshooting

### Check Migration Status

**Startup migrations**:
```bash
kubectl logs -l app.kubernetes.io/name=chroniclehub --tail=100 | grep -i migration
```

**Init container migrations**:
```bash
kubectl logs <pod-name> -c migration
```

**Job-based migrations**:
```bash
kubectl get jobs
kubectl logs job/chroniclehub-migration
kubectl describe job/chroniclehub-migration
```

### Common Issues

**Migration Job Failed**:
```bash
# Check job logs
kubectl logs job/chroniclehub-migration

# Check secret exists
kubectl get secret chroniclehub-db-secret
kubectl get secret chroniclehub-db-secret -o yaml

# Manually run migration job
kubectl delete job chroniclehub-migration
helm upgrade chroniclehub ./helm/chroniclehub -f values-postgres-job.yaml
```

**Connection String Issues**:
```bash
# Verify secret content
kubectl get secret chroniclehub-db-secret -o jsonpath='{.data.connectionString}' | base64 -d

# Test connection from pod
kubectl run -it --rm debug --image=chroniclehub-api:latest --restart=Never -- bash
```

## Uninstalling

```bash
# Uninstall release
helm uninstall chroniclehub

# Delete PVC (if using persistence)
kubectl delete pvc chroniclehub-pvc

# Delete namespace (if created)
kubectl delete namespace chroniclehub
```

## Values Files

This chart includes several pre-configured values files:

- `values.yaml` - Default (SQLite, startup migrations)
- `values-postgres-initcontainer.yaml` - PostgreSQL with init container
- `values-postgres-job.yaml` - PostgreSQL with job-based migrations
- `values-sqlserver-aks.yaml` - Azure SQL Server for AKS

Use them as templates for your own deployment.
