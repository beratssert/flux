#!/usr/bin/env bash
set -euo pipefail

base_url="http://localhost:5001"
reset_stack="${RESET_STACK:-true}"
build_stack="${BUILD_STACK:-false}"

start_services_if_needed() {
  if [[ "$build_stack" == "true" ]]; then
    docker compose up -d --build sqlserver webapi >/dev/null
  else
    docker compose up -d sqlserver webapi >/dev/null
  fi
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

echo "[INFO] Login seeded admin"
admin_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"admin@flux.local","password":"123Pa$$word!"}')
assert_contains "$admin_login" "accessToken" "Admin login returns accessToken"
admin_token=$(echo "$admin_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')


echo "[INFO] Register new employee"
register_resp=$(curl -s -X POST "$base_url/api/v1/auth/register" -H 'Content-Type: application/json' -d '{"firstName":"Ece","lastName":"Employee","email":"ece@flux.local","password":"123Pa$$word!"}')
assert_contains "$register_resp" "User registered successfully" "Employee self-registration works"

echo "[INFO] Login new employee"
emp_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"ece@flux.local","password":"123Pa$$word!"}')
assert_contains "$emp_login" "accessToken" "Employee login returns accessToken"
emp_token=$(echo "$emp_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')


echo "[INFO] Get my profile"
me_resp=$(curl -s -H "Authorization: Bearer $emp_token" "$base_url/api/v1/users/me")
assert_contains "$me_resp" '"role":"Employee"' "Users/me returns Employee role"
assert_contains "$me_resp" '"isActive":true' "Users/me returns active status"

echo "[INFO] Employee updates own profile"
update_me_resp=$(curl -s -X PATCH "$base_url/api/v1/users/me" -H "Authorization: Bearer $emp_token" -H 'Content-Type: application/json' -d '{"firstName":"EceUpdated","lastName":"EmployeeUpdated"}')
assert_contains "$update_me_resp" '"firstName":"EceUpdated"' "Users/me PATCH updates profile"


echo "[INFO] Admin creates manager"
create_manager_resp=$(curl -s -X POST "$base_url/api/v1/admin/users/manager" -H "Authorization: Bearer $admin_token" -H 'Content-Type: application/json' -d '{"firstName":"Mert","lastName":"Manager","email":"mert@flux.local","password":"123Pa$$word!"}')
assert_contains "$create_manager_resp" "Manager account created" "Admin can create manager"

echo "[INFO] Manager login"
mgr_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"mert@flux.local","password":"123Pa$$word!"}')
assert_contains "$mgr_login" "accessToken" "Manager login works"

mgr_token=$(echo "$mgr_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

echo "[INFO] Manager reads users list"
users_list_resp=$(curl -s -H "Authorization: Bearer $mgr_token" "$base_url/api/v1/users?pageNumber=1&pageSize=50")
assert_contains "$users_list_resp" '"items":' "Manager can read team users list"

mgr_user_id=$(echo "$mgr_login" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')


echo "[INFO] Admin updates manager status to Suspended"
status_resp=$(curl -s -X PATCH "$base_url/api/v1/admin/users/$mgr_user_id/status" -H "Authorization: Bearer $admin_token" -H 'Content-Type: application/json' -d '{"status":"Suspended"}')
assert_contains "$status_resp" "User status updated" "Admin can update user status"


echo "[INFO] Suspended manager cannot login"
suspended_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"mert@flux.local","password":"123Pa$$word!"}')
assert_contains "$suspended_login" "Account is not active" "Suspended user login blocked"


echo "[INFO] Auth E2E tests completed successfully"
