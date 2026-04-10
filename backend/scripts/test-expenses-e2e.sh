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

echo "[INFO] Login seeded admin"
admin_login=$(curl -s -X POST "$base_url/api/v1/auth/login" -H 'Content-Type: application/json' -d '{"email":"admin@flux.local","password":"123Pa$$word!"}')
assert_contains "$admin_login" "accessToken" "Admin login returns accessToken"
admin_token=$(echo "$admin_login" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

echo "[INFO] Employee can list expense categories"
categories_body=$(curl -s "$base_url/api/v1/expense-categories" -H "Authorization: Bearer $employee_token")
assert_contains "$categories_body" '"name":' "Expense categories endpoint returns category list"

echo "[INFO] Employee creates a draft expense"
create_file=$(mktemp)
create_status=$(curl -s -o "$create_file" -w "%{http_code}" \
  -X POST "$base_url/api/v1/expenses" \
  -H "Authorization: Bearer $employee_token" \
  -H 'Content-Type: application/json' \
  -d '{"projectId":1,"expenseDate":"2035-01-15","amount":250.75,"currencyCode":"TRY","categoryId":1,"notes":"Expense E2E draft","receiptUrl":"https://example.com/receipt/e2e"}')
create_body=$(cat "$create_file")
rm -f "$create_file"
assert_status "$create_status" "201" "Employee create expense returns 201"
expense_id=$(echo "$create_body" | tr -d '[:space:]')
if [[ -z "$expense_id" || ! "$expense_id" =~ ^[0-9]+$ ]]; then
  echo "[FAIL] Expense id parsing failed"
  echo "Response: $create_body"
  exit 1
fi
echo "[PASS] Expense id parsed: $expense_id"

echo "[INFO] Employee can list own expenses"
list_body=$(curl -s "$base_url/api/v1/expenses?pageNumber=1&pageSize=10" -H "Authorization: Bearer $employee_token")
assert_contains "$list_body" "\"id\":$expense_id" "Created expense appears in list"

echo "[INFO] Submit expense"
submit_status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$base_url/api/v1/expenses/$expense_id/submit" -H "Authorization: Bearer $employee_token")
assert_status "$submit_status" "200" "Expense submit returns 200"

echo "[INFO] Patch should fail after submit"
patch_status=$(curl -s -o /dev/null -w "%{http_code}" \
  -X PATCH "$base_url/api/v1/expenses/$expense_id" \
  -H "Authorization: Bearer $employee_token" \
  -H 'Content-Type: application/json' \
  -d "{\"id\":$expense_id,\"projectId\":1,\"expenseDate\":\"2035-01-15\",\"amount\":300,\"currencyCode\":\"TRY\",\"categoryId\":1,\"notes\":\"Should fail\",\"receiptUrl\":\"https://example.com/receipt/updated\"}")
assert_status "$patch_status" "400" "Submitted expense patch blocked"

echo "[INFO] Manager rejects submitted expense"
reject_status=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$base_url/api/v1/expenses/$expense_id/reject" \
  -H "Authorization: Bearer $manager_token" \
  -H 'Content-Type: application/json' \
  -d "{\"id\":$expense_id,\"reason\":\"Receipt unreadable\"}")
assert_status "$reject_status" "200" "Manager reject returns 200"

echo "[INFO] Employee can resubmit rejected expense"
resubmit_status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$base_url/api/v1/expenses/$expense_id/submit" -H "Authorization: Bearer $employee_token")
assert_status "$resubmit_status" "200" "Employee resubmit returns 200"

echo "[INFO] Admin cannot create expense (MVP)"
admin_create_status=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$base_url/api/v1/expenses" \
  -H "Authorization: Bearer $admin_token" \
  -H 'Content-Type: application/json' \
  -d '{"projectId":1,"expenseDate":"2035-01-15","amount":100,"currencyCode":"TRY","categoryId":1,"notes":"Admin should fail","receiptUrl":"https://example.com/receipt/admin"}')
assert_status "$admin_create_status" "403" "Admin create expense forbidden in MVP"

echo "[INFO] Expenses E2E tests completed successfully"
