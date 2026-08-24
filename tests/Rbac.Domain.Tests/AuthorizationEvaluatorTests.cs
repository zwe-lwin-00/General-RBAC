using Rbac.Domain.Authorization;
using Rbac.Domain.Enums;
using Rbac.Domain.ValueObjects;

namespace Rbac.Domain.Tests;

public class PermissionCodeTests
{
    [Fact]
    public void Create_derives_resource_dot_action()
    {
        var code = PermissionCode.Create("Passenger", "Read");
        Assert.Equal("passenger", code.Resource);
        Assert.Equal("read", code.Action);
        Assert.Equal("passenger.read", code.Value);
    }

    [Theory]
    [InlineData("passenger.read")]
    [InlineData(" rbac.assign.roles ")]
    public void Parse_accepts_valid_codes(string value)
    {
        Assert.True(PermissionCode.TryParse(value.Trim() == value ? value : value, out _));
    }

    [Fact]
    public void Parse_allows_namespaced_resource()
    {
        var code = PermissionCode.Parse("rbac.assign.roles");
        Assert.Equal("rbac.assign", code.Resource);
        Assert.Equal("roles", code.Action);
        Assert.Equal("rbac.assign.roles", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("passenger")]
    [InlineData(".read")]
    [InlineData("passenger.")]
    [InlineData("passenger read")]
    public void TryParse_rejects_invalid_codes(string value)
    {
        Assert.False(PermissionCode.TryParse(value, out _));
    }
}

public class AuthorizationEvaluatorTests
{
    [Fact]
    public void Default_deny_when_no_grants()
    {
        var decision = AuthorizationEvaluator.Evaluate(true, [], [], "passenger.read");
        Assert.False(decision.IsAllowed);
        Assert.Equal("Default deny.", decision.Reason);
    }

    [Fact]
    public void Role_allow_grants_access()
    {
        var decision = AuthorizationEvaluator.Evaluate(
            true,
            [],
            [new PermissionGrant("passenger.read", PermissionEffect.Allow)],
            "passenger.read");
        Assert.True(decision.IsAllowed);
        Assert.Equal("Role allow.", decision.Reason);
    }

    [Fact]
    public void User_deny_wins_over_role_allow()
    {
        var decision = AuthorizationEvaluator.Evaluate(
            true,
            [new PermissionGrant("report.export", PermissionEffect.Deny)],
            [new PermissionGrant("report.export", PermissionEffect.Allow)],
            "report.export");
        Assert.False(decision.IsAllowed);
        Assert.Equal("Explicit user deny.", decision.Reason);
    }

    [Fact]
    public void User_allow_wins_over_role_deny()
    {
        var decision = AuthorizationEvaluator.Evaluate(
            true,
            [new PermissionGrant("report.export", PermissionEffect.Allow)],
            [new PermissionGrant("report.export", PermissionEffect.Deny)],
            "report.export");
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Inactive_user_is_denied()
    {
        var decision = AuthorizationEvaluator.Evaluate(
            false,
            [],
            [new PermissionGrant("passenger.read", PermissionEffect.Allow)],
            "passenger.read");
        Assert.False(decision.IsAllowed);
        Assert.Equal("User is inactive.", decision.Reason);
    }

    [Fact]
    public void Scoped_request_is_satisfied_by_global_grant()
    {
        var yangon = Guid.NewGuid();
        var decision = AuthorizationEvaluator.Evaluate(
            true,
            [],
            [new PermissionGrant("passenger.read", PermissionEffect.Allow)],
            "passenger.read",
            yangon);
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Scoped_request_is_denied_for_a_different_scope()
    {
        var yangon = Guid.NewGuid();
        var mandalay = Guid.NewGuid();
        var decision = AuthorizationEvaluator.Evaluate(
            true,
            [],
            [new PermissionGrant("passenger.read", PermissionEffect.Allow, yangon)],
            "passenger.read",
            mandalay);
        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void Effective_allows_apply_deny_precedence()
    {
        var effective = AuthorizationEvaluator.ComputeEffectiveAllows(
            [new PermissionGrant("report.export", PermissionEffect.Deny)],
            [
                new PermissionGrant("passenger.read", PermissionEffect.Allow),
                new PermissionGrant("report.export", PermissionEffect.Allow)
            ]);

        Assert.Contains("passenger.read", effective);
        Assert.DoesNotContain("report.export", effective);
    }

    [Fact]
    public void Unscoped_request_is_not_satisfied_by_scoped_grant()
    {
        var yangon = Guid.NewGuid();
        var decision = AuthorizationEvaluator.Evaluate(
            true,
            [],
            [new PermissionGrant("passenger.read", PermissionEffect.Allow, yangon)],
            "passenger.read");
        Assert.False(decision.IsAllowed);
        Assert.Equal("Default deny.", decision.Reason);
    }

    [Fact]
    public void Effective_allows_ignore_scoped_grants_when_unscoped()
    {
        var yangon = Guid.NewGuid();
        var effective = AuthorizationEvaluator.ComputeEffectiveAllows(
            [],
            [new PermissionGrant("passenger.read", PermissionEffect.Allow, yangon)]);
        Assert.DoesNotContain("passenger.read", effective);
    }
}

public class PrivilegeRulesTests
{
    [Fact]
    public void Non_admin_cannot_assign_system_role()
    {
        var held = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "passenger.read" };
        Assert.False(PrivilegeRules.CanAssignRole(false, false, held, true, held));
    }

    [Fact]
    public void Non_admin_cannot_grant_unheld_permission()
    {
        var held = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "passenger.read" };
        Assert.False(PrivilegeRules.CanGrantPermission(false, false, held, "passenger.delete"));
        Assert.True(PrivilegeRules.CanGrantPermission(false, false, held, "passenger.read"));
    }

    [Fact]
    public void System_admin_can_grant_any_permission()
    {
        Assert.True(PrivilegeRules.CanGrantPermission(
            false,
            true,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            "passenger.delete"));
    }

    [Fact]
    public void Unscoped_match_requires_global_grant()
    {
        Assert.True(PrivilegeRules.ScopeMatches(null, null));
        Assert.False(PrivilegeRules.ScopeMatches(Guid.NewGuid(), null));
        Assert.True(PrivilegeRules.ScopeMatches(null, Guid.NewGuid()));
    }
}
