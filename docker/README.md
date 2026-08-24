# Docker

The sample stack is API + React behind nginx. SQLite is the default database so `docker compose up` does not need SQL Server.

```bash
git pull
docker compose up --build
# or: make docker
```

If you already ran an older image, rebuild cleanly:

```bash
docker compose down -v
docker compose up --build
```

Open http://localhost:8080

| Username | Password | What to check |
| --- | --- | --- |
| `officer` | `Passw0rd!` | Create passenger works. Export / Delete / Administration are hidden. `/admin/users` → Missing permission. |
| `viewer` | `Passw0rd!` | Read only. No create form. |
| `supervisor` | `Passw0rd!` | Export report is visible and works. |
| `john` | `Passw0rd!` | Same as supervisor, but Reports hides export (user DENY on `report.export`). |
| `admin` | `Passw0rd!` | Administration catalog + passengers. |
| `superadmin` | `Passw0rd!` | Full `rbac.*` catalog. |

Automated check after the stack is up:

```bash
make docker-test
```

- UI: http://localhost:8080 (nginx serves React and proxies `/api` to the API)
- API: http://localhost:5265 (optional direct access)

The browser talks to one origin, so the UI does not need CORS for `/api`. SQLite data is stored in the `rbac-data` volume.

```bash
docker compose down
docker compose down -v   # also delete the SQLite volume
```

## SQL Server instead of SQLite

```bash
docker compose -f docker-compose.yml -f docker/docker-compose.sqlserver.yml up --build
```

The sample password is for local demos only.

## Images

| File | Image |
| --- | --- |
| `docker/api.Dockerfile` | .NET 8 sample API |
| `docker/web.Dockerfile` | nginx + production React build |
| `docker/nginx.conf` | `/` static files, `/api/` reverse proxy |
