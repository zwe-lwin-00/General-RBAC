namespace Rbac.Domain.Enums;

/// <summary>
/// Grant vs explicit deny. V1 seed data uses Allow only; the evaluator already honors Deny.
/// </summary>
public enum PermissionEffect
{
    Allow = 1,
    Deny = 2
}

public enum MenuType
{
    Group = 0,
    Item = 1,
    ExternalLink = 2
}

public enum AuditEventType
{
    RoleCreated = 1,
    RoleUpdated = 2,
    RoleDeleted = 3,
    RolePermissionGranted = 4,
    RolePermissionRevoked = 5,
    UserRoleAssigned = 6,
    UserRoleRemoved = 7,
    UserPermissionGranted = 8,
    UserPermissionRevoked = 9,
    PermissionCreated = 10,
    PermissionUpdated = 11,
    ProgramCreated = 12,
    ProgramUpdated = 13,
    MenuCreated = 14,
    MenuUpdated = 15,
    UserCreated = 16,
    UserUpdated = 17
}
