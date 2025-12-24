# CI/CD Pipeline

ChronicleHub uses GitHub Actions for continuous integration and deployment, providing automated validation of code quality, security, and production-readiness.

## Pipeline Overview

The CI/CD pipeline runs on:
- **Push** to `main` or `develop` branches
- **Pull requests** to `main` or `develop`
- **Release** creation (tagged releases)

## Pipeline Jobs

### 1. Build & Test

**Purpose:** Validate code quality and run all 128 tests

**Steps:**
- ✅ Restore .NET dependencies
- ✅ Build solution in Release mode
- ✅ Run all unit and integration tests
- ✅ Generate code coverage reports (Cobertura format)
- ✅ Publish test results with detailed reporting
- ✅ Upload coverage to Codecov
- ✅ Archive test artifacts (30-day retention)

**Artifacts:**
- `test-results/` - TRX test result files
- `coverage-report/` - HTML and Cobertura coverage reports

**Outputs:**
- Test summary in PR comments
- Code coverage percentage
- Test pass/fail status

### 2. Docker Build & Scan

**Purpose:** Build container image and scan for security vulnerabilities

**Steps:**
- ✅ Set up Docker Buildx for optimized builds
- ✅ Extract metadata for Docker tags (semantic versioning)
- ✅ Build Docker image with layer caching
- ✅ Run Trivy security scan (CRITICAL and HIGH severities)
- ✅ Upload security results to GitHub Security tab
- ✅ Push image to GitHub Container Registry (on main branch)

**Security Features:**
- Container vulnerability scanning with Trivy
- SARIF format results uploaded to GitHub Security
- Fail on critical vulnerabilities
- Multi-architecture support ready

**Published Images:**
- `ghcr.io/joshlikestocode/chroniclehub:latest` (main branch)
- `ghcr.io/joshlikestocode/chroniclehub:main` (branch tag)
- `ghcr.io/joshlikestocode/chroniclehub:v1.2.3` (release tags)
- `ghcr.io/joshlikestocode/chroniclehub:main-abc1234` (commit SHA)

### 3. Helm Chart Validation

**Purpose:** Validate Kubernetes manifests and Helm charts

**Steps:**
- ✅ Helm lint with strict mode
- ✅ Helm template rendering
- ✅ Kubeconform validation of generated manifests
- ✅ Validate raw K8s manifests in `k8s/` directory
- ✅ Archive rendered Helm templates

**Validation Tools:**
- **Helm Lint:** Checks chart syntax and best practices
- **Kubeconform:** Validates K8s manifest schemas against API specifications

**Artifacts:**
- `helm-templates/` - Rendered Helm chart templates (7-day retention)

### 4. Release Automation

**Purpose:** Package and publish release artifacts

**Triggered:** Only on GitHub Release creation

**Steps:**
- ✅ Package Helm chart with release version
- ✅ Upload Helm chart `.tgz` to GitHub Release
- ✅ Generate release notes with Docker and Helm installation commands
- ✅ Tag Docker image with semantic version

**Release Artifacts:**
- `chroniclehub-{version}.tgz` - Helm chart package
- Docker image tagged with release version

### 5. CI Success Summary

**Purpose:** Provide consolidated status of all pipeline jobs

**Features:**
- Summary in GitHub Actions UI
- Links to all job results
- Quick status overview

## Required Secrets

### GitHub Container Registry

No additional secrets needed - uses built-in `GITHUB_TOKEN` with `packages: write` permission.

### Codecov (Optional)

To enable code coverage reporting:

