# Kubernetes Deployment Guide

This guide covers deploying ChronicleHub to Kubernetes clusters using raw manifests, from local development with minikube to production managed Kubernetes services.

> **Recommended:** For production deployments, consider using our [Helm chart](helm.md) which provides one-command deployment with production-ready defaults, easier configuration management, and version control.

## Prerequisites

- [Docker](https://www.docker.com/get-started)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [minikube](https://minikube.sigs.k8s.io/docs/start/) (for local testing)

## Local Development with Minikube

### Quick Start

```bash
# 1. Start minikube with Docker driver
minikube start --driver=docker

# 2. Build the Docker image inside minikube's Docker daemon
minikube image build -t chroniclehub-api:latest .

# 3. Deploy all Kubernetes resources
kubectl apply -f k8s/

# 4. Wait for pods to be ready (may take 30-60 seconds)
kubectl get pods -w
# Press Ctrl+C when you see both pods showing 1/1 READY

# 5. Get the service URL (this keeps a tunnel open)
minikube service chroniclehub-api --url
# Example output: http://127.0.0.1:39377
```

**Access Swagger UI:** Navigate to the URL from step 5 + `/swagger`

**API Key:** `local-dev-key-12345` (see `k8s/secret.yaml`)

### Testing the Deployment

```bash
# Get the service URL (keep this terminal open)
SERVICE_URL=$(minikube service chroniclehub-api --url)

# In another terminal, test the API
curl -X POST $SERVICE_URL/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: local-dev-key-12345" \
  -d '{
    "Type": "PageView",
    "Source": "web-app",
    "Payload": {
      "page": "/home",
      "duration": 5000
    }
  }'
```

### Viewing Logs

```bash
# View logs from all pods
kubectl logs -l app=chroniclehub --tail=50 -f

# View logs from a specific pod
kubectl logs <pod-name>

# Get pod names
kubectl get pods
```

### Monitoring Deployment

```bash
# View all resources
kubectl get all

# Describe deployment
kubectl describe deployment chroniclehub-api

# Check pod health
kubectl get pods

# Watch pod status
kubectl get pods -w
```

### Cleanup

```bash
# Delete all ChronicleHub resources
kubectl delete -f k8s/

# Stop minikube
minikube stop

# Delete minikube cluster (optional - removes everything)
minikube delete
```

## Kubernetes Manifests

The `k8s/` directory contains production-ready configurations:

### deployment.yaml

- **2 replicas** for high availability
- **Resource limits**: 500m CPU, 512Mi memory
- **Health probes**: Liveness (`/health/live`) and Readiness (`/health/ready`)
- **Configuration**: Environment variables from ConfigMap and Secret
- **Storage**: EmptyDir volume for SQLite (not recommended for production)

### service.yaml

- **Type**: NodePort (for local development)
- **Port**: 8080 (internal cluster port)
- **NodePort**: 30080 (fixed external port)

For production, change to `type: LoadBalancer` or use an Ingress controller.

### configmap.yaml

Non-sensitive configuration:
- Database connection string
- Swagger enabled/disabled
- Service name
- Log level

### secret.yaml

Sensitive configuration (base64 encoded):
- API key for write operations

**⚠️ WARNING**: The included secret is for local development only. **DO NOT commit real secrets to version control.**

See [k8s/README.md](../../k8s/README.md) for detailed manifest documentation.

## Production Deployment

### 1. Database Strategy

**Replace SQLite with PostgreSQL or managed database:**

```yaml
# In configmap.yaml or secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: chroniclehub-secret
type: Opaque
stringData:
  api-key: "prod-secure-key-xyz789"
  db-connection: "Host=postgres-service;Database=chroniclehub;Username=app;Password=secret"
```

Update deployment to use database connection from secret:

```yaml
- name: ConnectionStrings__DefaultConnection
  valueFrom:
    secretKeyRef:
      name: chroniclehub-secret
      key: db-connection
```

### 2. Secrets Management

Use a proper secrets management solution:

**Sealed Secrets:**
```bash
# Install sealed-secrets controller
kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.24.0/controller.yaml

# Create sealed secret
kubectl create secret generic chroniclehub-secret \
  --from-literal=api-key=your-secret-key \
  --dry-run=client -o yaml | \
  kubeseal -o yaml > sealed-secret.yaml

# Apply sealed secret
kubectl apply -f sealed-secret.yaml
```

**External Secrets Operator:**
```yaml
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: chroniclehub-secret
spec:
  secretStoreRef:
    name: aws-secrets-manager
    kind: SecretStore
  target:
    name: chroniclehub-secret
  data:
  - secretKey: api-key
    remoteRef:
      key: chroniclehub/api-key
```

### 3. Service Exposure

**Option A: LoadBalancer (Cloud providers)**

```yaml
apiVersion: v1
kind: Service
metadata:
  name: chroniclehub-api
spec:
  type: LoadBalancer
  selector:
    app: chroniclehub
  ports:
  - port: 80
    targetPort: 8080
```

**Option B: Ingress with TLS**

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: chroniclehub-ingress
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/rate-limit: "100"
spec:
  ingressClassName: nginx
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

### 4. Persistent Storage (if using SQLite)

**Not recommended for production**, but if needed:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: chroniclehub-pvc
spec:
  accessModes:
  - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
---
# In deployment.yaml
volumes:
- name: data
  persistentVolumeClaim:
    claimName: chroniclehub-pvc
```

**Note**: SQLite doesn't support concurrent writes. Use PostgreSQL for multiple replicas.

### 5. Horizontal Pod Autoscaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: chroniclehub-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: chroniclehub-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### 6. Resource Limits

Adjust based on your workload:

```yaml
resources:
  requests:
    cpu: 200m
    memory: 256Mi
  limits:
    cpu: 1000m
    memory: 1Gi
```

### 7. Monitoring and Observability

**Add Prometheus annotations:**

```yaml
metadata:
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "8080"
    prometheus.io/path: "/metrics"
```

**Structured logging** is already configured (Serilog with JSON output).

## Troubleshooting

### Pods in CrashLoopBackOff

```bash
# Check pod logs
kubectl logs <pod-name>

# Describe pod for events
kubectl describe pod <pod-name>

# Common issues:
# 1. Database connection failed - check connection string in ConfigMap/Secret
# 2. Missing volume mount - check deployment.yaml volumes section
# 3. ImagePullBackOff - image not available in cluster
```

### ImagePullBackOff (Minikube)

```bash
# Ensure image was built inside minikube's Docker daemon
minikube image build -t chroniclehub-api:latest .

# List images in minikube
minikube image ls | grep chroniclehub

# Alternative: Load image into minikube
docker save chroniclehub-api:latest | minikube image load -
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

### Health Check Failures

```bash
# Check if health endpoints respond
kubectl port-forward <pod-name> 8080:8080

# In another terminal
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready

# Check database connectivity
kubectl exec -it <pod-name> -- /bin/sh
# Then test database connection
```

## Rebuild After Code Changes

```bash
# Rebuild image in minikube
minikube image build -t chroniclehub-api:latest .

# Restart deployment to use new image
kubectl rollout restart deployment/chroniclehub-api

# Watch rollout status
kubectl rollout status deployment/chroniclehub-api

# View rollout history
kubectl rollout history deployment/chroniclehub-api
```

## Cloud Provider Specific Notes

### Azure Kubernetes Service (AKS)

```bash
# Build and push to Azure Container Registry
az acr build --registry <your-acr> --image chroniclehub-api:latest .

# Update deployment to use ACR image
# In deployment.yaml:
# image: <your-acr>.azurecr.io/chroniclehub-api:latest
```

### Amazon EKS

```bash
# Build and push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
docker build -t chroniclehub-api .
docker tag chroniclehub-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/chroniclehub-api:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/chroniclehub-api:latest
```

### Google Kubernetes Engine (GKE)

```bash
# Build and push to GCR
gcloud builds submit --tag gcr.io/<project-id>/chroniclehub-api
```

## Next Steps

- [Production Best Practices](production.md) - Security, monitoring, scaling
- [Configuration Guide](../configuration.md) - Detailed configuration options
- [Architecture Overview](../architecture/overview.md) - Understand the system design
