# Architecture

General RBAC is **permission-centric**. Authentication is a host concern. The library maps an already-authenticated subject (`ExternalId`) to an authorization user and decides ALLOW / DENY.

```text
Authentication (host JWT / OIDC / Identity)
        │
        │ UserId / sub  →  RbacUser.ExternalId
        ▼
     RBAC library
        │
        ├── Roles ──► RolePermissions ──► Permissions
        ├── UserPermissions (optional grant or deny)
        ├── Programs (feature catalog; do not grant access)
        └── Menus (navigation; filtered by permissions)
```

## Non-negotiable rules

1. **Menus never authorize APIs.** A visible screen is not a security boundary.
2. **Programs describe features.** `ProgramPermissions` says which actions belong to a feature; roles still have to grant them.
3. **Default deny.** Missing permission is DENY.
4. **Precedence** (already implemented, even though V1 seed data mostly uses Allow):

```text
Explicit user DENY
  → Explicit user ALLOW
    → Role DENY
      → Role ALLOW
        → Default DENY
```

5. **Permission code** is always `resource.action`, with optional namespaced resources (`rbac.users.read`).

## Layering

| Project | Responsibility |
| --- | --- |
| `Rbac.Domain` | Entities, `PermissionCode`, `AuthorizationEvaluator` |
| `Rbac.Contracts` | DTOs shared with hosts and the React sample |
| `Rbac.Application` | Authorization + admin use cases |
| `Rbac.Infrastructure` | EF Core, SQL Server / SQLite / InMemory, cache, seed |
| `Rbac.AspNetCore` | `[AuthorizePermission]`, policy handler, optional admin API |

Host applications own:

- Password / OIDC / JWT issuance
- Their own business APIs
- Which admin endpoints they map

## Request flow

```text
HTTP request
  → host authentication
  → RbacUserMiddleware (ExternalId → RbacUser.Id)
  → [AuthorizePermission("passenger.read")]
  → PermissionAuthorizationHandler
  → IRbacAuthorizationService (cache then database)
  → ALLOW continues / DENY 403
```

## V1 vs later

- **V1:** users, roles, permissions, direct user grants/denies, programs, menus, SQL Server + SQLite, ASP.NET Core attributes, React helpers, in-memory cache, management audit log.
- **V2:** Redis cache, richer admin UI, configurable decision logging.
- **V3:** first-class tenant isolation in every query, resource-level ACLs.
- **V4:** NuGet/npm packaging polish and additional host samples.
