using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.AspNetCore.Authorization;
using Rbac.AspNetCore.Endpoints;
using Rbac.Infrastructure;

namespace Rbac.AspNetCore;

public static class RbacServiceCollectionExtensions
{
    /// <summary>
    /// Registers RBAC application services, persistence, and ASP.NET Core permission authorization.
    /// Authentication itself is owned by the host.
    /// </summary>
    public static IServiceCollection AddRbac(
        this IServiceCollection services,
        Action<RbacOptions>? configure = null,
        Action<RbacInfrastructureOptions>? infrastructure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<RbacOptions>();
        }

        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Scoped<IRbacActor, HttpRbacActor>());
        services.AddRbacApplication();
        services.AddRbacInfrastructure(infrastructure);
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }

    public static IApplicationBuilder UseRbac(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RbacUserMiddleware>();
    }

    public static IApplicationBuilder UseRbacApi(this IApplicationBuilder app, string prefix = "/api/rbac")
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = prefix;
        return app.UseRbac();
    }
}

public static class RbacEndpointExtensions
{
    public static WebApplication MapRbac(this WebApplication app, string prefix = "/api/rbac")
    {
        app.MapRbacApi(prefix);
        return app;
    }
}
