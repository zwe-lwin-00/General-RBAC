using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.AspNetCore.Authorization;
using Rbac.Contracts;
using Rbac.Domain;

namespace Rbac.AspNetCore.Endpoints;

public static class RbacApiEndpointExtensions
{
    public static IEndpointRouteBuilder MapRbacApi(this IEndpointRouteBuilder app, string prefix = "/api/rbac")
    {
        var enableAdmin = app.ServiceProvider.GetService<IOptions<RbacOptions>>()?.Value.EnableAdminApi ?? true;
        var api = app.MapGroup(prefix).WithTags("RBAC");
        MapMe(api);
        if (enableAdmin)
        {
            MapUsers(api);
            MapRoles(api);
            MapPermissions(api);
            MapPrograms(api);
            MapMenus(api);
        }

        return app;
    }

    private static void MapMe(RouteGroupBuilder api)
    {
        api.MapGet("/me", async (HttpContext http, ICurrentUserQuery query, CancellationToken ct) =>
        {
            var userId = http.GetRbacUserId();
            if (userId is null) return Results.Unauthorized();
            return ToHttp(await query.GetMeAsync(userId.Value, ct));
        }).RequireAuthorization();

        api.MapGet("/me/permissions", async (HttpContext http, ICurrentUserQuery query, CancellationToken ct) =>
        {
            var userId = http.GetRbacUserId();
            if (userId is null) return Results.Unauthorized();
            var result = await query.GetMeAsync(userId.Value, ct);
            return result.IsSuccess ? Results.Ok(result.Value!.Permissions) : ToHttp(result);
        }).RequireAuthorization();

        api.MapGet("/me/menus", async (HttpContext http, ICurrentUserQuery query, CancellationToken ct) =>
        {
            var userId = http.GetRbacUserId();
            if (userId is null) return Results.Unauthorized();
            return Results.Ok(await query.GetVisibleMenusAsync(userId.Value, ct));
        }).RequireAuthorization();
    }

