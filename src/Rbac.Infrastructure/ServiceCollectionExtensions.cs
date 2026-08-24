using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.Infrastructure.Caching;
using Rbac.Infrastructure.Persistence;
using Rbac.Infrastructure.Seed;

namespace Rbac.Infrastructure;

public sealed class RbacInfrastructureOptions
{
    public string? SqlServerConnectionString { get; set; }
    public string? SqliteConnectionString { get; set; }
    public bool UseInMemory { get; set; }
    public string InMemoryDatabaseName { get; set; } = "Rbac";
    public bool ApplyMigrations { get; set; }
    public bool EnsureCreated { get; set; } = true;
    public RbacSeedOptions Seed { get; set; } = new();
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRbacInfrastructure(
        this IServiceCollection services,
        Action<RbacInfrastructureOptions>? configure = null)
    {
        var infrastructure = new RbacInfrastructureOptions();
        configure?.Invoke(infrastructure);

        services.AddMemoryCache();
        services.AddSingleton(infrastructure);
        services.AddSingleton(infrastructure.Seed);
        services.AddScoped<IPermissionCache, MemoryPermissionCache>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.TryAddScopedActor();

        services.AddDbContext<RbacDbContext>(options =>
        {
            if (infrastructure.UseInMemory)
            {
                options.UseInMemoryDatabase(infrastructure.InMemoryDatabaseName);
                return;
            }

            if (!string.IsNullOrWhiteSpace(infrastructure.SqlServerConnectionString))
            {
                options.UseSqlServer(infrastructure.SqlServerConnectionString);
                return;
            }

            var sqlite = string.IsNullOrWhiteSpace(infrastructure.SqliteConnectionString)
                ? "Data Source=rbac.db"
                : infrastructure.SqliteConnectionString;
            options.UseSqlite(sqlite);
        });
        services.AddScoped<IRbacDbContext>(sp => sp.GetRequiredService<RbacDbContext>());
        services.AddHostedService<RbacDatabaseInitializer>();
        return services;
    }

    private static void TryAddScopedActor(this IServiceCollection services)
    {
        if (services.Any(s => s.ServiceType == typeof(IRbacActor)))
        {
            return;
        }

        services.AddScoped<IRbacActor, SystemRbacActor>();
    }
}

internal sealed class RbacDatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly RbacInfrastructureOptions _options;

    public RbacDatabaseInitializer(IServiceProvider services, RbacInfrastructureOptions options)
    {
        _services = services;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RbacDbContext>();
        if (_options.UseInMemory || _options.EnsureCreated)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        else if (_options.ApplyMigrations)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        await RbacSeeder.SeedAsync(db, _options.Seed, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
