using Microsoft.EntityFrameworkCore;
using Rbac.Application.Abstractions;
using Rbac.Contracts;
using Rbac.Domain;
using Rbac.Domain.Authorization;
using Rbac.Domain.Entities;
using Rbac.Domain.Enums;
using Rbac.Domain.ValueObjects;

namespace Rbac.Application.Services;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IRbacDbContext _db;
    private readonly IPermissionCache _cache;
    private readonly IAuditWriter _audit;
    private readonly IRbacActor _actor;
    private readonly AssignmentGuard _guard;

    public UserAdminService(
        IRbacDbContext db,
        IPermissionCache cache,
        IAuditWriter audit,
        IRbacActor actor,
        AssignmentGuard guard)
    {
        _db = db;
        _cache = cache;
        _audit = audit;
        _actor = actor;
        _guard = guard;
    }

    public async Task<PagedResult<UserDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Username.Contains(term) ||
                u.DisplayName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                u.ExternalId.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = items.Select(u => u.ToDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<UserDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        return user is null ? Result.Fail<UserDto>("User not found.", "not_found") : Result.Ok(user.ToDto());
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId) || string.IsNullOrWhiteSpace(request.Username))
        {
            return Result.Fail<UserDto>("ExternalId and Username are required.", "validation");
        }

        var exists = await _db.Users.AnyAsync(
            u => !u.IsDeleted && (u.ExternalId == request.ExternalId.Trim() || u.Username == request.Username.Trim()),
            cancellationToken);
        if (exists)
        {
            return Result.Fail<UserDto>("A user with that external id or username already exists.", "conflict");
        }

        var user = new RbacUser
        {
            ExternalId = request.ExternalId.Trim(),
            Username = request.Username.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            IsActive = request.IsActive,
            CreatedBy = _actor.Name
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.UserCreated, nameof(RbacUser), user.Id, null, user.Username, cancellationToken);
        return Result.Ok(user.ToDto());
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return Result.Fail<UserDto>("User not found.", "not_found");
        }

        if (request.DisplayName.Trim().Length == 0)
        {
            return Result.Fail<UserDto>("Display name is required.", "validation");
        }

        var old = $"{user.DisplayName}|{user.IsActive}";
        if (user.IsActive && !request.IsActive)
        {
            var lastAdmin = await _guard.EnsureNotLastSystemAdminAsync(id, null, deactivatingOrDeleting: true, cancellationToken);
            if (!lastAdmin.IsSuccess)
            {
                return Result.Fail<UserDto>(lastAdmin.Error!, lastAdmin.ErrorCode);
            }
        }
        user.DisplayName = request.DisplayName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.IsActive = request.IsActive;
        user.Touch(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(id, cancellationToken);
        await _audit.WriteAsync(AuditEventType.UserUpdated, nameof(RbacUser), id, old, $"{user.DisplayName}|{user.IsActive}", cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return Result.Fail("User not found.", "not_found");
        }

        var lastAdmin = await _guard.EnsureNotLastSystemAdminAsync(id, null, deactivatingOrDeleting: true, cancellationToken);
        if (!lastAdmin.IsSuccess)
        {
            return lastAdmin;
        }

        user.MarkDeleted(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(id, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SetRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return Result.Fail("User not found.", "not_found");
        }

        var unique = roleIds.Distinct().ToList();
        var assignable = await _guard.EnsureCanAssignRolesAsync(unique, cancellationToken);
        if (!assignable.IsSuccess)
        {
            return assignable;
        }

        var lastAdmin = await _guard.EnsureNotLastSystemAdminAsync(userId, unique, deactivatingOrDeleting: false, cancellationToken);
        if (!lastAdmin.IsSuccess)
        {
            return lastAdmin;
        }

        var old = string.Join(",", user.UserRoles.Select(r => r.RoleId).OrderBy(x => x));
        _db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in unique)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedBy = _actor.Name
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(userId, cancellationToken);
        await _audit.WriteAsync(
            AuditEventType.UserRoleAssigned,
            nameof(RbacUser),
            userId,
            old,
            string.Join(",", unique.OrderBy(x => x)),
            cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SetDirectPermissionsAsync(
        Guid userId,
        IReadOnlyList<PermissionAssignmentDto> assignments,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return Result.Fail("User not found.", "not_found");
        }

        var parsed = ParseAssignments(assignments, out var error);
        if (error is not null)
        {
            return Result.Fail(error, "validation");
        }

        var grantable = await _guard.EnsureCanGrantPermissionsAsync(parsed, cancellationToken);
        if (!grantable.IsSuccess)
        {
            return grantable;
        }

        var ids = parsed.Select(a => a.PermissionId).Distinct().ToList();

        _db.UserPermissions.RemoveRange(user.UserPermissions);
        foreach (var assignment in parsed)
        {
            _db.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = assignment.PermissionId,
                Effect = assignment.Effect,
                ScopeId = assignment.ScopeId,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedBy = _actor.Name
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(userId, cancellationToken);
        await _audit.WriteAsync(AuditEventType.UserPermissionGranted, nameof(RbacUser), userId, null, string.Join(",", ids), cancellationToken);
        return Result.Ok();
    }

    internal static (int page, int pageSize) Normalize(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        return (page, pageSize);
    }

    internal static List<(Guid PermissionId, PermissionEffect Effect, Guid? ScopeId)> ParseAssignments(
        IReadOnlyList<PermissionAssignmentDto> assignments,
        out string? error)
    {
        error = null;
        var list = new List<(Guid, PermissionEffect, Guid?)>();
        foreach (var item in assignments)
        {
            if (!Enum.TryParse<PermissionEffect>(item.Effect, true, out var effect))
            {
                error = $"Unknown permission effect '{item.Effect}'.";
                return [];
            }

            list.Add((item.PermissionId, effect, item.ScopeId));
        }

        return list;
    }
}

public sealed class RoleAdminService : IRoleAdminService
{
    private readonly IRbacDbContext _db;
    private readonly IPermissionCache _cache;
    private readonly IAuditWriter _audit;
    private readonly IRbacActor _actor;
    private readonly AssignmentGuard _guard;

    public RoleAdminService(
        IRbacDbContext db,
        IPermissionCache cache,
        IAuditWriter audit,
        IRbacActor actor,
        AssignmentGuard guard)
    {
        _db = db;
        _cache = cache;
        _audit = audit;
        _actor = actor;
        _guard = guard;
    }

    public async Task<PagedResult<RoleDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = UserAdminService.Normalize(page, pageSize);
        var query = _db.Roles.AsNoTracking().Where(r => !r.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.Code.Contains(term) || r.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleDto>
        {
            Items = items.Select(r => r.ToDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<RoleDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        return role is null ? Result.Fail<RoleDto>("Role not found.", "not_found") : Result.Ok(role.ToDto());
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Fail<RoleDto>("Code and Name are required.", "validation");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Roles.AnyAsync(r => !r.IsDeleted && r.Code == code, cancellationToken))
        {
            return Result.Fail<RoleDto>("A role with that code already exists.", "conflict");
        }

        var role = new RbacRole
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedBy = _actor.Name
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.RoleCreated, nameof(RbacRole), role.Id, null, role.Code, cancellationToken);
        return Result.Ok(role.ToDto());
    }

    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
        {
            return Result.Fail<RoleDto>("Role not found.", "not_found");
        }

        if (role.IsSystemRole && !request.IsActive)
        {
            return Result.Fail<RoleDto>(PrivilegeRules.SystemRoleActiveLocked, "forbidden");
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        role.IsActive = request.IsActive;
        role.Touch(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateRoleUsersAsync(id, cancellationToken);
        await _audit.WriteAsync(AuditEventType.RoleUpdated, nameof(RbacRole), id, null, role.Name, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
        {
            return Result.Fail("Role not found.", "not_found");
        }

        if (role.IsSystemRole)
        {
            return Result.Fail("System roles cannot be deleted.", "forbidden");
        }

        role.MarkDeleted(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateRoleUsersAsync(id, cancellationToken);
        await _audit.WriteAsync(AuditEventType.RoleDeleted, nameof(RbacRole), id, role.Code, null, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SetPermissionsAsync(
        Guid roleId,
        IReadOnlyList<PermissionAssignmentDto> assignments,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
        if (role is null)
        {
            return Result.Fail("Role not found.", "not_found");
        }

        if (role.IsSystemRole)
        {
            return Result.Fail(PrivilegeRules.SystemRoleImmutable, "forbidden");
        }

        var parsed = UserAdminService.ParseAssignments(assignments, out var error);
        if (error is not null)
        {
            return Result.Fail(error, "validation");
        }

        var grantable = await _guard.EnsureCanGrantPermissionsAsync(parsed, cancellationToken);
        if (!grantable.IsSuccess)
        {
            return grantable;
        }

        var ids = parsed.Select(a => a.PermissionId).Distinct().ToList();

        _db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var assignment in parsed)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = assignment.PermissionId,
                Effect = assignment.Effect,
                ScopeId = assignment.ScopeId,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedBy = _actor.Name
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateRoleUsersAsync(roleId, cancellationToken);
        await _audit.WriteAsync(AuditEventType.RolePermissionGranted, nameof(RbacRole), roleId, null, string.Join(",", ids), cancellationToken);
        return Result.Ok();
    }

    private async Task InvalidateRoleUsersAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var userIds = await _db.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        await _cache.RemoveUsersAsync(userIds, cancellationToken);
    }
}

public sealed class PermissionAdminService : IPermissionAdminService
{
    private readonly IRbacDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IRbacActor _actor;
    private readonly IPermissionCache _cache;

    public PermissionAdminService(IRbacDbContext db, IAuditWriter audit, IRbacActor actor, IPermissionCache cache)
    {
        _db = db;
        _audit = audit;
        _actor = actor;
        _cache = cache;
    }

    public async Task<PagedResult<PermissionDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = UserAdminService.Normalize(page, pageSize);
        var query = _db.Permissions.AsNoTracking().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Code.Contains(term) ||
                p.Name.Contains(term) ||
                p.Resource.Contains(term) ||
                p.Action.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Resource).ThenBy(p => p.Action)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PermissionDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<PermissionDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        return permission is null
            ? Result.Fail<PermissionDto>("Permission not found.", "not_found")
            : Result.Ok(permission.ToDto());
    }

    public async Task<Result<PermissionDto>> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        PermissionCode code;
        try
        {
            code = PermissionCode.Create(request.Resource, request.Action);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail<PermissionDto>(ex.Message, "validation");
        }

        if (await _db.Permissions.AnyAsync(p => !p.IsDeleted && p.Code == code.Value, cancellationToken))
        {
            return Result.Fail<PermissionDto>("A permission with that code already exists.", "conflict");
        }

        var permission = new RbacPermission
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? code.Value : request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedBy = _actor.Name
        };
        permission.SetCode(code);
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.PermissionCreated, nameof(RbacPermission), permission.Id, null, permission.Code, cancellationToken);
        return Result.Ok(permission.ToDto());
    }

    public async Task<Result<PermissionDto>> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (permission is null)
        {
            return Result.Fail<PermissionDto>("Permission not found.", "not_found");
        }

        if (permission.IsSystemPermission && !request.IsActive)
        {
            return Result.Fail<PermissionDto>(PrivilegeRules.SystemPermissionLocked, "forbidden");
        }

        permission.Name = request.Name.Trim();
        permission.Description = request.Description?.Trim();
        permission.IsActive = request.IsActive;
        permission.Touch(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.InvalidateAllAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.PermissionUpdated, nameof(RbacPermission), id, null, permission.Name, cancellationToken);
        return Result.Ok(permission.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (permission is null)
        {
            return Result.Fail("Permission not found.", "not_found");
        }

        if (permission.IsSystemPermission)
        {
            return Result.Fail("System permissions cannot be deleted.", "forbidden");
        }

        permission.MarkDeleted(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.InvalidateAllAsync(cancellationToken);
        return Result.Ok();
    }
}
