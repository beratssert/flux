#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:8.0"
reset_stack="${RESET_STACK:-false}"
build_stack="${BUILD_STACK:-false}"
cpu_limit="${TEST_CPU_LIMIT:-2}"
memory_limit="${TEST_MEMORY_LIMIT:-4g}"
skip_e2e="${SKIP_E2E:-false}"

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

echo "[3/5] Running unit + infrastructure tests in single SDK container..."
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

if [[ "$skip_e2e" == "true" ]]; then
  echo "[4/5] Skipping E2E tests (SKIP_E2E=true)"
  echo "[5/5] Skipping E2E tests (SKIP_E2E=true)"
  echo "All selected tests passed."
  exit 0
fi

echo "[4/5] Running auth E2E tests..."
RESET_STACK=false BUILD_STACK=false bash scripts/test-auth-e2e.sh

echo "[5/5] Running time entries E2E tests..."
RESET_STACK=false BUILD_STACK=false bash scripts/test-timeentries-e2e.sh

echo "All tests passed."
