namespace Rbac.Domain.Authorization;

/// <summary>
/// Privilege-escalation rules for administration. Authorization checks remain default-deny.
/// </summary>
public static class PrivilegeRules
{
    public const string UnmappedActor = "The caller is not mapped to an RBAC user.";
    public const string GrantExceedsHolder = "You can only assign permissions you already hold.";
    public const string SystemRoleReserved = "System roles can only be assigned by a system administrator.";
    public const string SystemRoleImmutable = "System role permission sets cannot be changed.";
    public const string SystemRoleActiveLocked = "System roles cannot be deactivated.";
    public const string SystemPermissionLocked = "System permissions cannot be deactivated or deleted.";
    public const string LastSystemAdmin = "Cannot remove or deactivate the last active system administrator.";
    public const string InactiveAssignment = "Inactive roles or permissions cannot be assigned.";

    public static bool CanGrantPermission(
        bool actorIsSystemProcess,
        bool actorHoldsSystemRole,
        IReadOnlySet<string> actorPermissions,
        string permissionCode)
    {
        if (actorIsSystemProcess || actorHoldsSystemRole)
        {
            return true;
        }

        return actorPermissions.Contains(permissionCode);
    }

    public static bool CanAssignRole(
        bool actorIsSystemProcess,
        bool actorHoldsSystemRole,
        IReadOnlySet<string> actorPermissions,
        bool targetIsSystemRole,
        IReadOnlySet<string> targetRolePermissionCodes)
    {
        if (actorIsSystemProcess || actorHoldsSystemRole)
        {
            return true;
        }

        if (targetIsSystemRole)
        {
            return false;
        }

        return targetRolePermissionCodes.All(actorPermissions.Contains);
    }

    /// <summary>
    /// Unscoped checks match only global grants. Scoped checks match a global grant or that exact scope.
    /// </summary>
    public static bool ScopeMatches(Guid? grantScopeId, Guid? requiredScopeId)
    {
        if (requiredScopeId is null)
        {
            return grantScopeId is null;
        }

        return grantScopeId is null || grantScopeId == requiredScopeId;
    }
}
