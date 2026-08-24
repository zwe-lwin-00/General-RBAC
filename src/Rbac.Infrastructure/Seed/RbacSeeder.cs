using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Rbac.Domain;
using Rbac.Domain.Entities;
using Rbac.Domain.Enums;
using Rbac.Domain.ValueObjects;
using Rbac.Infrastructure.Persistence;

namespace Rbac.Infrastructure.Seed;

public static class SeedIds
{
    public static Guid For(string name)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("general-rbac:" + name));
        return new Guid(hash.AsSpan(0, 16).ToArray());
    }
}

public sealed class RbacSeedOptions
{
    public bool SeedSystemCatalog { get; set; } = true;
    public bool SeedDemoData { get; set; }
}

public static class RbacSeeder
{
    public static async Task SeedAsync(RbacDbContext db, RbacSeedOptions options, CancellationToken cancellationToken = default)
    {
        if (options.SeedSystemCatalog)
        {
            await SeedSystemCatalogAsync(db, cancellationToken);
        }

        if (options.SeedDemoData)
        {
            await SeedDemoAsync(db, cancellationToken);
        }
    }

    private static async Task SeedSystemCatalogAsync(RbacDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Permissions.AnyAsync(cancellationToken))
        {
            return;
        }

        var app = AddApplication(db, "SAMPLE", "Sample Application", "Reusable RBAC sample host");
        var permissions = AddPermissions(db, new (string Resource, string Action, string Name, bool System)[]
        {
            ("rbac.users", "read", "Read users", true),
            ("rbac.users", "create", "Create users", true),
            ("rbac.users", "update", "Update users", true),
            ("rbac.users", "delete", "Delete users", true),
            ("rbac.roles", "read", "Read roles", true),
            ("rbac.roles", "create", "Create roles", true),
            ("rbac.roles", "update", "Update roles", true),
            ("rbac.roles", "delete", "Delete roles", true),
            ("rbac.permissions", "read", "Read permissions", true),
            ("rbac.permissions", "create", "Create permissions", true),
            ("rbac.permissions", "update", "Update permissions", true),
            ("rbac.permissions", "delete", "Delete permissions", true),
            ("rbac.programs", "read", "Read programs", true),
            ("rbac.programs", "create", "Create programs", true),
            ("rbac.programs", "update", "Update programs", true),
            ("rbac.programs", "delete", "Delete programs", true),
            ("rbac.menus", "read", "Read menus", true),
            ("rbac.menus", "create", "Create menus", true),
            ("rbac.menus", "update", "Update menus", true),
            ("rbac.menus", "delete", "Delete menus", true),
            ("rbac.assign", "roles", "Assign user roles", true),
            ("rbac.assign", "permissions", "Assign direct permissions", true)
        });

