#!/usr/bin/env bash
set -u

API_BASE="${API_BASE:-http://localhost:5001/api/v1}"
HEALTH_URL="${HEALTH_URL:-http://localhost:5001/health}"
REGISTER_PASSWORD="${REGISTER_PASSWORD:-123Pa$$word!}"
REGISTER_EMAIL="${REGISTER_EMAIL:-manual_test_$(date +%s)@company.com}"
DB_CONTAINER="${DB_CONTAINER:-flux-sqlserver}"
DB_NAME="${DB_NAME:-CleanArchitectureApplicationDb}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-Your_strong_password_123}"

PASS_COUNT=0
FAIL_COUNT=0

RESP_BODY=""
RESP_CODE=""

print_header() {
  echo "===================================================="
  echo "   MANUAL TEST: AUTH + TIME ENTRIES + TIMERS"
  echo "===================================================="
  echo "API_BASE: $API_BASE"
}

pass() {
  PASS_COUNT=$((PASS_COUNT + 1))
  echo "[PASS] $1"
}

fail() {
  FAIL_COUNT=$((FAIL_COUNT + 1))
  echo "[FAIL] $1"
  if [[ -n "${2:-}" ]]; then
    echo "       $2"
  fi
}

check_dep() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "[CRITICAL] Required command not found: $cmd"
    exit 1
  fi
}

wait_for_health() {
  echo "[INFO] Waiting for API health: $HEALTH_URL"
  local code="000"
  for _ in {1..30}; do
    code=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL" || true)
    if [[ "$code" == "200" ]]; then
      pass "API health check"
      return
    fi
    sleep 2
  done

  echo "[CRITICAL] API is not healthy (last status: $code)"
  exit 1
}

