#!/usr/bin/env bash
set -euo pipefail

base_url="http://localhost:5001"
reset_stack="${RESET_STACK:-false}"
build_stack="${BUILD_STACK:-false}"

start_services_if_needed() {
  if [[ "$build_stack" == "true" ]]; then
    docker compose up -d --build sqlserver webapi >/dev/null
  else
    docker compose up -d sqlserver webapi >/dev/null
  fi
}

assert_status() {
  local actual="$1"
  local expected="$2"
  local message="$3"
  if [[ "$actual" != "$expected" ]]; then
    echo "[FAIL] $message (expected $expected, got $actual)"
    exit 1
  fi
  echo "[PASS] $message"
}

assert_contains() {
  local haystack="$1"
  local needle="$2"
  local message="$3"
  if [[ "$haystack" != *"$needle"* ]]; then
    echo "[FAIL] $message"
    echo "Response: $haystack"
    exit 1
  fi
  echo "[PASS] $message"
}

if [[ "$reset_stack" == "true" ]]; then
  echo "[INFO] Resetting stack with clean database volume"
  docker compose down -v >/dev/null 2>&1 || true
  if [[ "$build_stack" == "true" ]]; then
    docker compose up -d --build >/dev/null
  else
    docker compose up -d >/dev/null
  fi
else
  echo "[INFO] Reusing running stack (no reset)"
  start_services_if_needed
fi

for i in {1..30}; do
  code=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/health" || true)
  if [[ "$code" == "200" ]]; then
    break
  fi
  sleep 2
done

if [[ "${code:-000}" != "200" ]]; then
  echo "[FAIL] API did not become healthy"
  exit 1
fi
echo "[PASS] API is healthy"

echo "[INFO] Login seeded manager"
manager_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"manager@flux.local","password":"123Pa$$word!"}')
assert_contains "$manager_login" "accessToken" "Manager login returns accessToken"
manager_token=$(echo "$manager_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

echo "[INFO] Login seeded employee"
employee_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"employee@flux.local","password":"123Pa$$word!"}')
assert_contains "$employee_login" "accessToken" "Employee login returns accessToken"
employee_token=$(echo "$employee_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

echo "[INFO] Manager can access team time entries"
manager_body_file=$(mktemp)
manager_status=$(curl -s -o "$manager_body_file" -w "%{http_code}" "$base_url/api/v1/TimeEntries/team?pageNumber=1&pageSize=10" -H "Authorization: Bearer $manager_token")
manager_body=$(cat "$manager_body_file")
rm -f "$manager_body_file"
assert_status "$manager_status" "200" "Manager team endpoint returns 200"
assert_contains "$manager_body" '"data":' "Manager team response includes data array"
assert_contains "$manager_body" '"userId":' "Manager team response includes employee userId"

echo "[INFO] Employee cannot access manager team endpoint"
employee_status=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/api/v1/TimeEntries/team?pageNumber=1&pageSize=10" -H "Authorization: Bearer $employee_token")
assert_status "$employee_status" "403" "Employee blocked from team endpoint"

echo "[INFO] Team endpoint supports employee filter"
filtered_body=$(curl -s "$base_url/api/v1/TimeEntries/team?pageNumber=1&pageSize=10&employeeUserId=does-not-exist" -H "Authorization: Bearer $manager_token")
assert_contains "$filtered_body" '"data":[]' "Filter by unknown employee returns empty team list"

echo "[INFO] Manager can access project summary"
project_summary_status=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/api/v1/TimeEntries/team/summary/projects" -H "Authorization: Bearer $manager_token")
assert_status "$project_summary_status" "200" "Manager project summary endpoint returns 200"

echo "[INFO] Manager can access period summary"
period_summary_status=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/api/v1/TimeEntries/team/summary/period" -H "Authorization: Bearer $manager_token")
assert_status "$period_summary_status" "200" "Manager period summary endpoint returns 200"

echo "[INFO] Employee cannot access team summary endpoints"
employee_project_summary_status=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/api/v1/TimeEntries/team/summary/projects" -H "Authorization: Bearer $employee_token")
assert_status "$employee_project_summary_status" "403" "Employee blocked from project summary endpoint"

employee_period_summary_status=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/api/v1/TimeEntries/team/summary/period" -H "Authorization: Bearer $employee_token")
assert_status "$employee_period_summary_status" "403" "Employee blocked from period summary endpoint"

echo "[INFO] TimeEntries team E2E tests completed successfully"
