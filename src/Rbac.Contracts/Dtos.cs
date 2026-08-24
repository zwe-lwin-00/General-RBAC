namespace Rbac.Contracts;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class CreateUserRequest
{
    public string ExternalId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class UpdateUserRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystemRole { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public sealed class CreateRoleRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class UpdateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class PermissionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool IsSystemPermission { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreatePermissionRequest
{
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class UpdatePermissionRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class ProgramDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public string? Version { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public sealed class CreateProgramRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public string? Version { get; init; }
}

public sealed class UpdateProgramRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public string? Version { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class MenuDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ProgramId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public string MenuType { get; init; } = "Item";
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<MenuDto> Children { get; init; } = [];
}

public sealed class CreateMenuRequest
{
    public Guid? ParentId { get; init; }
    public Guid? ProgramId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public string MenuType { get; init; } = "Item";
    public int SortOrder { get; init; }
}

public sealed class UpdateMenuRequest
{
    public Guid? ParentId { get; init; }
    public Guid? ProgramId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public string MenuType { get; init; } = "Item";
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; } = true;
    public bool IsActive { get; init; } = true;
}

public sealed class AssignIdsRequest
{
    public IReadOnlyList<Guid> Ids { get; init; } = [];
}

public sealed class AssignPermissionEffectsRequest
{
    public IReadOnlyList<PermissionAssignmentDto> Assignments { get; init; } = [];
}

public sealed class PermissionAssignmentDto
{
    public Guid PermissionId { get; init; }
    public string Effect { get; init; } = "Allow";
    public Guid? ScopeId { get; init; }
}

public sealed class MeDto
{
    public UserDto User { get; init; } = null!;
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public IReadOnlyList<MenuDto> Menus { get; init; } = [];
}
