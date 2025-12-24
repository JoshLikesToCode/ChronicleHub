#!/bin/bash
# Helm Template Validation Tests
# Tests that Helm charts render valid YAML for different configurations

# Don't exit on error - we want to run all tests and report results
set +e

CHART_PATH="./helm/chroniclehub"
TEST_NAME="Helm Template Tests"
PASSED=0
FAILED=0

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "$TEST_NAME"
echo "=========================================="
echo ""

# Helper function to run a test
run_test() {
    local test_name="$1"
    local helm_args="$2"

    echo -n "Testing: $test_name... "

    if helm template test-release $CHART_PATH $helm_args > /dev/null 2>&1; then
        echo -e "${GREEN}PASSED${NC}"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}FAILED${NC}"
        echo "  Command: helm template test-release $CHART_PATH $helm_args"
        helm template test-release $CHART_PATH $helm_args 2>&1 | head -20 | sed 's/^/  /'
        ((FAILED++))
        return 1
    fi
}

# Test 1: Default values
run_test "Default values (SQLite, startup migrations)" ""

# Test 2: Migration job enabled
run_test "Migration job enabled" \
    "--set database.migrations.job.enabled=true"

# Test 3: Init container enabled
run_test "Init container enabled" \
    "--set database.migrations.initContainer.enabled=true"

# Test 4: PostgreSQL with secret
run_test "PostgreSQL with connection string secret" \
    "--set database.provider=postgresql \
     --set database.connectionStringSecretName=db-secret \
     --set database.connectionString=''"

# Test 5: PostgreSQL with inline connection string
run_test "PostgreSQL with inline connection string" \
    "--set database.provider=postgresql \
     --set database.connectionString='Host=postgres;Database=test' \
     --set persistence.enabled=false"

# Test 6: SQL Server configuration
run_test "SQL Server configuration" \
    "--set database.provider=sqlserver \
     --set database.connectionString='Server=sqlserver;Database=test' \
     --set persistence.enabled=false"

# Test 7: Production values file (PostgreSQL init container)
run_test "Production values: PostgreSQL with init container" \
    "-f $CHART_PATH/values-postgres-initcontainer.yaml"

# Test 8: Production values file (PostgreSQL job)
run_test "Production values: PostgreSQL with job migrations" \
    "-f $CHART_PATH/values-postgres-job.yaml"

# Test 9: Production values file (SQL Server AKS)
run_test "Production values: SQL Server for AKS" \
    "-f $CHART_PATH/values-sqlserver-aks.yaml"

# Test 10: Autoscaling enabled
run_test "Autoscaling enabled" \
    "--set autoscaling.enabled=true \
     --set autoscaling.minReplicas=2 \
     --set autoscaling.maxReplicas=10"

# Test 11: High replica count
run_test "High replica count (5 replicas)" \
    "--set replicaCount=5"

# Test 12: Migrations disabled, no init container, no job
run_test "All migration strategies disabled" \
    "--set database.migrations.runOnStartup=false \
     --set database.migrations.initContainer.enabled=false \
     --set database.migrations.job.enabled=false"

# Test 13: Persistence disabled
run_test "Persistence disabled" \
    "--set persistence.enabled=false"

# Test 14: Custom resource limits
run_test "Custom resource limits" \
    "--set resources.limits.cpu=2000m \
     --set resources.limits.memory=2Gi \
     --set resources.requests.cpu=1000m \
     --set resources.requests.memory=1Gi"

echo ""
echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo "Total:  $((PASSED + FAILED))"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed!${NC}"
    exit 1
fi
