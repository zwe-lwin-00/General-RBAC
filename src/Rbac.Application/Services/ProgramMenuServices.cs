using Microsoft.EntityFrameworkCore;
using Rbac.Application.Abstractions;
using Rbac.Application.Authorization;
using Rbac.Contracts;
using Rbac.Domain.Entities;
using Rbac.Domain.Enums;

namespace Rbac.Application.Services;

public sealed class ProgramAdminService : IProgramAdminService
{
    private readonly IRbacDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IRbacActor _actor;

    public ProgramAdminService(IRbacDbContext db, IAuditWriter audit, IRbacActor actor)
    {
        _db = db;
        _audit = audit;
        _actor = actor;
    }

    public async Task<PagedResult<ProgramDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = UserAdminService.Normalize(page, pageSize);
        var query = _db.Programs.AsNoTracking().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Code.Contains(term) || p.Name.Contains(term) || (p.Module != null && p.Module.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(p => p.ProgramPermissions).ThenInclude(pp => pp.Permission)
            .OrderBy(p => p.Module).ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProgramDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<ProgramDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await _db.Programs
            .AsNoTracking()
            .Include(p => p.ProgramPermissions).ThenInclude(pp => pp.Permission)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        return program is null ? Result.Fail<ProgramDto>("Program not found.", "not_found") : Result.Ok(program.ToDto());
    }

    public async Task<Result<ProgramDto>> CreateAsync(CreateProgramRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Fail<ProgramDto>("Code and Name are required.", "validation");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Programs.AnyAsync(p => !p.IsDeleted && p.Code == code, cancellationToken))
        {
            return Result.Fail<ProgramDto>("A program with that code already exists.", "conflict");
        }

