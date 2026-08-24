namespace Rbac.Domain;

/// <summary>
/// Built-in permission codes used by the optional admin API. Host apps add their own.
/// </summary>
public static class RbacPermissions
{
    public const string UsersRead = "rbac.users.read";
    public const string UsersCreate = "rbac.users.create";
    public const string UsersUpdate = "rbac.users.update";
    public const string UsersDelete = "rbac.users.delete";

    public const string RolesRead = "rbac.roles.read";
    public const string RolesCreate = "rbac.roles.create";
    public const string RolesUpdate = "rbac.roles.update";
    public const string RolesDelete = "rbac.roles.delete";

    public const string PermissionsRead = "rbac.permissions.read";
    public const string PermissionsCreate = "rbac.permissions.create";
    public const string PermissionsUpdate = "rbac.permissions.update";
    public const string PermissionsDelete = "rbac.permissions.delete";

    public const string ProgramsRead = "rbac.programs.read";
    public const string ProgramsCreate = "rbac.programs.create";
    public const string ProgramsUpdate = "rbac.programs.update";
    public const string ProgramsDelete = "rbac.programs.delete";

    public const string MenusRead = "rbac.menus.read";
    public const string MenusCreate = "rbac.menus.create";
    public const string MenusUpdate = "rbac.menus.update";
    public const string MenusDelete = "rbac.menus.delete";

    public const string AssignRoles = "rbac.assign.roles";
    public const string AssignPermissions = "rbac.assign.permissions";
}
