# ERD and cardinality

```text
RbacTenant 1───* RbacUser          (via RbacUserTenants, M:N)
RbacTenant 1───* RbacRole          (optional TenantId; also M:N via RbacRoleTenants)

RbacUser   M───N RbacRole          (RbacUserRoles)
RbacRole   M───N RbacPermission    (RbacRolePermissions, Effect Allow|Deny, optional Scope)
RbacUser   M───N RbacPermission    (RbacUserPermissions, Effect Allow|Deny, optional Scope)

RbacApplication 1───* RbacProgram
RbacApplication 1───* RbacMenu
RbacProgram     M───N RbacPermission   (RbacProgramPermissions)  // catalog only
RbacMenu        *───1 RbacMenu          (ParentId hierarchy)
RbacMenu        *───0..1 RbacProgram

RbacPermission  *───0..1 RbacScope      (on grants, not on the permission row)
RbacResource                             // V3 record-level placeholder
```

## Soft delete

Catalog entities (`Users`, `Roles`, `Permissions`, `Programs`, `Menus`, `Tenants`, `Applications`, `Scopes`, `Resources`) have `IsDeleted`, `DeletedAt`, `DeletedBy`. Unique indexes are filtered to `IsDeleted = 0` so a code can be reused after delete.

Join tables are hard-deleted with the parent (ON DELETE CASCADE).

## Seed philosophy

System permissions (`rbac.*`) and the Super Admin role are catalog. Demo passengers/reports/users are opt-in via `RbacSeedOptions.SeedDemoData` so a real host does not inherit sample accounts.
