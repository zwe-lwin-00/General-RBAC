using Rbac.Domain.Enums;

namespace Rbac.Domain.Authorization;

public sealed record PermissionGrant(
    string PermissionCode,
    PermissionEffect Effect,
    Guid? ScopeId = null);

public sealed record AuthorizationDecision(
    bool IsAllowed,
    string PermissionCode,
    string Reason)
{
    public static AuthorizationDecision Allow(string permission, string reason) =>
        new(true, permission, reason);

    public static AuthorizationDecision Deny(string permission, string reason) =>
        new(false, permission, reason);
}

/// <summary>
/// Pure authorization rules. Persistence, caching, and HTTP stay outside this type.
/// Precedence: user deny → user allow → role deny → role allow → default deny.
/// </summary>
public static class AuthorizationEvaluator
{
    public static AuthorizationDecision Evaluate(
        bool isUserActive,
        IReadOnlyCollection<PermissionGrant> userGrants,
        IReadOnlyCollection<PermissionGrant> roleGrants,
        string requiredPermission,
        Guid? requiredScopeId = null)
    {
        if (!isUserActive)
        {
            return AuthorizationDecision.Deny(requiredPermission, "User is inactive.");
        }

        if (!ValueObjects.PermissionCode.TryParse(requiredPermission, out var parsed))
        {
            return AuthorizationDecision.Deny(requiredPermission, "Permission code is invalid.");
        }

        var code = parsed.Value;
        var userMatches = userGrants.Where(g => Matches(g, code, requiredScopeId)).ToList();
        if (userMatches.Exists(g => g.Effect == PermissionEffect.Deny))
        {
            return AuthorizationDecision.Deny(code, "Explicit user deny.");
        }

        if (userMatches.Exists(g => g.Effect == PermissionEffect.Allow))
        {
            return AuthorizationDecision.Allow(code, "Explicit user allow.");
        }

        var roleMatches = roleGrants.Where(g => Matches(g, code, requiredScopeId)).ToList();
        if (roleMatches.Exists(g => g.Effect == PermissionEffect.Deny))
        {
            return AuthorizationDecision.Deny(code, "Role deny.");
        }

        if (roleMatches.Exists(g => g.Effect == PermissionEffect.Allow))
        {
            return AuthorizationDecision.Allow(code, "Role allow.");
        }

        return AuthorizationDecision.Deny(code, "Default deny.");
    }

    public static IReadOnlySet<string> ComputeEffectiveAllows(
        IReadOnlyCollection<PermissionGrant> userGrants,
        IReadOnlyCollection<PermissionGrant> roleGrants)
    {
        var allows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var grant in roleGrants.Where(g => g.Effect == PermissionEffect.Allow))
        {
            allows.Add(grant.PermissionCode);
        }

        foreach (var grant in roleGrants.Where(g => g.Effect == PermissionEffect.Deny))
        {
            allows.Remove(grant.PermissionCode);
        }

        foreach (var grant in userGrants.Where(g => g.Effect == PermissionEffect.Allow))
        {
            allows.Add(grant.PermissionCode);
        }

        foreach (var grant in userGrants.Where(g => g.Effect == PermissionEffect.Deny))
        {
            allows.Remove(grant.PermissionCode);
        }

        return allows;
    }

    /// <summary>
    /// A request with no scope is satisfied by any grant of that permission.
    /// A scoped request is satisfied by a global (ALL) grant or an exact scope match.
    /// </summary>
    private static bool Matches(PermissionGrant grant, string requiredCode, Guid? requiredScopeId)
    {
        if (!string.Equals(grant.PermissionCode, requiredCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (requiredScopeId is null)
        {
            return true;
        }

        return grant.ScopeId is null || grant.ScopeId == requiredScopeId;
    }
}
