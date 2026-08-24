FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Do not copy global.json — it pins a host SDK the image may not have.
COPY Directory.Build.props ./
COPY src/Rbac.Domain/Rbac.Domain.csproj src/Rbac.Domain/
COPY src/Rbac.Contracts/Rbac.Contracts.csproj src/Rbac.Contracts/
COPY src/Rbac.Application/Rbac.Application.csproj src/Rbac.Application/
COPY src/Rbac.Infrastructure/Rbac.Infrastructure.csproj src/Rbac.Infrastructure/
COPY src/Rbac.AspNetCore/Rbac.AspNetCore.csproj src/Rbac.AspNetCore/
COPY samples/Rbac.Sample.Api/Rbac.Sample.Api.csproj samples/Rbac.Sample.Api/
RUN dotnet restore samples/Rbac.Sample.Api/Rbac.Sample.Api.csproj

COPY src/ src/
COPY samples/Rbac.Sample.Api/ samples/Rbac.Sample.Api/
RUN dotnet publish samples/Rbac.Sample.Api/Rbac.Sample.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gosu \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data \
    && chown -R $APP_UID:$APP_UID /app/data

COPY --from=build /app/publish .
COPY docker/api-entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Sqlite="Data Source=/app/data/rbac.sample.db"

EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=12 \
    CMD curl -fsS http://127.0.0.1:8080/api/health || exit 1

# Entrypoint starts as root so it can chown the SQLite volume, then drops to $APP_UID.
ENTRYPOINT ["/entrypoint.sh"]
