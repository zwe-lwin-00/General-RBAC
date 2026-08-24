using Rbac.Domain.Common;

namespace Rbac.Domain.Entities;

/// <summary>
/// Placeholder for record-level authorization (V3). Not used in V1 evaluation.
/// </summary>
public class RbacResource : AuditableEntity
{
    public Guid? TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceKey { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
}
