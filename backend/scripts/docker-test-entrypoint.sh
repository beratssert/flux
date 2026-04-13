#!/bin/sh
set -e

if [ -n "${INTEGRATION_API_BASE_URL:-}" ]; then
  echo "Waiting for API health at ${INTEGRATION_API_BASE_URL}/health ..."
  i=0
  while ! curl -sf "${INTEGRATION_API_BASE_URL}/health" >/dev/null 2>&1; do
    i=$((i + 1))
    if [ "$i" -gt 90 ]; then
      echo "Timeout waiting for API (90 attempts)."
      exit 1
    fi
    sleep 2
  done
  echo "API is ready."
fi

exec dotnet test CleanArchitecture.sln -c Release --verbosity normal "$@"
