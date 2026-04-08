#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:8.0"
reset_stack="${RESET_STACK:-false}"
build_stack="${BUILD_STACK:-false}"
cpu_limit="${TEST_CPU_LIMIT:-2}"
memory_limit="${TEST_MEMORY_LIMIT:-4g}"
skip_e2e="${SKIP_E2E:-false}"
skip_integration="${SKIP_INTEGRATION:-false}"

cd "$ROOT_DIR"

if [[ "$reset_stack" == "true" ]]; then
  echo "[1/5] Resetting docker stack..."
  docker compose down -v >/dev/null 2>&1 || true
fi

echo "[2/5] Starting required services (sqlserver, webapi)..."
if [[ "$build_stack" == "true" ]]; then
  docker compose up -d --build sqlserver webapi
else
  docker compose up -d sqlserver webapi
fi

echo "[3/6] Running unit + infrastructure tests in single SDK container..."
docker run --rm \
  --cpus="$cpu_limit" \
  --memory="$memory_limit" \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
  -v "$ROOT_DIR":/src \
  -v flux-nuget-cache:/root/.nuget/packages \
  -w /src \
  "$SDK_IMAGE" bash -lc '
  dotnet restore CleanArchitecture.sln --disable-parallel &&
  dotnet test Tests/CleanArchitecture.UnitTests/CleanArchitecture.UnitTests.csproj --no-restore --nologo --verbosity minimal -m:1 -- RunConfiguration.MaxCpuCount=1 &&
  dotnet test Tests/CleanArchitecture.Infrastructure.Tests/CleanArchitecture.Infrastructure.Tests.csproj --no-restore --nologo --verbosity minimal -m:1 -- RunConfiguration.MaxCpuCount=1
'

if [[ "$skip_integration" == "true" ]]; then
  echo "[4/6] Skipping integration tests (SKIP_INTEGRATION=true)"
else
  echo "[4/6] Running API integration tests (JWT + SQL + authorization)..."
  docker run --rm \
    --cpus="$cpu_limit" \
    --memory="$memory_limit" \
    --network backend_default \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    -e DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    -e INTEGRATION_API_BASE_URL="http://flux-webapi:8080" \
    -e INTEGRATION_SQL_CONNECTION="Server=flux-sqlserver,1433;Database=CleanArchitectureApplicationDb;User Id=sa;Password=Your_strong_password_123;TrustServerCertificate=True;Encrypt=False" \
    -v "$ROOT_DIR":/src \
    -v flux-nuget-cache:/root/.nuget/packages \
    -w /src \
    "$SDK_IMAGE" bash -lc '
    dotnet test Tests/CleanArchitecture.IntegrationTests/CleanArchitecture.IntegrationTests.csproj --no-restore --nologo --verbosity minimal -m:1 -- RunConfiguration.MaxCpuCount=1
  '
fi

if [[ "$skip_e2e" == "true" ]]; then
  echo "[5/8] Skipping E2E tests (SKIP_E2E=true)"
  echo "[6/8] Skipping E2E tests (SKIP_E2E=true)"
  echo "[7/8] Skipping E2E tests (SKIP_E2E=true)"
  echo "[8/8] Skipping E2E tests (SKIP_E2E=true)"
  echo "All selected tests passed."
  exit 0
fi

echo "[5/8] Running auth E2E tests..."
RESET_STACK=false BUILD_STACK=false bash scripts/test-auth-e2e.sh

echo "[6/8] Running time entries E2E tests..."
RESET_STACK=false BUILD_STACK=false bash scripts/test-timeentries-e2e.sh

echo "[7/8] Running reports E2E tests..."
RESET_STACK=false BUILD_STACK=false bash scripts/test-reports-e2e.sh

echo "[8/8] Running expenses E2E tests..."
RESET_STACK=false BUILD_STACK=false bash scripts/test-expenses-e2e.sh

echo "All tests passed."
