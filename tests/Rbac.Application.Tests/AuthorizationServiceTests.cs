using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.Contracts;
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
        public ICurrentUserQuery Me { get; }

        private Harness(ServiceProvider provider, IServiceScope scope)
        {
            _provider = provider;
            _scope = scope;
            Db = scope.ServiceProvider.GetRequiredService<RbacDbContext>();
            Auth = scope.ServiceProvider.GetRequiredService<IRbacAuthorizationService>();
            Users = scope.ServiceProvider.GetRequiredService<IUserAdminService>();
            Me = scope.ServiceProvider.GetRequiredService<ICurrentUserQuery>();
        }

        public static async Task<Harness> CreateAsync()
        {
            var root = new InMemoryDatabaseRoot();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
            services.AddOptions<RbacOptions>();
            services.AddScoped<IRbacActor, SystemRbacActor>();
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
