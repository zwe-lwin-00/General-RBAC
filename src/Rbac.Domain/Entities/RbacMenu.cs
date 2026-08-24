using Rbac.Domain.Common;
using Rbac.Domain.Enums;

namespace Rbac.Domain.Entities;

/// <summary>
/// Navigation node. Visibility is derived from the current user's permissions, never the reverse.
/// </summary>
public class RbacMenu : AuditableEntity
{
    public Guid? ApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? ProgramId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public MenuType MenuType { get; set; } = MenuType.Item;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public RbacApplication? Application { get; set; }
    public RbacTenant? Tenant { get; set; }
    public RbacMenu? Parent { get; set; }
    public RbacProgram? Program { get; set; }
    public ICollection<RbacMenu> Children { get; set; } = new List<RbacMenu>();
}
