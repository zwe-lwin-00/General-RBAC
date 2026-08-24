using Rbac.Domain.Common;

namespace Rbac.Domain.Entities;

/// <summary>
/// Authorization identity only. Passwords, MFA, and token issuance live in the host auth system.
/// Map the authenticated principal via <see cref="ExternalId"/>.
/// </summary>
public class RbacUser : AuditableEntity
{
    public string ExternalId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}
