using Microsoft.EntityFrameworkCore;
using Rbac.Domain.Entities;

namespace Rbac.Application.Abstractions;

public interface IRbacDbContext
{
    DbSet<RbacUser> Users { get; }
    DbSet<RbacRole> Roles { get; }
    DbSet<RbacPermission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<RbacApplication> Applications { get; }
    DbSet<RbacProgram> Programs { get; }
    DbSet<ProgramPermission> ProgramPermissions { get; }
    DbSet<RbacMenu> Menus { get; }
    DbSet<RbacTenant> Tenants { get; }
    DbSet<UserTenant> UserTenants { get; }
    DbSet<RoleTenant> RoleTenants { get; }
    DbSet<RbacScope> Scopes { get; }
    DbSet<RbacResource> Resources { get; }
    DbSet<AuthorizationAuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IRbacAuthorizationService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        AuthorizationContext? context = null,
        CancellationToken cancellationToken = default);

    Task<Domain.Authorization.AuthorizationDecision> AuthorizeAsync(
        Guid userId,
        string permissionCode,
        AuthorizationContext? context = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid userId,
        AuthorizationContext? context = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuthorizationContext
{
    public Guid? TenantId { get; init; }
    public Guid? ScopeId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
}

public interface IPermissionCache
{
    Task<IReadOnlySet<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetAsync(Guid userId, IReadOnlySet<string> permissions, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RemoveUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}

public interface IAuditWriter
{
    Task WriteAsync(
        Domain.Enums.AuditEventType eventType,
        string targetType,
        Guid? targetId,
        string? oldValue,
        string? newValue,
        CancellationToken cancellationToken = default);
}

public interface IRbacActor
{
    string? Name { get; }
    Guid? UserId { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }

    /// <summary>
    /// True for seed/tests/background jobs. HTTP callers must never be a system process.
    /// </summary>
    bool IsSystemProcess { get; }
}

public interface IRbacUserResolver
{
    Task<RbacUser?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<RbacUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
