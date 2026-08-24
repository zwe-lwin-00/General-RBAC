# General RBAC

Reusable **permission-centric RBAC** for .NET 8 and React.

Build authorization once. Drop it into immigration systems, commerce apps, and internal admin tools without rewriting roles, menus, or `[AuthorizePermission]` every time.

Authentication stays in the host (JWT, Entra ID, Keycloak, ASP.NET Identity). This library maps that identity onto **users, roles, permissions, programs, and menus**.

```text
User  →  Role  →  Permission  →  Authorization decision
Menu  →  Program              →  Navigation / feature catalog
```

Menus never authorize APIs. Permissions do.

## What V1 includes

- Users mapped by `ExternalId` (no passwords in this repo)
- Roles, permissions (`resource.action`), optional direct user grants **and denies**
- Programs (feature modules) and hierarchical menus
- ASP.NET Core `[AuthorizePermission("passenger.read")]` and `HasPermissionAsync`
- Optional admin API (`/api/rbac/...` and `/api/rbac/me`)
- EF Core on SQL Server, SQLite, or InMemory
- React package: `RbacProvider`, `HasPermission`, `PermissionRoute`, `usePermission`
- Working sample API + React console with demo users

Designed for later: scopes, tenants, resource-level ACLs, Redis. Schema columns exist so V3 is not a rewrite.

## Repository layout

```text
src/Rbac.Domain            entities + evaluator (no EF, no ASP.NET)
src/Rbac.Contracts         DTOs
src/Rbac.Application       authorization + admin services
src/Rbac.Infrastructure    EF Core, cache, seed
src/Rbac.AspNetCore        attributes, handler, MapRbac()
samples/Rbac.Sample.Api    JWT host + passenger/report APIs
samples/Rbac.Sample.React  React console
packages/rbac-react        reusable React helpers
database/sqlserver         canonical SQL Server script
docs/                      architecture and ERD
```

## Quick start (Docker)

```bash
git pull
docker compose up --build
# or: make docker          # detached, then runs a smoke test
# or: make docker-reset    # wipe SQLite and start clean
```

Open http://localhost:8080 — nginx serves the React console and proxies `/api` to the .NET API.

| Username | Password | What you should see |
| --- | --- | --- |
| `officer` | `Passw0rd!` | Create passengers, no export, no admin |
| `viewer` | `Passw0rd!` | Read only |
| `supervisor` | `Passw0rd!` | Passenger + report export |
| `john` | `Passw0rd!` | Same as supervisor except **report.export is denied** |
| `admin` | `Passw0rd!` | Admin catalog + passengers |
| `superadmin` | `Passw0rd!` | Full `rbac.*` catalog |

Best first pass: `officer` (buttons gated), then `john` vs `supervisor` on Reports, then `superadmin` (Administration).

```bash
make docker-test    # API permission smoke test
docker compose down
```

Details: [`docker/README.md`](docker/README.md)

## Quick start (local SDK)

```bash
# API (SQLite file rbac.sample.db is created automatically)
dotnet run --project samples/Rbac.Sample.Api

# React (proxies /api to http://localhost:5265)
cd samples/Rbac.Sample.React
npm install
npm run dev
```

Open http://localhost:5173. Same demo users as above.

## Use in another .NET API

```csharp
builder.Services.AddAuthentication(/* your JWT / OIDC */).AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddRbac(
    options => options.ExternalIdClaimType = "sub",
    infrastructure =>
    {
        infrastructure.SqlServerConnectionString = builder.Configuration.GetConnectionString("Rbac");
        infrastructure.EnsureCreated = false;
        infrastructure.ApplyMigrations = true;
        infrastructure.Seed.SeedSystemCatalog = true;
        infrastructure.Seed.SeedDemoData = false;
    });

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseRbac();
app.MapRbac(); // optional admin API
```

Protect a business endpoint:

```csharp
app.MapGet("/api/passengers", handler)
   .RequireAuthorization()
   .RequirePermission("passenger.read");
```

Or MVC:

```csharp
[AuthorizePermission("passenger.export")]
[HttpGet("export")]
public IActionResult Export() => Ok();
```

Programmatic check:

```csharp
if (await authorization.HasPermissionAsync(userId, "passenger.update"))
{
    // ...
}
```

The host must create `RbacUser` rows whose `ExternalId` matches the authenticated `sub` / nameidentifier.

## Use in React

```tsx
<HasPermission permission="passenger.create">
  <button>Create passenger</button>
</HasPermission>

<PermissionRoute permission="passenger.read">
  <PassengersPage />
</PermissionRoute>
```

Load `/api/rbac/me` after login and pass `permissions` + `menus` into `RbacProvider`. UI hiding is convenience; the API is the security boundary.

## Authorization rules

Default deny. Fail closed.

```text
User DENY → User ALLOW → Role DENY → Role ALLOW → DENY
```

Unscoped API checks match **only global grants**. A scoped grant (one airport, one department) does not authorize a global endpoint.

Administration is least-privilege: callers can only assign permissions they already hold. System roles cannot be rewritten, deactivated, or assigned except by a system administrator. The last system admin cannot be removed.

A menu item linked to a program is shown only if the user has **any global** permission on that program. See [`docs/security.md`](docs/security.md).

## SQL Server

Production schema: [`database/sqlserver/001_create_schema.sql`](database/sqlserver/001_create_schema.sql)

```bash
docker compose -f docker-compose.yml -f docker/docker-compose.sqlserver.yml up --build
```

## Tests

```bash
dotnet test Rbac.sln
```

## License

MIT