        var program = new RbacProgram
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Module = request.Module?.Trim(),
            Version = request.Version?.Trim(),
            CreatedBy = _actor.Name
        };
        _db.Programs.Add(program);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.ProgramCreated, nameof(RbacProgram), program.Id, null, program.Code, cancellationToken);
        return Result.Ok(program.ToDto());
    }

    public async Task<Result<ProgramDto>> UpdateAsync(Guid id, UpdateProgramRequest request, CancellationToken cancellationToken = default)
    {
        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (program is null)
        {
            return Result.Fail<ProgramDto>("Program not found.", "not_found");
        }

        program.Name = request.Name.Trim();
        program.Description = request.Description?.Trim();
        program.Module = request.Module?.Trim();
        program.Version = request.Version?.Trim();
        program.IsActive = request.IsActive;
        program.Touch(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.ProgramUpdated, nameof(RbacProgram), id, null, program.Name, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (program is null)
        {
            return Result.Fail("Program not found.", "not_found");
        }

        program.MarkDeleted(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SetPermissionsAsync(Guid programId, IReadOnlyList<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var program = await _db.Programs
            .Include(p => p.ProgramPermissions)
            .FirstOrDefaultAsync(p => p.Id == programId && !p.IsDeleted, cancellationToken);
        if (program is null)
        {
            return Result.Fail("Program not found.", "not_found");
        }

        var unique = permissionIds.Distinct().ToList();
        var validCount = await _db.Permissions.CountAsync(p => unique.Contains(p.Id) && !p.IsDeleted, cancellationToken);
        if (validCount != unique.Count)
        {
            return Result.Fail("One or more permissions were not found.", "not_found");
        }

        _db.ProgramPermissions.RemoveRange(program.ProgramPermissions);
        foreach (var permissionId in unique)
        {
            _db.ProgramPermissions.Add(new ProgramPermission { ProgramId = programId, PermissionId = permissionId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

public sealed class MenuAdminService : IMenuAdminService
{
    private readonly IRbacDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IRbacActor _actor;

    public MenuAdminService(IRbacDbContext db, IAuditWriter audit, IRbacActor actor)
    {
        _db = db;
        _audit = audit;
        _actor = actor;
    }

    public async Task<IReadOnlyList<MenuDto>> ListTreeAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _db.Menus.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
        return BuildTree(menus);
    }

    public async Task<Result<MenuDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var menu = await _db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
        return menu is null ? Result.Fail<MenuDto>("Menu not found.", "not_found") : Result.Ok(menu.ToDto());
    }

    public async Task<Result<MenuDto>> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Fail<MenuDto>("Code and Name are required.", "validation");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Menus.AnyAsync(m => !m.IsDeleted && m.Code == code, cancellationToken))
        {
            return Result.Fail<MenuDto>("A menu with that code already exists.", "conflict");
        }

        if (!Enum.TryParse<MenuType>(request.MenuType, true, out var menuType))
        {
            return Result.Fail<MenuDto>("Invalid menu type.", "validation");
        }

        var menu = new RbacMenu
        {
            ParentId = request.ParentId,
            ProgramId = request.ProgramId,
            Code = code,
            Name = request.Name.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim(),
            Route = request.Route?.Trim(),
            Icon = request.Icon?.Trim(),
            MenuType = menuType,
            SortOrder = request.SortOrder,
            CreatedBy = _actor.Name
        };
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.MenuCreated, nameof(RbacMenu), menu.Id, null, menu.Code, cancellationToken);
        return Result.Ok(menu.ToDto());
    }

    public async Task<Result<MenuDto>> UpdateAsync(Guid id, UpdateMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
        if (menu is null)
        {
            return Result.Fail<MenuDto>("Menu not found.", "not_found");
        }

        if (!Enum.TryParse<MenuType>(request.MenuType, true, out var menuType))
        {
            return Result.Fail<MenuDto>("Invalid menu type.", "validation");
        }

        if (request.ParentId == id)
        {
            return Result.Fail<MenuDto>("A menu cannot be its own parent.", "validation");
        }

        menu.ParentId = request.ParentId;
        menu.ProgramId = request.ProgramId;
        menu.Name = request.Name.Trim();
        menu.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim();
        menu.Route = request.Route?.Trim();
        menu.Icon = request.Icon?.Trim();
        menu.MenuType = menuType;
        menu.SortOrder = request.SortOrder;
        menu.IsVisible = request.IsVisible;
        menu.IsActive = request.IsActive;
        menu.Touch(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(AuditEventType.MenuUpdated, nameof(RbacMenu), id, null, menu.Name, cancellationToken);
        return Result.Ok(menu.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
        if (menu is null)
        {
            return Result.Fail("Menu not found.", "not_found");
        }

        menu.MarkDeleted(_actor.Name);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    internal static IReadOnlyList<MenuDto> BuildTree(IReadOnlyList<RbacMenu> menus)
    {
        var byParent = menus
            .GroupBy(m => m.ParentId)
            .ToDictionary(g => g.Key ?? Guid.Empty, g => g.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToList());

        return Build(null);

        IReadOnlyList<MenuDto> Build(Guid? parentId)
        {
            var key = parentId ?? Guid.Empty;
            if (!byParent.TryGetValue(key, out var children))
            {
                return [];
            }

            return children.Select(child => child.ToDto(Build(child.Id))).ToList();
        }
    }
}

public sealed class CurrentUserQuery : ICurrentUserQuery
{
    private readonly IRbacDbContext _db;
    private readonly IRbacAuthorizationService _authorization;

    public CurrentUserQuery(IRbacDbContext db, IRbacAuthorizationService authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task<Result<MeDto>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return Result.Fail<MeDto>("User not found.", "not_found");
        }

        if (!user.IsActive)
        {
            return Result.Fail<MeDto>("User is inactive.", "forbidden");
        }

        var permissions = await _authorization.GetEffectivePermissionsAsync(userId, cancellationToken: cancellationToken);
        var menus = await GetVisibleMenusAsync(userId, cancellationToken);
        return Result.Ok(new MeDto
        {
            User = user.ToDto(),
            Permissions = permissions.OrderBy(x => x).ToList(),
            Menus = menus
        });
    }

    public async Task<IReadOnlyList<MenuDto>> GetVisibleMenusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var permissions = await _authorization.GetEffectivePermissionsAsync(userId, cancellationToken: cancellationToken);
        var menus = await _db.Menus
            .AsNoTracking()
            .Include(m => m.Program)
                .ThenInclude(p => p!.ProgramPermissions)
                .ThenInclude(pp => pp.Permission)
            .Where(m => !m.IsDeleted)
            .ToListAsync(cancellationToken);
        return MenuVisibility.FilterTree(menus, permissions);
    }
}

public sealed class RbacUserResolver : IRbacUserResolver
{
    private readonly IRbacDbContext _db;

    public RbacUserResolver(IRbacDbContext db) => _db = db;

    public Task<RbacUser?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ExternalId == externalId && !u.IsDeleted, cancellationToken);

    public Task<RbacUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
}
