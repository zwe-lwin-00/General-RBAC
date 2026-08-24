#!/usr/bin/env bash
set -euo pipefail

BASE="${BASE:-http://localhost:8080}"

echo "Waiting for $BASE ..."
for i in $(seq 1 60); do
  if curl -fsS "$BASE/api/health" >/dev/null 2>&1 && curl -fsS "$BASE/" >/dev/null 2>&1; then
    echo "Stack is up."
    break
  fi
  if [ "$i" -eq 60 ]; then
    echo "Timed out waiting for $BASE"
    exit 1
  fi
  sleep 2
done

python3 - "$BASE" <<'PY'
import json, sys, urllib.error, urllib.request

base = sys.argv[1]

def req(method, path, token=None, body=None):
    data = None if body is None else json.dumps(body).encode()
    headers = {}
    if body is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(base + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            return response.status, response.read().decode()
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode()

def login(username):
    code, raw = req("POST", "/api/auth/login", body={"username": username, "password": "Passw0rd!"})
    if code != 200:
        raise SystemExit(f"login {username} failed: {code} {raw}")
    return json.loads(raw)["accessToken"]

def expect(name, got, want):
    ok = got == want
    print(f"{'OK ' if ok else 'FAIL'} {name}: {got} (want {want})")
    return ok

ok = True
ok &= expect("GET /", req("GET", "/")[0], 200)
ok &= expect("health", req("GET", "/api/health")[0], 200)
ok &= expect("bad password", req("POST", "/api/auth/login", body={"username": "officer", "password": "wrong"})[0], 401)

officer = login("officer")
ok &= expect("officer passengers", req("GET", "/api/passengers", officer)[0], 200)
ok &= expect("officer export", req("GET", "/api/passengers/export", officer)[0], 403)
ok &= expect("officer admin", req("GET", "/api/rbac/users", officer)[0], 403)

viewer = login("viewer")
ok &= expect("viewer create", req("POST", "/api/passengers", viewer, {"fullName": "Nope", "documentNo": "X", "nationality": "MM"})[0], 403)

supervisor = login("supervisor")
john = login("john")
ok &= expect("supervisor report export", req("GET", "/api/reports/export", supervisor)[0], 200)
ok &= expect("john report export", req("GET", "/api/reports/export", john)[0], 403)

admin = login("admin")
ok &= expect("admin users", req("GET", "/api/rbac/users", admin)[0], 200)

if not ok:
    raise SystemExit(1)

print()
print("Ready to test in the browser:")
print(f"  UI  {base}")
print(f"  API {base}/api/health")
print("  Password for every demo user: Passw0rd!")
print("  Try officer, then john vs supervisor on Reports, then superadmin.")
PY
