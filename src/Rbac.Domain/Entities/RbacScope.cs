using Rbac.Domain.Common;

namespace Rbac.Domain.Entities;

/// <summary>
/// Optional constraint on a permission grant (airport, department, organization, …).
/// Null scope on a grant means ALL.
/// </summary>
public class RbacScope : AuditableEntity
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ScopeType { get; set; } = "custom";
    public bool IsActive { get; set; } = true;

    public RbacTenant? Tenant { get; set; }
}
