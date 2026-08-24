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

## Quick start (sample)

```bash
# API (SQLite file rbac.sample.db is created automatically)
dotnet run --project samples/Rbac.Sample.Api

# React (proxies /api to http://localhost:5265)
cd samples/Rbac.Sample.React
npm install
npm run dev
```

Open http://localhost:5173

| Username | Password | What you should see |
| --- | --- | --- |
| `officer` | `Passw0rd!` | Create passengers, no export, no admin |
| `viewer` | `Passw0rd!` | Read only |
| `supervisor` | `Passw0rd!` | Passenger + report export |
| `john` | `Passw0rd!` | Same as supervisor except **report.export is denied** |
| `admin` | `Passw0rd!` | Admin catalog + passengers |
| `superadmin` | `Passw0rd!` | Full `rbac.*` catalog |

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

Default deny.

```text
User DENY → User ALLOW → Role DENY → Role ALLOW → DENY
```

A menu item linked to a program is shown only if the user has **any** permission on that program. Groups show when a child is visible. Dashboard-style items with no program are visible to any authenticated user.

## SQL Server

Production schema: [`database/sqlserver/001_create_schema.sql`](database/sqlserver/001_create_schema.sql)

Docker:

```bash
docker compose -f docker/docker-compose.yml up -d
# then set ConnectionStrings:SqlServer in the sample
```

## Tests

```bash
dotnet test Rbac.sln
```

## License

MIT
