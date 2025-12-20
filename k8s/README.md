# Kubernetes Manifests for ChronicleHub

This directory contains Kubernetes manifests for deploying ChronicleHub to a local cluster (minikube) or managed Kubernetes services.

## Files

### deployment.yaml
Main application deployment with:
- **2 replicas** for high availability testing
- **Resource limits**: 500m CPU, 512Mi memory
- **Resource requests**: 100m CPU, 128Mi memory
- **Health probes**:
  - Liveness: `/health/live` (checks if app is running)
  - Readiness: `/health/ready` (checks database connectivity)
- **Environment variables**: From ConfigMap and Secret
- **EmptyDir volume**: `/app/data` for SQLite database (ephemeral, not shared between pods)

### service.yaml
NodePort service for local access:
- **Type**: NodePort (for local development)
- **Port**: 8080 (internal cluster port)
- **NodePort**: 30080 (fixed external port for easy access)
- **Target Port**: 8080 (container port)

For production, change to `type: LoadBalancer` or use an Ingress.

### configmap.yaml
Non-sensitive configuration:
- **connection-string**: SQLite database path (`/app/data/chroniclehub.db`)
- **swagger-enabled**: Set to `"true"` for local testing
- **service-name**: Service identifier for logging/telemetry
- **log-level**: Logging verbosity

### secret.yaml
Sensitive configuration (base64 encoded):
- **api-key**: API key for write operations
  - Default value: `local-dev-key-12345` (base64: `bG9jYWwtZGV2LWtleS0xMjM0NQ==`)

**WARNING**: This is a placeholder for local development. DO NOT commit real secrets to version control.

For production, use:
- [Sealed Secrets](https://github.com/bitnami-labs/sealed-secrets)
- [External Secrets Operator](https://external-secrets.io/)
- [HashiCorp Vault](https://www.vaultproject.io/)
- Cloud provider secret managers (AWS Secrets Manager, Azure Key Vault, GCP Secret Manager)

## Quick Start

```bash
# Start minikube
minikube start --driver=docker

# Build image inside minikube
minikube image build -t chroniclehub-api:latest .

# Deploy all resources
kubectl apply -f k8s/

# Wait for pods to be ready
kubectl get pods -w

# Get service URL
minikube service chroniclehub-api --url
```

## Configuration Changes

### Using PostgreSQL Instead of SQLite

Update `configmap.yaml`:
```yaml
data:
  connection-string: "Host=postgres-service;Database=chroniclehub;Username=app;Password=secret"
```

Or better yet, move the connection string to `secret.yaml`:
```yaml
data:
  api-key: bG9jYWwtZGV2LWtleS0xMjM0NQ==
  db-connection: <base64-encoded-postgres-connection-string>
```

Then update `deployment.yaml` to reference the secret:
```yaml
- name: ConnectionStrings__DefaultConnection
  valueFrom:
    secretKeyRef:
      name: chroniclehub-secret
      key: db-connection
```

### Changing Resource Limits

Edit `deployment.yaml`:
```yaml
resources:
  requests:
    cpu: 200m      # Increase for higher throughput
    memory: 256Mi
  limits:
    cpu: 1000m     # 1 CPU core
    memory: 1Gi
```

### Scaling Replicas

```bash
# Scale to 3 replicas
kubectl scale deployment chroniclehub-api --replicas=3

# Or edit deployment.yaml and reapply
kubectl apply -f k8s/deployment.yaml
```

**Note**: With SQLite and emptyDir volumes, each replica has its own database. For shared state, use PostgreSQL or persistent volumes.

## Production Considerations

### 1. Database Strategy
Replace SQLite with PostgreSQL:
- Deploy PostgreSQL as a StatefulSet or use managed database (AWS RDS, Azure Database, etc.)
- Update connection string in Secret
- Each replica will share the same database

### 2. Persistent Storage
For SQLite in production (not recommended), use PersistentVolumeClaims:
```yaml
volumes:
- name: data
  persistentVolumeClaim:
    claimName: chroniclehub-pvc
```

### 3. Service Type
Change from NodePort to LoadBalancer or use Ingress:
```yaml
apiVersion: v1
kind: Service
metadata:
  name: chroniclehub-api
spec:
  type: LoadBalancer  # Cloud provider will create external load balancer
  # ... rest of config
```

### 4. TLS/HTTPS
Use an Ingress controller with cert-manager:
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: chroniclehub-ingress
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - api.chroniclehub.com
    secretName: chroniclehub-tls
  rules:
  - host: api.chroniclehub.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: chroniclehub-api
            port:
              number: 8080
```

### 5. Monitoring
Add Prometheus annotations for metrics scraping:
```yaml
metadata:
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "8080"
    prometheus.io/path: "/metrics"
```

### 6. Horizontal Pod Autoscaling
Create an HPA based on CPU/memory:
```bash
kubectl autoscale deployment chroniclehub-api \
  --cpu-percent=70 \
  --min=2 \
  --max=10
```

## Troubleshooting

### Pods in CrashLoopBackOff
```bash
# Check logs
kubectl logs <pod-name>

# Describe pod for events
kubectl describe pod <pod-name>

# Common issues:
# 1. Database connection failed - check connection string
# 2. Missing volume mount - check deployment.yaml volumes section
# 3. Incorrect environment variables - check configmap/secret
```

### Service Not Accessible
```bash
# Check service endpoints
kubectl get endpoints chroniclehub-api

# Ensure pods are ready
kubectl get pods

# Test from inside cluster
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://chroniclehub-api:8080/health/live
```

### ImagePullBackOff
```bash
# For minikube, ensure you built the image inside minikube's Docker daemon
minikube image build -t chroniclehub-api:latest .

# List images in minikube
minikube image ls | grep chroniclehub
```

## Viewing Resources

```bash
# All resources
kubectl get all

# Specific resources
kubectl get deployments
kubectl get pods
kubectl get services
kubectl get configmaps
kubectl get secrets

# Detailed information
kubectl describe deployment chroniclehub-api
kubectl describe pod <pod-name>

# Logs
kubectl logs <pod-name>
kubectl logs -l app=chroniclehub --tail=50 -f  # Follow logs from all pods
```

## Cleanup

```bash
# Delete all ChronicleHub resources
kubectl delete -f k8s/

# Or delete individually
kubectl delete deployment chroniclehub-api
kubectl delete service chroniclehub-api
kubectl delete configmap chroniclehub-config
kubectl delete secret chroniclehub-secret
```
