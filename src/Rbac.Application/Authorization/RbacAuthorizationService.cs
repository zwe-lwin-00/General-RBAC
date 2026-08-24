using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rbac.Application.Abstractions;
using Rbac.Domain.Authorization;
using Rbac.Domain.Entities;
using Rbac.Domain.Enums;

namespace Rbac.Application.Authorization;

public sealed class RbacAuthorizationService : IRbacAuthorizationService
{
    private readonly IRbacDbContext _db;
    private readonly IPermissionCache _cache;
    private readonly ILogger<RbacAuthorizationService> _logger;

    public RbacAuthorizationService(
        IRbacDbContext db,
        IPermissionCache cache,
        ILogger<RbacAuthorizationService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        AuthorizationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var decision = await AuthorizeAsync(userId, permissionCode, context, cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<AuthorizationDecision> AuthorizeAsync(
        Guid userId,
        string permissionCode,
        AuthorizationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await LoadSnapshotAsync(userId, cancellationToken);
            if (snapshot is null)
            {
                return AuthorizationDecision.Deny(permissionCode, "User was not found.");
            }

            var decision = AuthorizationEvaluator.Evaluate(
                snapshot.IsActive,
                snapshot.UserGrants,
                snapshot.RoleGrants,
                permissionCode.Trim(),
                context?.ScopeId);

            _logger.LogDebug(
                "Authorization {Decision} for user {UserId} permission {Permission}: {Reason}",
                decision.IsAllowed ? "ALLOW" : "DENY",
                userId,
                permissionCode,
                decision.Reason);

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authorization failed closed for user {UserId}", userId);
            return AuthorizationDecision.Deny(permissionCode, "Authorization error.");
        }
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid userId,
        AuthorizationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (context?.ScopeId is null)
        {
            var cached = await _cache.GetAsync(userId, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var snapshot = await LoadSnapshotAsync(userId, cancellationToken);
        if (snapshot is null || !snapshot.IsActive)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        IReadOnlyCollection<PermissionGrant> userGrants = snapshot.UserGrants;
        IReadOnlyCollection<PermissionGrant> roleGrants = snapshot.RoleGrants;
        var effective = AuthorizationEvaluator.ComputeEffectiveAllows(userGrants, roleGrants, context?.ScopeId);
        if (context?.ScopeId is null)
        {
            await _cache.SetAsync(userId, effective, cancellationToken);
        }

        return effective;
    }

    private async Task<GrantSnapshot?> LoadSnapshotAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var userGrants = await (
            from up in _db.UserPermissions.AsNoTracking()
            join p in _db.Permissions.AsNoTracking() on up.PermissionId equals p.Id
            where up.UserId == userId && p.IsActive && !p.IsDeleted
            select new PermissionGrant(p.Code, up.Effect, up.ScopeId)
        ).ToListAsync(cancellationToken);

        var roleGrants = await (
            from ur in _db.UserRoles.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            join rp in _db.RolePermissions.AsNoTracking() on r.Id equals rp.RoleId
            join p in _db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where ur.UserId == userId
                  && r.IsActive && !r.IsDeleted
                  && p.IsActive && !p.IsDeleted
            select new PermissionGrant(p.Code, rp.Effect, rp.ScopeId)
        ).ToListAsync(cancellationToken);

        return new GrantSnapshot(user.IsActive, userGrants, roleGrants);
    }

    private sealed record GrantSnapshot(
        bool IsActive,
        IReadOnlyList<PermissionGrant> UserGrants,
        IReadOnlyList<PermissionGrant> RoleGrants);
}

public sealed class MenuVisibility
{
    public static IReadOnlyList<Rbac.Contracts.MenuDto> FilterTree(
        IReadOnlyList<RbacMenu> menus,
        IReadOnlySet<string> effectivePermissions)
    {
        var byParent = menus
            .GroupBy(m => m.ParentId)
            .ToDictionary(g => g.Key ?? Guid.Empty, g => g.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToList());

        return Build(null);

        IReadOnlyList<Rbac.Contracts.MenuDto> Build(Guid? parentId)
        {
            var key = parentId ?? Guid.Empty;
            if (!byParent.TryGetValue(key, out var children))
            {
                return [];
            }

            var result = new List<Rbac.Contracts.MenuDto>();
            foreach (var menu in children.Where(m => m.IsActive && m.IsVisible && !m.IsDeleted))
            {
                var nested = Build(menu.Id);
                if (!IsVisible(menu, nested, effectivePermissions))
                {
                    continue;
                }

                result.Add(menu.ToDto(nested));
            }

            return result;
        }
    }

    private static bool IsVisible(
        RbacMenu menu,
        IReadOnlyList<Rbac.Contracts.MenuDto> visibleChildren,
        IReadOnlySet<string> effectivePermissions)
    {
        if (menu.ProgramId is null)
        {
            if (visibleChildren.Count > 0)
            {
                return true;
            }

            // Groups stay hidden until a child is allowed. Unlinked items (e.g. Dashboard)
            // are shown to any authenticated user.
            return menu.MenuType != Domain.Enums.MenuType.Group;
        }

        var programPermissions = menu.Program?.ProgramPermissions
            .Where(pp => pp.Permission is { IsDeleted: false, IsActive: true })
            .Select(pp => pp.Permission.Code)
            .ToList() ?? [];

        if (programPermissions.Count == 0)
        {
            return false;
        }

        return programPermissions.Any(effectivePermissions.Contains);
    }
}
