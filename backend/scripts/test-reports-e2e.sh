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
  docker compose down -v >/dev/null 2>&1 || true
  if [[ "$build_stack" == "true" ]]; then
    docker compose up -d --build >/dev/null
  else
    docker compose up -d >/dev/null
  fi
else
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

echo "[INFO] Login seeded manager"
manager_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"manager@flux.local","password":"123Pa$$word!"}')
assert_contains "$manager_login" "accessToken" "Manager login returns accessToken"
manager_token=$(echo "$manager_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

echo "[INFO] Login seeded employee"
employee_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"employee@flux.local","password":"123Pa$$word!"}')
assert_contains "$employee_login" "accessToken" "Employee login returns accessToken"
employee_token=$(echo "$employee_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

echo "[INFO] Employee can read self time summary"
my_summary=$(curl -s "$base_url/api/v1/reports/me/time-summary?groupBy=week" -H "Authorization: Bearer $employee_token")
assert_contains "$my_summary" '"totalMinutes":' "Self time summary returns total minutes"
assert_contains "$my_summary" '"groups":' "Self time summary returns groups"

echo "[INFO] Manager can read team time summary"
team_summary=$(curl -s "$base_url/api/v1/reports/manager/team-time-summary?groupBy=user" -H "Authorization: Bearer $manager_token")
assert_contains "$team_summary" '"totalMinutes":' "Team time summary returns total minutes"
assert_contains "$team_summary" '"groups":' "Team time summary returns groups"

echo "[INFO] Employee cannot read manager team summary"
employee_team_status=$(curl -s -o /dev/null -w "%{http_code}" "$base_url/api/v1/reports/manager/team-time-summary?groupBy=user" -H "Authorization: Bearer $employee_token")
assert_status "$employee_team_status" "403" "Employee blocked from manager team summary"

echo "[INFO] CSV export works for self summary"
headers_file=$(mktemp)
export_status=$(curl -s -D "$headers_file" -o /dev/null -w "%{http_code}" "$base_url/api/v1/reports/me/time-summary/export?format=csv&groupBy=week" -H "Authorization: Bearer $employee_token")
export_headers=$(cat "$headers_file")
rm -f "$headers_file"
assert_status "$export_status" "200" "Self summary export returns 200"
assert_contains "$export_headers" "text/csv" "Self summary export returns csv content-type"

echo "[INFO] CSV export works for manager team summary"
team_headers_file=$(mktemp)
team_export_status=$(curl -s -D "$team_headers_file" -o /dev/null -w "%{http_code}" "$base_url/api/v1/reports/manager/team-time-summary/export?format=csv&groupBy=user" -H "Authorization: Bearer $manager_token")
team_export_headers=$(cat "$team_headers_file")
rm -f "$team_headers_file"
assert_status "$team_export_status" "200" "Manager team summary export returns 200"
assert_contains "$team_export_headers" "text/csv" "Manager team summary export returns csv content-type"

echo "[INFO] Reports E2E tests completed successfully"
