# Docker

The sample stack is API + React behind nginx. SQLite is the default database so `docker compose up` does not need SQL Server.

```bash
docker compose up --build
# or: make docker
```

Open http://localhost:8080

| Username | Password |
| --- | --- |
| `officer` | `Passw0rd!` |
| `superadmin` | `Passw0rd!` |

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
