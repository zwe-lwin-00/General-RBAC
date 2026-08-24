using Rbac.Domain.Common;

namespace Rbac.Domain.Entities;

/// <summary>
/// An application feature/module. Programs describe the product; they do not grant access.
/// </summary>
public class RbacProgram : AuditableEntity
{
    public Guid? ApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public string? Version { get; set; }
    public bool IsActive { get; set; } = true;

    public RbacApplication? Application { get; set; }
    public RbacTenant? Tenant { get; set; }
    public ICollection<ProgramPermission> ProgramPermissions { get; set; } = new List<ProgramPermission>();
    public ICollection<RbacMenu> Menus { get; set; } = new List<RbacMenu>();
}
