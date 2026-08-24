using Rbac.Contracts;
using Rbac.Domain.Entities;

namespace Rbac.Application.Abstractions;

public interface IUserAdminService
{
    Task<PagedResult<UserDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> SetRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<Result> SetDirectPermissionsAsync(Guid userId, IReadOnlyList<PermissionAssignmentDto> assignments, CancellationToken cancellationToken = default);
}

public interface IRoleAdminService
{
    Task<PagedResult<RoleDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> SetPermissionsAsync(Guid roleId, IReadOnlyList<PermissionAssignmentDto> assignments, CancellationToken cancellationToken = default);
}

public interface IPermissionAdminService
{
    Task<PagedResult<PermissionDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProgramAdminService
{
    Task<PagedResult<ProgramDto>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<ProgramDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ProgramDto>> CreateAsync(CreateProgramRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProgramDto>> UpdateAsync(Guid id, UpdateProgramRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> SetPermissionsAsync(Guid programId, IReadOnlyList<Guid> permissionIds, CancellationToken cancellationToken = default);
}

public interface IMenuAdminService
{
    Task<IReadOnlyList<MenuDto>> ListTreeAsync(CancellationToken cancellationToken = default);
    Task<Result<MenuDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<MenuDto>> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default);
    Task<Result<MenuDto>> UpdateAsync(Guid id, UpdateMenuRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICurrentUserQuery
{
    Task<Result<MeDto>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuDto>> GetVisibleMenusAsync(Guid userId, CancellationToken cancellationToken = default);
}

internal static class Mapping
{
    public static UserDto ToDto(this RbacUser user) => new()
    {
        Id = user.Id,
        ExternalId = user.ExternalId,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Email = user.Email,
        IsActive = user.IsActive,
        Roles = user.UserRoles
            .Where(ur => ur.Role is { IsDeleted: false })
            .Select(ur => ur.Role.Code)
            .OrderBy(x => x)
            .ToList()
    };

    public static RoleDto ToDto(this RbacRole role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        IsSystemRole = role.IsSystemRole,
        IsActive = role.IsActive,
        Permissions = role.RolePermissions
            .Where(rp => rp.Permission is { IsDeleted: false })
            .Select(rp => rp.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList()
    };

    public static PermissionDto ToDto(this RbacPermission permission) => new()
    {
        Id = permission.Id,
        Code = permission.Code,
        Name = permission.Name,
        Description = permission.Description,
        Resource = permission.Resource,
        Action = permission.Action,
        IsSystemPermission = permission.IsSystemPermission,
        IsActive = permission.IsActive
    };

    public static ProgramDto ToDto(this RbacProgram program) => new()
    {
        Id = program.Id,
        Code = program.Code,
        Name = program.Name,
        Description = program.Description,
        Module = program.Module,
        Version = program.Version,
        IsActive = program.IsActive,
        Permissions = program.ProgramPermissions
            .Where(pp => pp.Permission is { IsDeleted: false })
            .Select(pp => pp.Permission.Code)
            .OrderBy(x => x)
            .ToList()
    };

    public static MenuDto ToDto(this RbacMenu menu, IReadOnlyList<MenuDto>? children = null) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        ProgramId = menu.ProgramId,
        Code = menu.Code,
        Name = menu.Name,
        DisplayName = string.IsNullOrWhiteSpace(menu.DisplayName) ? menu.Name : menu.DisplayName,
        Route = menu.Route,
        Icon = menu.Icon,
        MenuType = menu.MenuType.ToString(),
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive,
        Children = children ?? []
    };
}
