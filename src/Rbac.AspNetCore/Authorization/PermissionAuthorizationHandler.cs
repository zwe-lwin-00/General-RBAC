using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rbac.Application;
using Rbac.Application.Abstractions;

namespace Rbac.AspNetCore.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRbacUserResolver _users;
    private readonly IRbacAuthorizationService _authorization;
    private readonly RbacOptions _options;

    public PermissionAuthorizationHandler(
        IRbacUserResolver users,
        IRbacAuthorizationService authorization,
        IOptions<RbacOptions> options)
    {
        _users = users;
        _authorization = authorization;
        _options = options.Value;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requirement.Permission))
            {
                return;
            }

            var externalId = context.User.FindExternalId(_options);
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return;
            }

            var user = await _users.FindByExternalIdAsync(externalId);
            if (user is null || !user.IsActive)
            {
                return;
            }

            var decision = await _authorization.AuthorizeAsync(user.Id, requirement.Permission.Trim());
            if (decision.IsAllowed)
            {
                context.Succeed(requirement);
            }
        }
        catch
        {
            // Fail closed. Do not succeed the requirement.
        }
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string? FindExternalId(this ClaimsPrincipal user, RbacOptions options)
    {
        var value = user.FindFirst(options.ExternalIdClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        foreach (var claimType in options.AdditionalExternalIdClaimTypes)
        {
            value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return user.Identity?.Name;
    }
}

public sealed class HttpRbacActor : IRbacActor
{
    private readonly IHttpContextAccessor _http;
    private readonly RbacOptions _options;

    public HttpRbacActor(IHttpContextAccessor http, IOptions<RbacOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public string? Name =>
        _http.HttpContext?.User.Identity?.Name
        ?? _http.HttpContext?.User.FindExternalId(_options)
        ?? "unmapped";

    public Guid? UserId =>
        _http.HttpContext?.Items.TryGetValue(Rbac.AspNetCore.RbacUserMiddleware.ItemKey, out var value) == true && value is Guid id
            ? id
            : null;

    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId =>
        _http.HttpContext?.TraceIdentifier
        ?? _http.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();

    public bool IsSystemProcess => false;
}

public static class EndpointConventionBuilderExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(PermissionPolicyProvider.PolicyName(permission));
    }
}