api_call() {
  local method="$1"
  local url="$2"
  local token="${3:-}"
  local json_body="${4:-}"

  local tmp_file
  tmp_file=$(mktemp)

  if [[ -n "$token" && -n "$json_body" ]]; then
    RESP_CODE=$(curl -s -o "$tmp_file" -w "%{http_code}" -X "$method" "$url" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $token" \
      -d "$json_body")
  elif [[ -n "$token" ]]; then
    RESP_CODE=$(curl -s -o "$tmp_file" -w "%{http_code}" -X "$method" "$url" \
      -H "Authorization: Bearer $token")
  elif [[ -n "$json_body" ]]; then
    RESP_CODE=$(curl -s -o "$tmp_file" -w "%{http_code}" -X "$method" "$url" \
      -H "Content-Type: application/json" \
      -d "$json_body")
  else
    RESP_CODE=$(curl -s -o "$tmp_file" -w "%{http_code}" -X "$method" "$url")
  fi

  RESP_BODY=$(cat "$tmp_file")
  rm -f "$tmp_file"
}

expect_status() {
  local expected="$1"
  local test_name="$2"
  if [[ "$RESP_CODE" == "$expected" ]]; then
    pass "$test_name (status=$RESP_CODE)"
  else
    fail "$test_name (expected=$expected, actual=$RESP_CODE)" "response=$RESP_BODY"
  fi
}

expect_status_one_of() {
  local expected_a="$1"
  local expected_b="$2"
  local test_name="$3"
  if [[ "$RESP_CODE" == "$expected_a" || "$RESP_CODE" == "$expected_b" ]]; then
    pass "$test_name (status=$RESP_CODE)"
  else
    fail "$test_name (expected=$expected_a|$expected_b, actual=$RESP_CODE)" "response=$RESP_BODY"
  fi
}

extract_token() {
  local body="$1"
  echo "$body" | jq -r '.accessToken // empty'
}

extract_active_timer_id() {
  local body="$1"
  echo "$body" | jq -r '.id // empty'
}

extract_user_id() {
  local body="$1"
  echo "$body" | jq -r '.id // empty'
}

sql_scalar() {
  local query="$1"
  docker exec "$DB_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U "$DB_USER" \
    -P "$DB_PASSWORD" \
    -d "$DB_NAME" \
    -C \
    -h -1 \
    -W \
    -Q "SET NOCOUNT ON; $query" 2>/dev/null | tr -d '\r' | sed '/^$/d' | head -n 1
}

sql_exec() {
  local query="$1"
  docker exec "$DB_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U "$DB_USER" \
    -P "$DB_PASSWORD" \
    -d "$DB_NAME" \
    -C \
    -Q "SET NOCOUNT ON; $query" >/dev/null
}

ensure_project_assignment_for_user_id() {
  local user_id="$1"
  local project_id
  local has_assignment

  project_id=$(sql_scalar "SELECT TOP 1 Id FROM Projects ORDER BY Id")
  if [[ -z "$project_id" ]]; then
    fail "Resolve project id from database" "No project exists in Projects table"
    return 1
  fi

  has_assignment=$(sql_scalar "SELECT TOP 1 1 FROM ProjectAssignments WHERE UserId = '$user_id' AND ProjectId = $project_id AND IsActive = 1")
  if [[ "$has_assignment" != "1" ]]; then
    sql_exec "INSERT INTO ProjectAssignments (UserId, ProjectId, IsActive, CreatedBy, Created) VALUES ('$user_id', $project_id, 1, 'manual-script', GETUTCDATE())"
  fi

  echo "$project_id"
  return 0
}

print_summary() {
  echo ""
  echo "===================================================="
  echo "RESULT: PASS=$PASS_COUNT | FAIL=$FAIL_COUNT"
  echo "===================================================="
  if [[ "$FAIL_COUNT" -gt 0 ]]; then
    exit 1
  fi
}

main() {
  check_dep curl
  check_dep jq
  check_dep docker

  print_header
  wait_for_health

  echo "[INFO] Registering a fresh employee account"
  api_call "POST" "$API_BASE/auth/register" "" "{\"firstName\":\"Manual\",\"lastName\":\"Tester\",\"email\":\"$REGISTER_EMAIL\",\"password\":\"$REGISTER_PASSWORD\"}"
  expect_status "200" "Auth register"

  echo "[INFO] Login with newly registered account"
  api_call "POST" "$API_BASE/auth/login" "" "{\"email\":\"$REGISTER_EMAIL\",\"password\":\"$REGISTER_PASSWORD\"}"
  expect_status "200" "Auth login (new user)"
  local new_user_token
  new_user_token=$(extract_token "$RESP_BODY")
  if [[ -n "$new_user_token" ]]; then
    pass "New user access token parsed"
  else
    fail "New user access token parsed" "response=$RESP_BODY"
  fi

  echo "[INFO] Resolve newly registered user profile"
  api_call "GET" "$API_BASE/users/me" "$new_user_token"
  expect_status "200" "Get newly registered user profile"
  local new_user_id
  new_user_id=$(extract_user_id "$RESP_BODY")
  if [[ -z "$new_user_id" ]]; then
    echo "[CRITICAL] Could not resolve user id from /users/me"
    echo "response=$RESP_BODY"
    exit 1
  fi

  echo "[INFO] Creating project assignment for newly registered user"
  local project_id
  project_id=$(ensure_project_assignment_for_user_id "$new_user_id")
  if [[ -z "$project_id" ]]; then
    echo "[CRITICAL] Could not assign user to a project"
    exit 1
  fi
  pass "Assigned new user to projectId=$project_id"

  echo "[INFO] Employee team endpoint must be forbidden"
  api_call "GET" "$API_BASE/TimeEntries/team?page=1&pageSize=10" "$new_user_token"
  expect_status "403" "Employee blocked from team entries"

  echo "[INFO] Ensure no active timer before timer tests"
  api_call "GET" "$API_BASE/Timers/active" "$new_user_token"
  expect_status_one_of "200" "204" "Get active timer"
  local active_timer_id
  active_timer_id=$(extract_active_timer_id "$RESP_BODY")
  if [[ -n "$active_timer_id" && "$active_timer_id" != "null" ]]; then
    api_call "POST" "$API_BASE/Timers/stop" "$new_user_token"
    expect_status "200" "Stop pre-existing active timer"
  fi

  echo "[INFO] Create manual time entry (happy path)"
  api_call "POST" "$API_BASE/TimeEntries" "$new_user_token" "{\"projectId\":$project_id,\"entryDate\":\"2035-01-15\",\"startTimeUtc\":\"2035-01-15T10:00:00Z\",\"endTimeUtc\":\"2035-01-15T12:00:00Z\",\"description\":\"Manual script test\",\"isBillable\":true}"
  expect_status "200" "Create time entry"
  local create_status="$RESP_CODE"

  echo "[INFO] Create overlapping manual time entry (negative path)"
  if [[ "$create_status" == "200" ]]; then
    api_call "POST" "$API_BASE/TimeEntries" "$new_user_token" "{\"projectId\":$project_id,\"entryDate\":\"2035-01-15\",\"startTimeUtc\":\"2035-01-15T11:00:00Z\",\"endTimeUtc\":\"2035-01-15T13:00:00Z\",\"description\":\"Overlap test\",\"isBillable\":false}"
    expect_status "400" "Overlapping entry blocked"
  else
    fail "Overlapping entry blocked" "Skipped because initial create did not succeed"
  fi

  echo "[INFO] Start timer"
  api_call "POST" "$API_BASE/Timers/start" "$new_user_token" "{\"projectId\":$project_id,\"description\":\"Timer test\",\"isBillable\":true}"
  expect_status "200" "Start timer"

  echo "[INFO] Starting second timer must fail"
  api_call "POST" "$API_BASE/Timers/start" "$new_user_token" "{\"projectId\":$project_id,\"description\":\"Timer second start\",\"isBillable\":true}"
  expect_status "400" "Second active timer blocked"

  echo "[INFO] Stop timer"
  api_call "POST" "$API_BASE/Timers/stop" "$new_user_token"
  expect_status "200" "Stop timer"

  print_summary
}

main "$@"
