# Security model

General RBAC is **fail closed**. A missing, inactive, unmapped, or malformed grant is DENY.

## Boundaries

| Layer | Trust |
| --- | --- |
| React `HasPermission` / route guards | UX only. Never a security boundary. |
| Menus | Navigation only. A visible item does not authorize APIs. |
| Programs | Feature catalog. They do not grant access. |
| `[AuthorizePermission]` / `HasPermissionAsync` | Security boundary. |

## Evaluation

1. User inactive or missing → DENY  
2. Explicit user DENY → DENY  
3. Explicit user ALLOW → ALLOW  
4. Role DENY → DENY  
5. Role ALLOW → ALLOW  
6. Default DENY  

Unscoped checks (`RequirePermission("passenger.read")`) match **only global grants** (`ScopeId` is null). A grant limited to one airport/department does **not** authorize a global API. Scoped checks match a global grant or that exact scope.

## Administration

HTTP callers are never a system process.

- You can only assign permissions you already hold, unless you have an active **system role**.
- System roles can only be assigned by a system administrator.
- System role permission sets cannot be rewritten through the API.
- System roles/permissions cannot be deactivated.
- The last active system administrator cannot be removed or deactivated.
- Inactive roles, permissions, and scopes cannot be assigned.

Seed/background code uses `SystemRbacActor` (`IsSystemProcess = true`) and is not exposed over HTTP.

## Caching

Effective permissions are cached per user with a generation token. Role, user, and permission catalog changes invalidate the cache. Callers receive a copy so they cannot mutate cached state. `AuthorizeAsync` always reads grants from the database (not the code list cache) for the decision itself.

## Host responsibilities

- Issue and validate JWTs (issuer, audience, lifetime, signature).
- Map `sub` onto `RbacUser.ExternalId`.
- Do not put permission codes in tokens as the source of truth.
- Narrow CORS to real UI origins in production.
- Use SQL Server (or equivalent) with migrations; do not enable `EnsureCreated` in production.
- Pass `AuthorizationContext.ScopeId` for scoped resources; do not take scope from an untrusted client header without validation.
