using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.Contracts;
using Rbac.Domain.Entities;
using Rbac.Domain.Enums;
using Rbac.Infrastructure.Caching;
using Rbac.Infrastructure.Persistence;
using Rbac.Infrastructure.Seed;

namespace Rbac.Application.Tests;

public class AuthorizationServiceTests
{
    [Fact]
    public async Task Officer_can_read_but_cannot_export()
    {
        await using var harness = await Harness.CreateAsync();
        var officerId = SeedIds.For("user:officer");

        Assert.True(await harness.Auth.HasPermissionAsync(officerId, "passenger.read"));
        Assert.True(await harness.Auth.HasPermissionAsync(officerId, "passenger.create"));
        Assert.False(await harness.Auth.HasPermissionAsync(officerId, "passenger.export"));
        Assert.False(await harness.Auth.HasPermissionAsync(officerId, "passenger.delete"));
    }

    [Fact]
    public async Task John_is_denied_report_export_despite_supervisor_role()
    {
        await using var harness = await Harness.CreateAsync();
        var johnId = SeedIds.For("user:john");
        var supervisorId = SeedIds.For("user:supervisor");

        Assert.True(await harness.Auth.HasPermissionAsync(supervisorId, "report.export"));
        Assert.False(await harness.Auth.HasPermissionAsync(johnId, "report.export"));
        Assert.True(await harness.Auth.HasPermissionAsync(johnId, "passenger.delete"));
    }

    [Fact]
    public async Task Direct_user_allow_adds_permission_beyond_role()
    {
        await using var harness = await Harness.CreateAsync();
        var officerId = SeedIds.For("user:officer");
        var export = await harness.Db.Permissions.SingleAsync(p => p.Code == "passenger.export");

        var result = await harness.Users.SetDirectPermissionsAsync(officerId, [
            new PermissionAssignmentDto { PermissionId = export.Id, Effect = "Allow" }
        ]);

        Assert.True(result.IsSuccess);
        Assert.True(await harness.Auth.HasPermissionAsync(officerId, "passenger.export"));
    }

    [Fact]
    public async Task Visible_menus_hide_programs_the_user_cannot_use()
    {
        await using var harness = await Harness.CreateAsync();
        var viewerId = SeedIds.For("user:viewer");
        var menus = await harness.Me.GetVisibleMenusAsync(viewerId);
        var routes = Flatten(menus).Select(m => m.Route).Where(r => r is not null).ToList();

        Assert.Contains("/", routes);
        Assert.Contains("/passengers", routes);
        Assert.DoesNotContain("/admin/users", routes);
    }

