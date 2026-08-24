using Microsoft.EntityFrameworkCore;
using Rbac.Infrastructure.Persistence;
using Rbac.Infrastructure.Seed;

namespace Rbac.Infrastructure.Tests;

public class SeedAndSchemaTests
{
    [Fact]
    public async Task Seeder_creates_stable_ids_and_relationships()
    {
        var options = new DbContextOptionsBuilder<RbacDbContext>()
            .UseInMemoryDatabase("rbac-infra-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new RbacDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await RbacSeeder.SeedAsync(db, new RbacSeedOptions { SeedSystemCatalog = true, SeedDemoData = true });

        Assert.Equal(SeedIds.For("user:john"), (await db.Users.SingleAsync(u => u.Username == "john")).Id);
        Assert.True(await db.RolePermissions.AnyAsync());
        Assert.True(await db.ProgramPermissions.AnyAsync());
        Assert.True(await db.Menus.AnyAsync(m => m.ParentId != null));
        Assert.Equal(1, await db.UserPermissions.CountAsync());

        await RbacSeeder.SeedAsync(db, new RbacSeedOptions { SeedSystemCatalog = true, SeedDemoData = true });
        Assert.Equal(6, await db.Users.CountAsync());
    }
}
