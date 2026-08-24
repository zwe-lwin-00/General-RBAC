.PHONY: docker docker-up docker-down docker-reset docker-test docker-sqlserver test

## Build and start the sample (SQLite). UI: http://localhost:8080
docker docker-up:
	docker compose up --build -d
	./scripts/docker-smoke.sh

## Recreate containers and the SQLite volume (fresh seed data).
docker-reset:
	docker compose down -v
	docker compose up --build -d
	./scripts/docker-smoke.sh

docker-down:
	docker compose down

docker-test:
	./scripts/docker-smoke.sh

## Same stack with SQL Server instead of SQLite.
docker-sqlserver:
	docker compose -f docker-compose.yml -f docker/docker-compose.sqlserver.yml up --build -d

test:
	dotnet test Rbac.sln
