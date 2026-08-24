using Rbac.Domain.Common;
using Rbac.Domain.ValueObjects;

namespace Rbac.Domain.Entities;

/// <summary>
/// An action a user may perform. Code is always <c>resource.action</c>.
/// </summary>
public class RbacPermission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsSystemPermission { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public ICollection<ProgramPermission> ProgramPermissions { get; set; } = new List<ProgramPermission>();

    public void SetCode(PermissionCode code)
    {
        Resource = code.Resource;
        Action = code.Action;
        Code = code.Value;
    }
}
