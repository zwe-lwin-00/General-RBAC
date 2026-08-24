using Rbac.Domain.Common;

namespace Rbac.Domain.Entities;

/// <summary>
/// Organization boundary for SaaS hosts. V1 stores membership; filtering is opt-in via context.
/// </summary>
public class RbacTenant : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public ICollection<RoleTenant> RoleTenants { get; set; } = new List<RoleTenant>();
}
