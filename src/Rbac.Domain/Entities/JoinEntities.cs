using Rbac.Domain.Common;
using Rbac.Domain.Enums;

namespace Rbac.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; set; }

    public RbacUser User { get; set; } = null!;
    public RbacRole Role { get; set; } = null!;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Guid? ScopeId { get; set; }
    public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; set; }

    public RbacRole Role { get; set; } = null!;
    public RbacPermission Permission { get; set; } = null!;
    public RbacScope? Scope { get; set; }
}

public class UserPermission
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    public Guid? ScopeId { get; set; }
    public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; set; }

    public RbacUser User { get; set; } = null!;
    public RbacPermission Permission { get; set; } = null!;
    public RbacScope? Scope { get; set; }
}

public class ProgramPermission
{
    public Guid ProgramId { get; set; }
    public Guid PermissionId { get; set; }

    public RbacProgram Program { get; set; } = null!;
    public RbacPermission Permission { get; set; } = null!;
}

public class UserTenant
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; set; }

    public RbacUser User { get; set; } = null!;
    public RbacTenant Tenant { get; set; } = null!;
}

public class RoleTenant
{
    public Guid RoleId { get; set; }
    public Guid TenantId { get; set; }

    public RbacRole Role { get; set; } = null!;
    public RbacTenant Tenant { get; set; } = null!;
}
