using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rbac.Domain.Entities;
using Rbac.Domain.ValueObjects;

namespace Rbac.Infrastructure.Persistence.Configurations;

internal static class Columns
{
    public const int Code = 64;
    public const int Name = 128;
    public const int Display = 256;
    public const int ExternalId = 256;
    public const int Email = 256;
    public const int Description = 512;
    public const int Actor = 128;
    public const int Route = 256;
    public const int Icon = 64;
}

internal sealed class RbacUserConfiguration : IEntityTypeConfiguration<RbacUser>
{
    public void Configure(EntityTypeBuilder<RbacUser> builder)
    {
        builder.ToTable("RbacUsers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalId).HasMaxLength(Columns.ExternalId).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(Columns.Display).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(Columns.Email);
        builder.Property(x => x.CreatedBy).HasMaxLength(Columns.Actor);
        builder.Property(x => x.UpdatedBy).HasMaxLength(Columns.Actor);
        builder.Property(x => x.DeletedBy).HasMaxLength(Columns.Actor);
        builder.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Username).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");
    }
}

internal sealed class RbacRoleConfiguration : IEntityTypeConfiguration<RbacRole>
{
    public void Configure(EntityTypeBuilder<RbacRole> builder)
    {
        builder.ToTable("RbacRoles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Columns.Description);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RbacPermissionConfiguration : IEntityTypeConfiguration<RbacPermission>
{
    public void Configure(EntityTypeBuilder<RbacPermission> builder)
    {
        builder.ToTable("RbacPermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(PermissionCode.MaxValueLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Columns.Description);
        builder.Property(x => x.Resource).HasMaxLength(PermissionCode.MaxSegmentLength).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(PermissionCode.MaxSegmentLength).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Resource, x.Action }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class RbacApplicationConfiguration : IEntityTypeConfiguration<RbacApplication>
{
    public void Configure(EntityTypeBuilder<RbacApplication> builder)
    {
        builder.ToTable("RbacApplications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Columns.Description);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class RbacProgramConfiguration : IEntityTypeConfiguration<RbacProgram>
{
    public void Configure(EntityTypeBuilder<RbacProgram> builder)
    {
        builder.ToTable("RbacPrograms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Columns.Description);
        builder.Property(x => x.Module).HasMaxLength(Columns.Name);
        builder.Property(x => x.Version).HasMaxLength(32);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Application).WithMany(a => a.Programs).HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RbacMenuConfiguration : IEntityTypeConfiguration<RbacMenu>
{
    public void Configure(EntityTypeBuilder<RbacMenu> builder)
    {
        builder.ToTable("RbacMenus");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(Columns.Display).IsRequired();
        builder.Property(x => x.Route).HasMaxLength(Columns.Route);
        builder.Property(x => x.Icon).HasMaxLength(Columns.Icon);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.ParentId, x.SortOrder });
        builder.HasOne(x => x.Parent).WithMany(m => m.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Program).WithMany(p => p.Menus).HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Application).WithMany(a => a.Menus).HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RbacTenantConfiguration : IEntityTypeConfiguration<RbacTenant>
{
    public void Configure(EntityTypeBuilder<RbacTenant> builder)
    {
        builder.ToTable("RbacTenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Columns.Description);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class RbacScopeConfiguration : IEntityTypeConfiguration<RbacScope>
{
    public void Configure(EntityTypeBuilder<RbacScope> builder)
    {
        builder.ToTable("RbacScopes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(Columns.Description);
        builder.Property(x => x.ScopeType).HasMaxLength(Columns.Code).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RbacResourceConfiguration : IEntityTypeConfiguration<RbacResource>
{
    public void Configure(EntityTypeBuilder<RbacResource> builder)
    {
        builder.ToTable("RbacResources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceType).HasMaxLength(Columns.Code).IsRequired();
        builder.Property(x => x.ResourceKey).HasMaxLength(Columns.Display).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(Columns.Display);
        builder.HasIndex(x => new { x.TenantId, x.ResourceType, x.ResourceKey }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("RbacUserRoles");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.Property(x => x.AssignedBy).HasMaxLength(Columns.Actor);
        builder.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.RoleId);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RbacRolePermissions");
        builder.HasKey(x => new { x.RoleId, x.PermissionId });
        builder.Property(x => x.Effect).HasConversion<int>();
        builder.Property(x => x.AssignedBy).HasMaxLength(Columns.Actor);
        builder.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Scope).WithMany().HasForeignKey(x => x.ScopeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PermissionId);
    }
}

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("RbacUserPermissions");
        builder.HasKey(x => new { x.UserId, x.PermissionId });
        builder.Property(x => x.Effect).HasConversion<int>();
        builder.Property(x => x.AssignedBy).HasMaxLength(Columns.Actor);
        builder.HasOne(x => x.User).WithMany(u => u.UserPermissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Permission).WithMany(p => p.UserPermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Scope).WithMany().HasForeignKey(x => x.ScopeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PermissionId);
    }
}

internal sealed class ProgramPermissionConfiguration : IEntityTypeConfiguration<ProgramPermission>
{
    public void Configure(EntityTypeBuilder<ProgramPermission> builder)
    {
        builder.ToTable("RbacProgramPermissions");
        builder.HasKey(x => new { x.ProgramId, x.PermissionId });
        builder.HasOne(x => x.Program).WithMany(p => p.ProgramPermissions).HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Permission).WithMany(p => p.ProgramPermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserTenantConfiguration : IEntityTypeConfiguration<UserTenant>
{
    public void Configure(EntityTypeBuilder<UserTenant> builder)
    {
        builder.ToTable("RbacUserTenants");
        builder.HasKey(x => new { x.UserId, x.TenantId });
        builder.Property(x => x.AssignedBy).HasMaxLength(Columns.Actor);
        builder.HasOne(x => x.User).WithMany(u => u.UserTenants).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tenant).WithMany(t => t.UserTenants).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RoleTenantConfiguration : IEntityTypeConfiguration<RoleTenant>
{
    public void Configure(EntityTypeBuilder<RoleTenant> builder)
    {
        builder.ToTable("RbacRoleTenants");
        builder.HasKey(x => new { x.RoleId, x.TenantId });
        builder.HasOne(x => x.Role).WithMany(r => r.RoleTenants).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tenant).WithMany(t => t.RoleTenants).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuthorizationAuditLogConfiguration : IEntityTypeConfiguration<AuthorizationAuditLog>
{
    public void Configure(EntityTypeBuilder<AuthorizationAuditLog> builder)
    {
        builder.ToTable("RbacAuthorizationAuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.Actor).HasMaxLength(Columns.Actor).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(Columns.Name).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(2000);
        builder.Property(x => x.NewValue).HasMaxLength(2000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}
