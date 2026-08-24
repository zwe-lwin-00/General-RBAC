using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.AspNetCore.Authorization;

namespace Rbac.AspNetCore;

/// <summary>
/// Resolves the authenticated external id to an RBAC user once per request.
/// </summary>
public sealed class RbacUserMiddleware
{
    public const string ItemKey = "Rbac.UserId";
    private readonly RequestDelegate _next;

    public RbacUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IRbacUserResolver resolver,
        IOptions<RbacOptions> options)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var externalId = context.User.FindExternalId(options.Value);
            if (!string.IsNullOrWhiteSpace(externalId))
            {
                var user = await resolver.FindByExternalIdAsync(externalId, context.RequestAborted);
                if (user is not null)
                {
                    context.Items[ItemKey] = user.Id;
                }
            }
        }

        await _next(context);
    }
}