        var superAdmin = AddRole(db, "SUPER_ADMIN", "Super Admin", "Full authorization catalog access", true);
        foreach (var permission in permissions.Values)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = superAdmin.Id,
                PermissionId = permission.Id,
                AssignedBy = "seed"
            });
        }

        var usersProgram = AddProgram(db, app, "RBAC_USERS", "User Management", "Administration", "Manage RBAC users");
        var rolesProgram = AddProgram(db, app, "RBAC_ROLES", "Role Management", "Administration", "Manage roles and role permissions");
        var permsProgram = AddProgram(db, app, "RBAC_PERMISSIONS", "Permission Catalog", "Administration", "Manage permission definitions");
        var programsProgram = AddProgram(db, app, "RBAC_PROGRAMS", "Program Catalog", "Administration", "Manage application programs");
        var menusProgram = AddProgram(db, app, "RBAC_MENUS", "Menu Management", "Administration", "Manage navigation");

        Link(db, usersProgram, permissions, "rbac.users.read", "rbac.users.create", "rbac.users.update", "rbac.users.delete", "rbac.assign.roles", "rbac.assign.permissions");
        Link(db, rolesProgram, permissions, "rbac.roles.read", "rbac.roles.create", "rbac.roles.update", "rbac.roles.delete");
        Link(db, permsProgram, permissions, "rbac.permissions.read", "rbac.permissions.create", "rbac.permissions.update", "rbac.permissions.delete");
        Link(db, programsProgram, permissions, "rbac.programs.read", "rbac.programs.create", "rbac.programs.update", "rbac.programs.delete");
        Link(db, menusProgram, permissions, "rbac.menus.read", "rbac.menus.create", "rbac.menus.update", "rbac.menus.delete");

        var adminMenu = AddMenu(db, app, null, null, "ADMIN", "Administration", "Administration", null, "shield", MenuType.Group, 100);
        AddMenu(db, app, adminMenu, usersProgram, "ADMIN_USERS", "Users", "Users", "/admin/users", "users", MenuType.Item, 10);
        AddMenu(db, app, adminMenu, rolesProgram, "ADMIN_ROLES", "Roles", "Roles", "/admin/roles", "badge", MenuType.Item, 20);
        AddMenu(db, app, adminMenu, permsProgram, "ADMIN_PERMISSIONS", "Permissions", "Permissions", "/admin/permissions", "key", MenuType.Item, 30);
        AddMenu(db, app, adminMenu, programsProgram, "ADMIN_PROGRAMS", "Programs", "Programs", "/admin/programs", "blocks", MenuType.Item, 40);
        AddMenu(db, app, adminMenu, menusProgram, "ADMIN_MENUS", "Menus", "Menus", "/admin/menus", "menu", MenuType.Item, 50);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDemoAsync(RbacDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var app = await db.Applications.FirstAsync(a => a.Code == "SAMPLE", cancellationToken);
        var permissions = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);
        AddPermissions(db, new (string Resource, string Action, string Name, bool System)[]
        {
            ("passenger", "read", "Read passengers", false),
            ("passenger", "create", "Create passengers", false),
            ("passenger", "update", "Update passengers", false),
            ("passenger", "delete", "Delete passengers", false),
            ("passenger", "export", "Export passengers", false),
            ("report", "read", "Read reports", false),
            ("report", "export", "Export reports", false),
            ("report", "approve", "Approve reports", false)
        }, permissions);
        await db.SaveChangesAsync(cancellationToken);
        permissions = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);

        var admin = AddRole(db, "ADMIN", "Admin", "Application and passenger administration", false);
        var supervisor = AddRole(db, "SUPERVISOR", "Supervisor", "Full passenger and report access", false);
        var officer = AddRole(db, "OFFICER", "Officer", "Day-to-day passenger work", false);
        var viewer = AddRole(db, "VIEWER", "Viewer", "Read-only access", false);
        var superAdmin = await db.Roles.FirstAsync(r => r.Code == "SUPER_ADMIN", cancellationToken);

        Grant(db, admin, permissions,
            RbacPermissions.UsersRead, RbacPermissions.UsersCreate, RbacPermissions.UsersUpdate,
            RbacPermissions.RolesRead, RbacPermissions.PermissionsRead, RbacPermissions.ProgramsRead, RbacPermissions.MenusRead,
            "passenger.read", "passenger.create", "passenger.update", "passenger.delete", "passenger.export",
            "report.read", "report.export", "report.approve");
        Grant(db, supervisor, permissions,
            "passenger.read", "passenger.create", "passenger.update", "passenger.delete", "passenger.export",
            "report.read", "report.export", "report.approve");
        Grant(db, officer, permissions, "passenger.read", "passenger.create", "passenger.update", "report.read");
        Grant(db, viewer, permissions, "passenger.read", "report.read");

        var passengerList = AddProgram(db, app, "PASSENGER_LIST", "Passenger Listing", "Passenger Management", "Search and maintain passengers");
        var reports = AddProgram(db, app, "REPORTS", "Reports", "Reports", "Operational reports");
        Link(db, passengerList, permissions, "passenger.read", "passenger.create", "passenger.update", "passenger.delete", "passenger.export");
        Link(db, reports, permissions, "report.read", "report.export", "report.approve");

        var dashboard = AddMenu(db, app, null, null, "DASHBOARD", "Dashboard", "Dashboard", "/", "home", MenuType.Item, 1);
        _ = dashboard;
        var passengerRoot = AddMenu(db, app, null, null, "PASSENGERS", "Passenger Management", "Passengers", null, "users", MenuType.Group, 10);
        AddMenu(db, app, passengerRoot, passengerList, "PASSENGER_LISTING", "Passenger Listing", "Passenger Listing", "/passengers", "list", MenuType.Item, 10);
        AddMenu(db, app, null, reports, "REPORTS_MENU", "Reports", "Reports", "/reports", "chart", MenuType.Item, 20);

        var superUser = AddUser(db, "superadmin", "superadmin", "Super Admin", "superadmin@example.com");
        var adminUser = AddUser(db, "admin", "admin", "Ada Admin", "admin@example.com");
        var supervisorUser = AddUser(db, "supervisor", "supervisor", "Sam Supervisor", "supervisor@example.com");
        var officerUser = AddUser(db, "officer", "officer", "Omar Officer", "officer@example.com");
        var viewerUser = AddUser(db, "viewer", "viewer", "Vera Viewer", "viewer@example.com");
        var john = AddUser(db, "john", "john", "John Denied-Export", "john@example.com");

        AssignRole(db, superUser, superAdmin);
        AssignRole(db, adminUser, admin);
        AssignRole(db, supervisorUser, supervisor);
        AssignRole(db, officerUser, officer);
        AssignRole(db, viewerUser, viewer);
        AssignRole(db, john, supervisor);
        db.UserPermissions.Add(new UserPermission
        {
            UserId = john.Id,
            PermissionId = permissions["report.export"].Id,
            Effect = PermissionEffect.Deny,
            AssignedBy = "seed"
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, RbacPermission> AddPermissions(
        RbacDbContext db,
        IEnumerable<(string Resource, string Action, string Name, bool System)> specs,
        Dictionary<string, RbacPermission>? existing = null)
    {
        existing ??= new Dictionary<string, RbacPermission>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in specs)
        {
            var code = PermissionCode.Create(spec.Resource, spec.Action);
            if (existing.ContainsKey(code.Value))
            {
                continue;
            }

            var permission = new RbacPermission
            {
                Id = SeedIds.For("permission:" + code.Value),
                Name = spec.Name,
                IsSystemPermission = spec.System,
                CreatedBy = "seed"
            };
            permission.SetCode(code);
            db.Permissions.Add(permission);
            existing[code.Value] = permission;
        }

        return existing;
    }

    private static RbacApplication AddApplication(RbacDbContext db, string code, string name, string description)
    {
        var app = new RbacApplication
        {
            Id = SeedIds.For("application:" + code),
            Code = code,
            Name = name,
            Description = description,
            CreatedBy = "seed"
        };
        db.Applications.Add(app);
        return app;
    }

    private static RbacRole AddRole(RbacDbContext db, string code, string name, string description, bool system)
    {
        var role = new RbacRole
        {
            Id = SeedIds.For("role:" + code),
            Code = code,
            Name = name,
            Description = description,
            IsSystemRole = system,
            CreatedBy = "seed"
        };
        db.Roles.Add(role);
        return role;
    }

    private static RbacProgram AddProgram(RbacDbContext db, RbacApplication app, string code, string name, string module, string description)
    {
        var program = new RbacProgram
        {
            Id = SeedIds.For("program:" + code),
            ApplicationId = app.Id,
            Code = code,
            Name = name,
            Module = module,
            Version = "1.0",
            Description = description,
            CreatedBy = "seed"
        };
        db.Programs.Add(program);
        return program;
    }

    private static RbacMenu AddMenu(
        RbacDbContext db,
        RbacApplication app,
        RbacMenu? parent,
        RbacProgram? program,
        string code,
        string name,
        string displayName,
        string? route,
        string? icon,
        MenuType type,
        int sort)
    {
        var menu = new RbacMenu
        {
            Id = SeedIds.For("menu:" + code),
            ApplicationId = app.Id,
            ParentId = parent?.Id,
            ProgramId = program?.Id,
            Code = code,
            Name = name,
            DisplayName = displayName,
            Route = route,
            Icon = icon,
            MenuType = type,
            SortOrder = sort,
            CreatedBy = "seed"
        };
        db.Menus.Add(menu);
        return menu;
    }

    private static RbacUser AddUser(RbacDbContext db, string externalId, string username, string displayName, string email)
    {
        var user = new RbacUser
        {
            Id = SeedIds.For("user:" + username),
            ExternalId = externalId,
            Username = username,
            DisplayName = displayName,
            Email = email,
            CreatedBy = "seed"
        };
        db.Users.Add(user);
        return user;
    }

    private static void AssignRole(RbacDbContext db, RbacUser user, RbacRole role) =>
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedBy = "seed" });

    private static void Grant(RbacDbContext db, RbacRole role, IReadOnlyDictionary<string, RbacPermission> permissions, params string[] codes)
    {
        foreach (var code in codes)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissions[code].Id,
                AssignedBy = "seed"
            });
        }
    }

    private static void Link(RbacDbContext db, RbacProgram program, IReadOnlyDictionary<string, RbacPermission> permissions, params string[] codes)
    {
        foreach (var code in codes)
        {
            db.ProgramPermissions.Add(new ProgramPermission
            {
                ProgramId = program.Id,
                PermissionId = permissions[code].Id
            });
        }
    }
}
