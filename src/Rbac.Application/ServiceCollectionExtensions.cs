using Microsoft.Extensions.DependencyInjection;
using Rbac.Application.Abstractions;
using Rbac.Application.Authorization;
using Rbac.Application.Services;

namespace Rbac.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRbacApplication(this IServiceCollection services)
    {
        services.AddScoped<IRbacAuthorizationService, RbacAuthorizationService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IPermissionAdminService, PermissionAdminService>();
        services.AddScoped<IProgramAdminService, ProgramAdminService>();
        services.AddScoped<IMenuAdminService, MenuAdminService>();
        services.AddScoped<ICurrentUserQuery, CurrentUserQuery>();
        services.AddScoped<IRbacUserResolver, RbacUserResolver>();
        return services;
    }
}
