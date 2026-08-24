using Microsoft.EntityFrameworkCore;
using Rbac.Application.Abstractions;
using Rbac.Domain.Common;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Persistence;

public class RbacDbContext : DbContext, IRbacDbContext
{
    public RbacDbContext(DbContextOptions<RbacDbContext> options) : base(options)
    {
    }

    public DbSet<RbacUser> Users => Set<RbacUser>();
    public DbSet<RbacRole> Roles => Set<RbacRole>();
    public DbSet<RbacPermission> Permissions => Set<RbacPermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RbacApplication> Applications => Set<RbacApplication>();
    public DbSet<RbacProgram> Programs => Set<RbacProgram>();
    public DbSet<ProgramPermission> ProgramPermissions => Set<ProgramPermission>();
    public DbSet<RbacMenu> Menus => Set<RbacMenu>();
    public DbSet<RbacTenant> Tenants => Set<RbacTenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<RoleTenant> RoleTenants => Set<RoleTenant>();
    public DbSet<RbacScope> Scopes => Set<RbacScope>();
    public DbSet<RbacResource> Resources => Set<RbacResource>();
    public DbSet<AuthorizationAuditLog> AuditLogs => Set<AuthorizationAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RbacDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAudits();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAudits()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RbacUser>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacRole>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacPermission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacApplication>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacProgram>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacMenu>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacTenant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacScope>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RbacResource>().HasQueryFilter(e => !e.IsDeleted);

        // Join rows must repeat the principal filters. Otherwise EF warns that a
        // required navigation can return null when the principal is soft-deleted.
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => !e.User.IsDeleted && !e.Role.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => !e.Role.IsDeleted && !e.Permission.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(e => !e.User.IsDeleted && !e.Permission.IsDeleted);
        modelBuilder.Entity<ProgramPermission>().HasQueryFilter(e => !e.Program.IsDeleted && !e.Permission.IsDeleted);
        modelBuilder.Entity<UserTenant>().HasQueryFilter(e => !e.User.IsDeleted && !e.Tenant.IsDeleted);
        modelBuilder.Entity<RoleTenant>().HasQueryFilter(e => !e.Role.IsDeleted && !e.Tenant.IsDeleted);
    }
}
