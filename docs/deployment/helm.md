# Helm Deployment Guide

This guide covers deploying ChronicleHub to Kubernetes using Helm, the package manager for Kubernetes. Helm provides a streamlined, production-ready deployment with one command.

## Why Helm?

Helm offers several advantages over raw Kubernetes manifests:

- **One-command deployment**: `helm install chroniclehub ./helm/chroniclehub`
- **Version management**: Track and rollback releases easily
- **Configuration management**: Centralized values with easy overrides
- **Templating**: Reusable templates with conditionals and loops
- **Dependency management**: Manage chart dependencies
- **Production-ready defaults**: Pre-configured for production use

## Prerequisites

1. **Kubernetes cluster** (minikube, kind, GKE, EKS, AKS, etc.)
2. **Helm 3.x** installed ([installation guide](https://helm.sh/docs/intro/install/))
3. **kubectl** configured to access your cluster
4. **Docker image** built and available to the cluster

## Quick Start (Minikube)

```bash
# Start minikube
minikube start --driver=docker

# Build the Docker image inside minikube
minikube image build -t chroniclehub-api:latest .

# Install ChronicleHub with Helm
helm install chroniclehub ./helm/chroniclehub

# Wait for the pod to be ready
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=chroniclehub --timeout=120s

# Port-forward to access locally
kubectl port-forward svc/chroniclehub 8080:8080

# Test the health endpoint
curl http://localhost:8080/health/ready
```

**Expected response:**
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "2025-12-20T10:30:00Z"
}
```

## Chart Structure

```
helm/chroniclehub/
├── Chart.yaml                    # Chart metadata
├── values.yaml                   # Default configuration values
└── templates/
    ├── _helpers.tpl              # Template helpers
    ├── deployment.yaml           # Deployment resource
    ├── service.yaml              # Service resource
    ├── ingress.yaml              # Ingress resource (optional)
    ├── pvc.yaml                  # PersistentVolumeClaim for SQLite
    └── serviceaccount.yaml       # ServiceAccount
```

## Configuration

### Default Values

The chart comes with production-ready defaults:

- **Environment**: Production
- **Replicas**: 1
- **Resources**:
  - Requests: 250m CPU, 256Mi memory
  - Limits: 500m CPU, 512Mi memory
- **Persistence**: 1Gi PVC for SQLite database
- **Health Checks**: Configured liveness and readiness probes
- **Image**: `chroniclehub-api:latest`

### Customizing Values

#### Method 1: Edit values.yaml

Edit `helm/chroniclehub/values.yaml` directly:

```yaml
replicaCount: 3

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 500m
    memory: 512Mi

env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - name: ConnectionStrings__DefaultConnection
    value: "Host=postgres;Database=chroniclehub;..."
```

Then upgrade the release:
```bash
helm upgrade chroniclehub ./helm/chroniclehub
```

#### Method 2: Override via CLI

```bash
helm install chroniclehub ./helm/chroniclehub \
  --set replicaCount=3 \
  --set resources.limits.memory=1Gi \
  --set ingress.enabled=true
```

#### Method 3: Custom values file

Create a custom values file (e.g., `production-values.yaml`):

```yaml
replicaCount: 3

resources:
  limits:
    cpu: 1000m
    memory: 1Gi

env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: chroniclehub-secrets
        key: database-connection-string

ingress:
  enabled: true
  className: "nginx"
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  hosts:
    - host: chroniclehub.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: chroniclehub-tls
      hosts:
        - chroniclehub.example.com
```

Install with custom values:
```bash
helm install chroniclehub ./helm/chroniclehub -f production-values.yaml
```

## Common Operations

### Install

```bash
helm install chroniclehub ./helm/chroniclehub
```

### Upgrade

```bash
helm upgrade chroniclehub ./helm/chroniclehub
```

### Upgrade with new values

```bash
helm upgrade chroniclehub ./helm/chroniclehub -f production-values.yaml
```

### Rollback

```bash
# List releases
helm history chroniclehub

# Rollback to previous version
helm rollback chroniclehub

# Rollback to specific revision
helm rollback chroniclehub 3
```

### Uninstall

```bash
helm uninstall chroniclehub
```

### Dry Run (Test without installing)

```bash
helm install chroniclehub ./helm/chroniclehub --dry-run --debug
```

### Template Rendering (View generated manifests)

```bash
helm template chroniclehub ./helm/chroniclehub
```

## Health Checks

The chart configures two types of health probes:

### Liveness Probe
- **Endpoint**: `/health/live`
- **Purpose**: Determines if the pod should be restarted
- **Configuration**:
  - Initial delay: 30 seconds
  - Period: 10 seconds
  - Timeout: 5 seconds
  - Failure threshold: 3

### Readiness Probe
- **Endpoint**: `/health/ready`
- **Purpose**: Determines if the pod should receive traffic
- **Checks**: Database connectivity
- **Configuration**:
  - Initial delay: 10 seconds
  - Period: 5 seconds
  - Timeout: 3 seconds
  - Failure threshold: 3

## Persistence

By default, the chart creates a 1Gi PersistentVolumeClaim for SQLite database storage. The database file is stored at `/app/data/chroniclehub.db` inside the container.

**Configure persistence:**

```yaml
persistence:
  enabled: true
  storageClassName: "standard"  # or your storage class
  accessMode: ReadWriteOnce
  size: 5Gi
  mountPath: /app/data
```

**Disable persistence** (not recommended for production):

```yaml
persistence:
  enabled: false
```

## Ingress Configuration

The chart includes an Ingress resource that's disabled by default.

**Enable Ingress with NGINX:**

```yaml
ingress:
  enabled: true
  className: "nginx"
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
  hosts:
    - host: chroniclehub.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: chroniclehub-tls
      hosts:
        - chroniclehub.example.com
```

**With cert-manager for automatic TLS:**

```yaml
ingress:
  enabled: true
  className: "nginx"
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  hosts:
    - host: chroniclehub.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: chroniclehub-tls
      hosts:
        - chroniclehub.example.com
```

## Production Deployment

### PostgreSQL Database

For production, use PostgreSQL instead of SQLite:

```yaml
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: chroniclehub-secrets
        key: postgres-connection-string
```

Create the secret:
```bash
kubectl create secret generic chroniclehub-secrets \
  --from-literal=postgres-connection-string="Host=postgres;Database=chroniclehub;Username=user;Password=pass"
```

### Secrets Management

Never store secrets in values.yaml. Use Kubernetes Secrets or external secret managers:

```yaml
env:
  - name: ApiKey__Key
    valueFrom:
      secretKeyRef:
        name: chroniclehub-secrets
        key: api-key
```

### High Availability

```yaml
replicaCount: 3

affinity:
  podAntiAffinity:
    preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 100
        podAffinityTerm:
          labelSelector:
            matchExpressions:
              - key: app.kubernetes.io/name
                operator: In
                values:
                  - chroniclehub
          topologyKey: kubernetes.io/hostname
```

### Resource Limits

```yaml
resources:
  limits:
    cpu: 2000m
    memory: 2Gi
  requests:
    cpu: 500m
    memory: 512Mi
```

### Autoscaling

```yaml
autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80
```

## Monitoring

### View Logs

```bash
# All pods
kubectl logs -l app.kubernetes.io/name=chroniclehub --tail=100 -f

# Specific pod
kubectl logs pod/chroniclehub-xxxxx --tail=100 -f
```

### Check Status

```bash
# Release status
helm status chroniclehub

# Pod status
kubectl get pods -l app.kubernetes.io/name=chroniclehub

# Service status
kubectl get svc chroniclehub

# Deployment status
kubectl get deployment chroniclehub
```

### Describe Resources

```bash
kubectl describe deployment chroniclehub
kubectl describe pod chroniclehub-xxxxx
kubectl describe svc chroniclehub
```

## Troubleshooting

### Pod not starting

```bash
# Check pod status
kubectl get pods -l app.kubernetes.io/name=chroniclehub

# View pod events
kubectl describe pod chroniclehub-xxxxx

# Check logs
kubectl logs chroniclehub-xxxxx
```

### Image pull errors

```bash
# For minikube, ensure image is built in minikube's Docker daemon
minikube image build -t chroniclehub-api:latest .

# For cloud providers, push to a container registry
docker tag chroniclehub-api:latest your-registry/chroniclehub-api:latest
docker push your-registry/chroniclehub-api:latest

# Update values.yaml
image:
  repository: your-registry/chroniclehub-api
  tag: latest
  pullPolicy: Always
```

### Health check failures

```bash
# Test health endpoint directly
kubectl port-forward svc/chroniclehub 8080:8080
curl http://localhost:8080/health/ready

# Check logs for errors
kubectl logs -l app.kubernetes.io/name=chroniclehub --tail=50
```

### Database issues

```bash
# Check PVC status
kubectl get pvc

# Check if volume is mounted
kubectl describe pod chroniclehub-xxxxx | grep -A 5 "Mounts:"

# Exec into pod and check database
kubectl exec -it chroniclehub-xxxxx -- /bin/bash
ls -la /app/data/
```

## Cloud Provider Specific Notes

### Google Kubernetes Engine (GKE)

```bash
# Build and push to Google Container Registry
gcloud builds submit --tag gcr.io/PROJECT_ID/chroniclehub-api:latest

# Update values.yaml
image:
  repository: gcr.io/PROJECT_ID/chroniclehub-api
  tag: latest

# Install
helm install chroniclehub ./helm/chroniclehub
```

### Amazon EKS

```bash
# Build and push to ECR
aws ecr get-login-password --region region | docker login --username AWS --password-stdin ACCOUNT_ID.dkr.ecr.region.amazonaws.com
docker tag chroniclehub-api:latest ACCOUNT_ID.dkr.ecr.region.amazonaws.com/chroniclehub-api:latest
docker push ACCOUNT_ID.dkr.ecr.region.amazonaws.com/chroniclehub-api:latest

# Update values.yaml
image:
  repository: ACCOUNT_ID.dkr.ecr.region.amazonaws.com/chroniclehub-api
  tag: latest

# Install
helm install chroniclehub ./helm/chroniclehub
```

### Azure AKS

```bash
# Build and push to ACR
az acr build --registry myregistry --image chroniclehub-api:latest .

# Update values.yaml
image:
  repository: myregistry.azurecr.io/chroniclehub-api
  tag: latest

# Install
helm install chroniclehub ./helm/chroniclehub
```

## Next Steps

- Configure Ingress for external access
- Set up monitoring with Prometheus and Grafana
- Implement GitOps with ArgoCD or Flux
- Configure backup strategy for persistent data
- Set up CI/CD pipeline for automated deployments

## See Also

- [Kubernetes Deployment Guide](kubernetes.md) - Raw Kubernetes manifests
- [Configuration Guide](../configuration.md) - Environment variables
- [Architecture Overview](../architecture/overview.md) - System design