    private static void MapUsers(RouteGroupBuilder api)
    {
        var users = api.MapGroup("/users").RequireAuthorization();
        users.MapGet("", async (string? search, int? page, int? pageSize, IUserAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(search, page ?? 1, pageSize ?? 50, ct)))
            .RequirePermission(RbacPermissions.UsersRead);

        users.MapGet("/{id:guid}", async (Guid id, IUserAdminService service, CancellationToken ct) =>
            ToHttp(await service.GetAsync(id, ct)))
            .RequirePermission(RbacPermissions.UsersRead);

        users.MapPost("", async (CreateUserRequest request, IUserAdminService service, CancellationToken ct) =>
            ToHttp(await service.CreateAsync(request, ct)))
            .RequirePermission(RbacPermissions.UsersCreate);

        users.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, IUserAdminService service, CancellationToken ct) =>
            ToHttp(await service.UpdateAsync(id, request, ct)))
            .RequirePermission(RbacPermissions.UsersUpdate);

        users.MapDelete("/{id:guid}", async (Guid id, IUserAdminService service, CancellationToken ct) =>
            ToHttp(await service.DeleteAsync(id, ct)))
            .RequirePermission(RbacPermissions.UsersDelete);

        users.MapPut("/{id:guid}/roles", async (Guid id, AssignIdsRequest request, IUserAdminService service, CancellationToken ct) =>
            ToHttp(await service.SetRolesAsync(id, request.Ids, ct)))
            .RequirePermission(RbacPermissions.AssignRoles);

        users.MapPut("/{id:guid}/permissions", async (Guid id, AssignPermissionEffectsRequest request, IUserAdminService service, CancellationToken ct) =>
            ToHttp(await service.SetDirectPermissionsAsync(id, request.Assignments, ct)))
            .RequirePermission(RbacPermissions.AssignPermissions);
    }

    private static void MapRoles(RouteGroupBuilder api)
    {
        var roles = api.MapGroup("/roles").RequireAuthorization();
        roles.MapGet("", async (string? search, int? page, int? pageSize, IRoleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(search, page ?? 1, pageSize ?? 50, ct)))
            .RequirePermission(RbacPermissions.RolesRead);

        roles.MapGet("/{id:guid}", async (Guid id, IRoleAdminService service, CancellationToken ct) =>
            ToHttp(await service.GetAsync(id, ct)))
            .RequirePermission(RbacPermissions.RolesRead);

        roles.MapPost("", async (CreateRoleRequest request, IRoleAdminService service, CancellationToken ct) =>
            ToHttp(await service.CreateAsync(request, ct)))
            .RequirePermission(RbacPermissions.RolesCreate);

        roles.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest request, IRoleAdminService service, CancellationToken ct) =>
            ToHttp(await service.UpdateAsync(id, request, ct)))
            .RequirePermission(RbacPermissions.RolesUpdate);

        roles.MapDelete("/{id:guid}", async (Guid id, IRoleAdminService service, CancellationToken ct) =>
            ToHttp(await service.DeleteAsync(id, ct)))
            .RequirePermission(RbacPermissions.RolesDelete);

        roles.MapPut("/{id:guid}/permissions", async (Guid id, AssignPermissionEffectsRequest request, IRoleAdminService service, CancellationToken ct) =>
            ToHttp(await service.SetPermissionsAsync(id, request.Assignments, ct)))
            .RequirePermission(RbacPermissions.RolesUpdate);
    }

    private static void MapPermissions(RouteGroupBuilder api)
    {
        var permissions = api.MapGroup("/permissions").RequireAuthorization();
        permissions.MapGet("", async (string? search, int? page, int? pageSize, IPermissionAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(search, page ?? 1, pageSize ?? 50, ct)))
            .RequirePermission(RbacPermissions.PermissionsRead);

        permissions.MapGet("/{id:guid}", async (Guid id, IPermissionAdminService service, CancellationToken ct) =>
            ToHttp(await service.GetAsync(id, ct)))
            .RequirePermission(RbacPermissions.PermissionsRead);

        permissions.MapPost("", async (CreatePermissionRequest request, IPermissionAdminService service, CancellationToken ct) =>
            ToHttp(await service.CreateAsync(request, ct)))
            .RequirePermission(RbacPermissions.PermissionsCreate);

        permissions.MapPut("/{id:guid}", async (Guid id, UpdatePermissionRequest request, IPermissionAdminService service, CancellationToken ct) =>
            ToHttp(await service.UpdateAsync(id, request, ct)))
            .RequirePermission(RbacPermissions.PermissionsUpdate);

        permissions.MapDelete("/{id:guid}", async (Guid id, IPermissionAdminService service, CancellationToken ct) =>
            ToHttp(await service.DeleteAsync(id, ct)))
            .RequirePermission(RbacPermissions.PermissionsDelete);
    }

    private static void MapPrograms(RouteGroupBuilder api)
    {
        var programs = api.MapGroup("/programs").RequireAuthorization();
        programs.MapGet("", async (string? search, int? page, int? pageSize, IProgramAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(search, page ?? 1, pageSize ?? 50, ct)))
            .RequirePermission(RbacPermissions.ProgramsRead);

        programs.MapGet("/{id:guid}", async (Guid id, IProgramAdminService service, CancellationToken ct) =>
            ToHttp(await service.GetAsync(id, ct)))
            .RequirePermission(RbacPermissions.ProgramsRead);

        programs.MapPost("", async (CreateProgramRequest request, IProgramAdminService service, CancellationToken ct) =>
            ToHttp(await service.CreateAsync(request, ct)))
            .RequirePermission(RbacPermissions.ProgramsCreate);

        programs.MapPut("/{id:guid}", async (Guid id, UpdateProgramRequest request, IProgramAdminService service, CancellationToken ct) =>
            ToHttp(await service.UpdateAsync(id, request, ct)))
            .RequirePermission(RbacPermissions.ProgramsUpdate);

        programs.MapDelete("/{id:guid}", async (Guid id, IProgramAdminService service, CancellationToken ct) =>
            ToHttp(await service.DeleteAsync(id, ct)))
            .RequirePermission(RbacPermissions.ProgramsDelete);

        programs.MapPut("/{id:guid}/permissions", async (Guid id, AssignIdsRequest request, IProgramAdminService service, CancellationToken ct) =>
            ToHttp(await service.SetPermissionsAsync(id, request.Ids, ct)))
            .RequirePermission(RbacPermissions.ProgramsUpdate);
    }

    private static void MapMenus(RouteGroupBuilder api)
    {
        var menus = api.MapGroup("/menus").RequireAuthorization();
        menus.MapGet("", async (IMenuAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListTreeAsync(ct)))
            .RequirePermission(RbacPermissions.MenusRead);

        menus.MapGet("/{id:guid}", async (Guid id, IMenuAdminService service, CancellationToken ct) =>
            ToHttp(await service.GetAsync(id, ct)))
            .RequirePermission(RbacPermissions.MenusRead);

        menus.MapPost("", async (CreateMenuRequest request, IMenuAdminService service, CancellationToken ct) =>
            ToHttp(await service.CreateAsync(request, ct)))
            .RequirePermission(RbacPermissions.MenusCreate);

        menus.MapPut("/{id:guid}", async (Guid id, UpdateMenuRequest request, IMenuAdminService service, CancellationToken ct) =>
            ToHttp(await service.UpdateAsync(id, request, ct)))
            .RequirePermission(RbacPermissions.MenusUpdate);

        menus.MapDelete("/{id:guid}", async (Guid id, IMenuAdminService service, CancellationToken ct) =>
            ToHttp(await service.DeleteAsync(id, ct)))
            .RequirePermission(RbacPermissions.MenusDelete);
    }

    private static IResult ToHttp(Result result) =>
        result.IsSuccess ? Results.NoContent() : Error(result.Error, result.ErrorCode);

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error, result.ErrorCode);

    private static IResult Error(string? error, string? code) =>
        Results.Json(new { error, code }, statusCode: code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            "forbidden" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        });
}

internal static class HttpContextRbacExtensions
{
    public static Guid? GetRbacUserId(this HttpContext http) =>
        http.Items.TryGetValue(RbacUserMiddleware.ItemKey, out var value) && value is Guid id ? id : null;
}