1. Sign up at [codecov.io](https://codecov.io)
2. Add your repository
3. Copy the upload token
4. Add to GitHub Secrets: `Settings → Secrets → Actions → New repository secret`
   - Name: `CODECOV_TOKEN`
   - Value: `<your-codecov-token>`

**Note:** Pipeline will continue without Codecov token (coverage upload is non-blocking).

## Badges

The following badges are displayed in the README:

```markdown
[![CI/CD Pipeline](https://github.com/JoshLikesToCode/ChronicleHub/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/JoshLikesToCode/ChronicleHub/actions/workflows/ci-cd.yml)
[![codecov](https://codecov.io/gh/JoshLikesToCode/ChronicleHub/branch/main/graph/badge.svg)](https://codecov.io/gh/JoshLikesToCode/ChronicleHub)
[![Docker Image](https://ghcr-badge.egpl.dev/joshlikestocode/chroniclehub/latest_tag?trim=major&label=latest)](https://github.com/JoshLikesToCode/ChronicleHub/pkgs/container/chroniclehub)
```

**Badge Status:**
- 🟢 **Green:** All checks passing
- 🔴 **Red:** Build/test failures
- 🟡 **Yellow:** In progress

## Workflow Triggers

### Push to Main/Develop
```yaml
on:
  push:
    branches: [ main, develop ]
```

**Actions:**
- Run all tests
- Build and scan Docker image
- Push image to registry (main only)
- Validate Helm charts

### Pull Requests
```yaml
on:
  pull_request:
    branches: [ main, develop ]
```

**Actions:**
- Run all tests with coverage
- Build Docker image (no push)
- Add test summary to PR
- Validate Helm charts

### Releases
```yaml
on:
  release:
    types: [ published ]
```

**Actions:**
- Run full pipeline
- Package Helm chart
- Tag and push Docker image
- Upload artifacts to release

## Local Testing

### Test Build Locally
```bash
# Build .NET solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release
```

### Test Docker Build
```bash
# Build image
docker build -t chroniclehub-api:test .

# Scan with Trivy
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy:latest image chroniclehub-api:test
```

### Test Helm Chart
```bash
# Lint chart
helm lint ./helm/chroniclehub --strict

# Template and validate
helm template chroniclehub ./helm/chroniclehub --output-dir ./helm-output
kubeconform -strict ./helm-output/chroniclehub/templates/*.yaml
```

## Caching Strategy

The pipeline uses GitHub Actions caching to improve performance:

- **NuGet packages:** Cached by `actions/setup-dotnet@v4`
- **Docker layers:** Cached with `type=gha` (GitHub Actions cache)
- **Helm charts:** No caching needed (small files)

**Cache Benefits:**
- Faster builds (30-50% reduction)
- Reduced bandwidth usage
- Lower GitHub Actions minutes consumption

## Monitoring and Debugging

### View Pipeline Results
1. Go to **Actions** tab in GitHub
2. Select the workflow run
3. Review job summaries and logs

### Common Issues

**Tests Failing:**
```bash
# Check test output in job logs
# Download test-results artifact for detailed TRX files
```

**Docker Build Failing:**
```bash
# Check Dockerfile syntax
# Verify base image availability
# Review build context
```

**Helm Validation Failing:**
```bash
# Run locally: helm lint ./helm/chroniclehub --strict
# Check template syntax
# Validate values.yaml schema
```

## Performance Metrics

**Average Pipeline Duration:**
- Build & Test: 2-3 minutes
- Docker Build & Scan: 3-5 minutes
- Helm Validation: 1-2 minutes
- **Total:** ~6-10 minutes

**Concurrent Jobs:**
- Build & Test (always runs first)
- Docker Build & Helm Validation (run in parallel after tests pass)

## Security Features

### Container Scanning
- **Tool:** Trivy by Aqua Security
- **Scan Target:** Final Docker image
- **Severities:** CRITICAL and HIGH
- **Results:** Uploaded to GitHub Security tab (SARIF format)

### Dependency Scanning
- **Tool:** GitHub Dependabot (auto-enabled)
- **Scope:** NuGet packages, Docker base images, GitHub Actions
- **Alerts:** Security tab

### Code Scanning
- **Tool:** CodeQL (optional, can be added)
- **Scope:** C# code analysis
- **Configuration:** Add `.github/workflows/codeql.yml`

## Future Enhancements

Potential additions to the pipeline:

- [ ] Performance benchmarking
- [ ] E2E tests with Playwright
- [ ] Multi-architecture builds (linux/amd64, linux/arm64)
- [ ] Deployment to staging environment
- [ ] Slack/Teams notifications
- [ ] Automated changelog generation
- [ ] SBOM (Software Bill of Materials) generation

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Trivy Security Scanner](https://github.com/aquasecurity/trivy)
- [Helm Best Practices](https://helm.sh/docs/chart_best_practices/)
- [Kubeconform](https://github.com/yannh/kubeconform)
- [Codecov](https://docs.codecov.com/)
