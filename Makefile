.PHONY: docker docker-up docker-down docker-sqlserver test

## Run the sample API + React UI (SQLite).
## UI: http://localhost:8080   API: http://localhost:5265
docker docker-up:
	docker compose up --build

docker-down:
	docker compose down

## Same stack with SQL Server instead of SQLite.
docker-sqlserver:
	docker compose -f docker-compose.yml -f docker/docker-compose.sqlserver.yml up --build

test:
	dotnet test tests/Rbac.Tests/Rbac.Tests.csproj
