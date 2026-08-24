using Rbac.Domain.Common;

namespace Rbac.Domain.Entities;

/// <summary>
/// Optional host application catalog. Programs and menus can belong to an application.
/// </summary>
public class RbacApplication : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RbacProgram> Programs { get; set; } = new List<RbacProgram>();
    public ICollection<RbacMenu> Menus { get; set; } = new List<RbacMenu>();
}