    [Fact]
    public async Task Super_admin_sees_administration_menus()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = SeedIds.For("user:superadmin");
        var menus = await harness.Me.GetVisibleMenusAsync(superId);
        var routes = Flatten(menus).Select(m => m.Route).ToList();
        Assert.Contains("/admin/roles", routes);
    }

    [Fact]
    public async Task Officer_cannot_self_escalate_to_super_admin()
    {
        await using var harness = await Harness.CreateAsync(new FixedActor(SeedIds.For("user:officer")));
        var officerId = SeedIds.For("user:officer");
        var superRole = await harness.Db.Roles.SingleAsync(r => r.Code == "SUPER_ADMIN");
        var officerRole = await harness.Db.Roles.SingleAsync(r => r.Code == "OFFICER");

        var result = await harness.Users.SetRolesAsync(officerId, [officerRole.Id, superRole.Id]);
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.ErrorCode);
        Assert.False(await harness.Auth.HasPermissionAsync(officerId, "rbac.users.delete"));
    }

    [Fact]
    public async Task Officer_cannot_grant_an_unheld_direct_permission()
    {
        await using var harness = await Harness.CreateAsync(new FixedActor(SeedIds.For("user:officer")));
        var officerId = SeedIds.For("user:officer");
        var export = await harness.Db.Permissions.SingleAsync(p => p.Code == "passenger.export");

        var result = await harness.Users.SetDirectPermissionsAsync(officerId, [
            new PermissionAssignmentDto { PermissionId = export.Id, Effect = "Allow" }
        ]);

        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.ErrorCode);
        Assert.False(await harness.Auth.HasPermissionAsync(officerId, "passenger.export"));
    }

    [Fact]
    public async Task System_role_permissions_cannot_be_rewritten()
    {
        await using var harness = await Harness.CreateAsync();
        var superRole = await harness.Db.Roles.SingleAsync(r => r.Code == "SUPER_ADMIN");
        var result = await harness.Roles.SetPermissionsAsync(superRole.Id, []);
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task Last_system_admin_cannot_be_deactivated()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = SeedIds.For("user:superadmin");
        var result = await harness.Users.UpdateAsync(superId, new UpdateUserRequest
        {
            DisplayName = "Super Admin",
            Email = "superadmin@example.com",
            IsActive = false
        });
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.ErrorCode);
        Assert.True(await harness.Auth.HasPermissionAsync(superId, "rbac.users.read"));
    }

    [Fact]
    public async Task Scoped_grant_does_not_authorize_unscoped_check()
    {
        await using var harness = await Harness.CreateAsync();
        var officerId = SeedIds.For("user:officer");
        var permission = await harness.Db.Permissions.SingleAsync(p => p.Code == "passenger.export");
        harness.Db.UserPermissions.Add(new UserPermission
        {
            UserId = officerId,
            PermissionId = permission.Id,
            ScopeId = Guid.NewGuid(),
            Effect = PermissionEffect.Allow,
            AssignedBy = "test"
        });
        await harness.Db.SaveChangesAsync();

        Assert.False(await harness.Auth.HasPermissionAsync(officerId, "passenger.export"));
        Assert.False((await harness.Auth.GetEffectivePermissionsAsync(officerId)).Contains("passenger.export"));
    }

    private static IEnumerable<MenuDto> Flatten(IEnumerable<MenuDto> menus)
    {
        foreach (var menu in menus)
        {
            yield return menu;
            foreach (var child in Flatten(menu.Children))
            {
                yield return child;
            }
        }
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        public RbacDbContext Db { get; }
        public IRbacAuthorizationService Auth { get; }
        public IUserAdminService Users { get; }
        public IRoleAdminService Roles { get; }
        public ICurrentUserQuery Me { get; }

        private Harness(ServiceProvider provider, IServiceScope scope)
        {
            _provider = provider;
            _scope = scope;
            Db = scope.ServiceProvider.GetRequiredService<RbacDbContext>();
            Auth = scope.ServiceProvider.GetRequiredService<IRbacAuthorizationService>();
            Users = scope.ServiceProvider.GetRequiredService<IUserAdminService>();
            Roles = scope.ServiceProvider.GetRequiredService<IRoleAdminService>();
            Me = scope.ServiceProvider.GetRequiredService<ICurrentUserQuery>();
        }

        public static Task<Harness> CreateAsync() => CreateAsync(new SystemRbacActor());

        public static async Task<Harness> CreateAsync(IRbacActor actor)
        {
            var root = new InMemoryDatabaseRoot();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
            services.AddOptions<RbacOptions>();
            services.AddSingleton(actor);
            services.AddScoped<IRbacActor>(_ => actor);
            services.AddScoped<IPermissionCache, MemoryPermissionCache>();
            services.AddScoped<IAuditWriter, EfAuditWriter>();
            services.AddDbContext<RbacDbContext>(o => o.UseInMemoryDatabase("rbac-app", root));
            services.AddScoped<IRbacDbContext>(sp => sp.GetRequiredService<RbacDbContext>());
            services.AddRbacApplication();
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var harness = new Harness(provider, scope);
            await harness.Db.Database.EnsureCreatedAsync();
            await RbacSeeder.SeedAsync(harness.Db, new RbacSeedOptions { SeedSystemCatalog = true, SeedDemoData = true });
            return harness;
        }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync();
        }
    }
}

internal sealed class FixedActor : IRbacActor
{
    public FixedActor(Guid userId) => UserId = userId;
    public string? Name => "test-actor";
    public Guid? UserId { get; }
    public string? IpAddress => null;
    public string? CorrelationId => null;
    public bool IsSystemProcess => false;
}
