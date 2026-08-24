using Microsoft.EntityFrameworkCore;
using Rbac.Application.Abstractions;
using Rbac.Contracts;
using Rbac.Domain.Authorization;
using Rbac.Domain.Enums;

namespace Rbac.Application.Services;

public sealed class AssignmentGuard
{
    private readonly IRbacDbContext _db;
    private readonly IRbacActor _actor;
    private readonly IRbacAuthorizationService _authorization;

    public AssignmentGuard(IRbacDbContext db, IRbacActor actor, IRbacAuthorizationService authorization)
    {
        _db = db;
        _actor = actor;
        _authorization = authorization;
    }

    public async Task<Result> EnsureCanAssignRolesAsync(IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken)
    {
        var actor = await LoadActorAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return Result.Fail(actor.Error!, actor.ErrorCode);
        }

        var roles = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Where(r => roleIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (roles.Count != roleIds.Distinct().Count())
        {
            return Result.Fail("One or more roles were not found.", "not_found");
        }

        if (roles.Exists(r => !r.IsActive))
        {
            return Result.Fail(PrivilegeRules.InactiveAssignment, "validation");
        }

        foreach (var role in roles)
        {
            var codes = role.RolePermissions
                .Where(rp => rp.Permission is { IsDeleted: false, IsActive: true })
                .Select(rp => rp.Permission.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!PrivilegeRules.CanAssignRole(
                    actor.Value!.IsSystemProcess,
                    actor.Value.HoldsSystemRole,
                    actor.Value.Permissions,
                    role.IsSystemRole,
                    codes))
            {
                return Result.Fail(
                    role.IsSystemRole ? PrivilegeRules.SystemRoleReserved : PrivilegeRules.GrantExceedsHolder,
                    "forbidden");
            }
        }

        return Result.Ok();
    }

    public async Task<Result> EnsureCanGrantPermissionsAsync(
        IReadOnlyList<(Guid PermissionId, PermissionEffect Effect, Guid? ScopeId)> assignments,
        CancellationToken cancellationToken)
    {
        var actor = await LoadActorAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return Result.Fail(actor.Error!, actor.ErrorCode);
        }

        var ids = assignments.Select(a => a.PermissionId).Distinct().ToList();
        var permissions = await _db.Permissions
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        if (permissions.Count != ids.Count)
        {
            return Result.Fail("One or more permissions were not found.", "not_found");
        }

        if (permissions.Exists(p => !p.IsActive))
        {
            return Result.Fail(PrivilegeRules.InactiveAssignment, "validation");
        }

        var scopeIds = assignments.Where(a => a.ScopeId.HasValue).Select(a => a.ScopeId!.Value).Distinct().ToList();
        if (scopeIds.Count > 0)
        {
            var validScopes = await _db.Scopes.CountAsync(
                s => scopeIds.Contains(s.Id) && s.IsActive && !s.IsDeleted,
                cancellationToken);
            if (validScopes != scopeIds.Count)
            {
                return Result.Fail("One or more scopes were not found or inactive.", "validation");
            }
        }

        foreach (var permission in permissions)
        {
            if (!PrivilegeRules.CanGrantPermission(
                    actor.Value!.IsSystemProcess,
                    actor.Value.HoldsSystemRole,
                    actor.Value.Permissions,
                    permission.Code))
            {
                return Result.Fail(PrivilegeRules.GrantExceedsHolder, "forbidden");
            }
        }

        return Result.Ok();
    }

    public async Task<Result> EnsureNotLastSystemAdminAsync(
        Guid targetUserId,
        IReadOnlyCollection<Guid>? replacementRoleIds,
        bool deactivatingOrDeleting,
        CancellationToken cancellationToken)
    {
        var currentlyAdmin = await IsActiveSystemAdminAsync(targetUserId, cancellationToken);
        if (!currentlyAdmin)
        {
            return Result.Ok();
        }

        var remainsAdmin = !deactivatingOrDeleting && replacementRoleIds is not null &&
                           await RolesIncludeActiveSystemRoleAsync(replacementRoleIds, cancellationToken);
        if (remainsAdmin)
        {
            return Result.Ok();
        }

        var otherAdmins = await CountOtherActiveSystemAdminsAsync(targetUserId, cancellationToken);
        if (otherAdmins == 0)
        {
            return Result.Fail(PrivilegeRules.LastSystemAdmin, "forbidden");
        }

        return Result.Ok();
    }

    private async Task<Result<ActorContext>> LoadActorAsync(CancellationToken cancellationToken)
    {
        if (_actor.IsSystemProcess)
        {
            return Result.Ok(new ActorContext(true, true, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
        }

        if (_actor.UserId is null)
        {
            return Result.Fail<ActorContext>(PrivilegeRules.UnmappedActor, "forbidden");
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _actor.UserId && !u.IsDeleted, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Fail<ActorContext>(PrivilegeRules.UnmappedActor, "forbidden");
        }

        var holdsSystemRole = await (
            from ur in _db.UserRoles.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == user.Id && r.IsSystemRole && r.IsActive && !r.IsDeleted
            select r.Id
        ).AnyAsync(cancellationToken);

        var permissions = await _authorization.GetEffectivePermissionsAsync(user.Id, cancellationToken: cancellationToken);
        return Result.Ok(new ActorContext(false, holdsSystemRole, permissions));
    }

    private async Task<bool> IsActiveSystemAdminAsync(Guid userId, CancellationToken cancellationToken) =>
        await (
            from u in _db.Users.AsNoTracking()
            join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where u.Id == userId && u.IsActive && !u.IsDeleted && r.IsSystemRole && r.IsActive && !r.IsDeleted
            select r.Id
        ).AnyAsync(cancellationToken);

    private async Task<int> CountOtherActiveSystemAdminsAsync(Guid userId, CancellationToken cancellationToken) =>
        await (
            from u in _db.Users.AsNoTracking()
            join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where u.Id != userId && u.IsActive && !u.IsDeleted && r.IsSystemRole && r.IsActive && !r.IsDeleted
            select u.Id
        ).Distinct().CountAsync(cancellationToken);

    private async Task<bool> RolesIncludeActiveSystemRoleAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken) =>
        await _db.Roles.AsNoTracking().AnyAsync(
            r => roleIds.Contains(r.Id) && r.IsSystemRole && r.IsActive && !r.IsDeleted,
            cancellationToken);

    private sealed record ActorContext(bool IsSystemProcess, bool HoldsSystemRole, IReadOnlySet<string> Permissions);
}
