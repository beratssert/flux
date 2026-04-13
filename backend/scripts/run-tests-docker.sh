#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."

echo "Building and starting SQL Server + Web API (if not already running)..."
docker compose up -d sqlserver webapi

echo "Running test suite inside Docker (SDK 8.0)..."
docker compose --profile test run --rm --build tests

echo "Tests finished."
