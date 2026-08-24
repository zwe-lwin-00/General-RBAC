-- General RBAC V1 schema (SQL Server)
-- Naming: Rbac* prefix so the library can live beside a host Users table.
-- Soft delete: IsDeleted + filtered unique indexes.
-- Authentication is NOT stored here (no passwords, tokens, or MFA).

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dbo')
    EXEC('CREATE SCHEMA dbo');

CREATE TABLE dbo.RbacTenants (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacTenants PRIMARY KEY,
    Code NVARCHAR(64) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RbacTenants_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacTenants_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL
);
CREATE UNIQUE INDEX UX_RbacTenants_Code ON dbo.RbacTenants (Code) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacApplications (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacApplications PRIMARY KEY,
    Code NVARCHAR(64) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RbacApplications_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacApplications_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL
);
CREATE UNIQUE INDEX UX_RbacApplications_Code ON dbo.RbacApplications (Code) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacUsers (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacUsers PRIMARY KEY,
    ExternalId NVARCHAR(256) NOT NULL,
    Username NVARCHAR(128) NOT NULL,
    DisplayName NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RbacUsers_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacUsers_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL
);
CREATE UNIQUE INDEX UX_RbacUsers_ExternalId ON dbo.RbacUsers (ExternalId) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_RbacUsers_Username ON dbo.RbacUsers (Username) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_RbacUsers_Email ON dbo.RbacUsers (Email) WHERE Email IS NOT NULL AND IsDeleted = 0;

CREATE TABLE dbo.RbacRoles (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacRoles PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NULL,
    Code NVARCHAR(64) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    IsSystemRole BIT NOT NULL CONSTRAINT DF_RbacRoles_IsSystemRole DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_RbacRoles_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacRoles_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL,
    CONSTRAINT FK_RbacRoles_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.RbacTenants (Id)
);
CREATE UNIQUE INDEX UX_RbacRoles_Tenant_Code ON dbo.RbacRoles (TenantId, Code) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacPermissions (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacPermissions PRIMARY KEY,
    Code NVARCHAR(129) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    Resource NVARCHAR(64) NOT NULL,
    Action NVARCHAR(64) NOT NULL,
    IsSystemPermission BIT NOT NULL CONSTRAINT DF_RbacPermissions_IsSystem DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_RbacPermissions_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacPermissions_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL
);
CREATE UNIQUE INDEX UX_RbacPermissions_Code ON dbo.RbacPermissions (Code) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_RbacPermissions_Resource_Action ON dbo.RbacPermissions (Resource, Action) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacScopes (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacScopes PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NULL,
    Code NVARCHAR(64) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    ScopeType NVARCHAR(64) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RbacScopes_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacScopes_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL,
    CONSTRAINT FK_RbacScopes_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.RbacTenants (Id)
);
CREATE UNIQUE INDEX UX_RbacScopes_Tenant_Code ON dbo.RbacScopes (TenantId, Code) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacResources (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacResources PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NULL,
    ResourceType NVARCHAR(64) NOT NULL,
    ResourceKey NVARCHAR(256) NOT NULL,
    DisplayName NVARCHAR(256) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RbacResources_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacResources_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL
);
CREATE UNIQUE INDEX UX_RbacResources_Type_Key ON dbo.RbacResources (TenantId, ResourceType, ResourceKey) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacPrograms (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacPrograms PRIMARY KEY,
    ApplicationId UNIQUEIDENTIFIER NULL,
    TenantId UNIQUEIDENTIFIER NULL,
    Code NVARCHAR(64) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    Module NVARCHAR(128) NULL,
    Version NVARCHAR(32) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RbacPrograms_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacPrograms_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL,
    CONSTRAINT FK_RbacPrograms_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.RbacApplications (Id),
    CONSTRAINT FK_RbacPrograms_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.RbacTenants (Id)
);
CREATE UNIQUE INDEX UX_RbacPrograms_Tenant_Code ON dbo.RbacPrograms (TenantId, Code) WHERE IsDeleted = 0;

CREATE TABLE dbo.RbacMenus (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacMenus PRIMARY KEY,
    ApplicationId UNIQUEIDENTIFIER NULL,
    TenantId UNIQUEIDENTIFIER NULL,
    ParentId UNIQUEIDENTIFIER NULL,
    ProgramId UNIQUEIDENTIFIER NULL,
    Code NVARCHAR(64) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    DisplayName NVARCHAR(256) NOT NULL,
    Route NVARCHAR(256) NULL,
    Icon NVARCHAR(64) NULL,
    MenuType INT NOT NULL CONSTRAINT DF_RbacMenus_Type DEFAULT (1), -- 0 Group, 1 Item, 2 ExternalLink
    SortOrder INT NOT NULL CONSTRAINT DF_RbacMenus_Sort DEFAULT (0),
    IsVisible BIT NOT NULL CONSTRAINT DF_RbacMenus_IsVisible DEFAULT (1),
    IsActive BIT NOT NULL CONSTRAINT DF_RbacMenus_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_RbacMenus_IsDeleted DEFAULT (0),
    CreatedAt DATETIMEOFFSET(7) NOT NULL,
    CreatedBy NVARCHAR(128) NULL,
    UpdatedAt DATETIMEOFFSET(7) NULL,
    UpdatedBy NVARCHAR(128) NULL,
    DeletedAt DATETIMEOFFSET(7) NULL,
    DeletedBy NVARCHAR(128) NULL,
    CONSTRAINT FK_RbacMenus_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.RbacApplications (Id),
    CONSTRAINT FK_RbacMenus_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.RbacTenants (Id),
    CONSTRAINT FK_RbacMenus_Parent FOREIGN KEY (ParentId) REFERENCES dbo.RbacMenus (Id),
    CONSTRAINT FK_RbacMenus_Program FOREIGN KEY (ProgramId) REFERENCES dbo.RbacPrograms (Id)
);
CREATE UNIQUE INDEX UX_RbacMenus_Code ON dbo.RbacMenus (Code) WHERE IsDeleted = 0;
CREATE INDEX IX_RbacMenus_Parent_Sort ON dbo.RbacMenus (ParentId, SortOrder);

CREATE TABLE dbo.RbacUserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIMEOFFSET(7) NOT NULL,
    AssignedBy NVARCHAR(128) NULL,
    CONSTRAINT PK_RbacUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_RbacUserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.RbacUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacUserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.RbacRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_RbacUserRoles_RoleId ON dbo.RbacUserRoles (RoleId);

CREATE TABLE dbo.RbacRolePermissions (
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    ScopeId UNIQUEIDENTIFIER NULL,
    Effect INT NOT NULL CONSTRAINT DF_RbacRolePermissions_Effect DEFAULT (1), -- 1 Allow, 2 Deny
    AssignedAt DATETIMEOFFSET(7) NOT NULL,
    AssignedBy NVARCHAR(128) NULL,
    CONSTRAINT PK_RbacRolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RbacRolePermissions_Role FOREIGN KEY (RoleId) REFERENCES dbo.RbacRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacRolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.RbacPermissions (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacRolePermissions_Scope FOREIGN KEY (ScopeId) REFERENCES dbo.RbacScopes (Id)
);
CREATE INDEX IX_RbacRolePermissions_PermissionId ON dbo.RbacRolePermissions (PermissionId);

CREATE TABLE dbo.RbacUserPermissions (
    UserId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    ScopeId UNIQUEIDENTIFIER NULL,
    Effect INT NOT NULL CONSTRAINT DF_RbacUserPermissions_Effect DEFAULT (1),
    AssignedAt DATETIMEOFFSET(7) NOT NULL,
    AssignedBy NVARCHAR(128) NULL,
    CONSTRAINT PK_RbacUserPermissions PRIMARY KEY (UserId, PermissionId),
    CONSTRAINT FK_RbacUserPermissions_User FOREIGN KEY (UserId) REFERENCES dbo.RbacUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacUserPermissions_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.RbacPermissions (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacUserPermissions_Scope FOREIGN KEY (ScopeId) REFERENCES dbo.RbacScopes (Id)
);

CREATE TABLE dbo.RbacProgramPermissions (
    ProgramId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_RbacProgramPermissions PRIMARY KEY (ProgramId, PermissionId),
    CONSTRAINT FK_RbacProgramPermissions_Program FOREIGN KEY (ProgramId) REFERENCES dbo.RbacPrograms (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacProgramPermissions_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.RbacPermissions (Id) ON DELETE CASCADE
);

CREATE TABLE dbo.RbacUserTenants (
    UserId UNIQUEIDENTIFIER NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    IsDefault BIT NOT NULL CONSTRAINT DF_RbacUserTenants_IsDefault DEFAULT (0),
    AssignedAt DATETIMEOFFSET(7) NOT NULL,
    AssignedBy NVARCHAR(128) NULL,
    CONSTRAINT PK_RbacUserTenants PRIMARY KEY (UserId, TenantId),
    CONSTRAINT FK_RbacUserTenants_User FOREIGN KEY (UserId) REFERENCES dbo.RbacUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacUserTenants_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.RbacTenants (Id) ON DELETE CASCADE
);

CREATE TABLE dbo.RbacRoleTenants (
    RoleId UNIQUEIDENTIFIER NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_RbacRoleTenants PRIMARY KEY (RoleId, TenantId),
    CONSTRAINT FK_RbacRoleTenants_Role FOREIGN KEY (RoleId) REFERENCES dbo.RbacRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RbacRoleTenants_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.RbacTenants (Id) ON DELETE CASCADE
);

CREATE TABLE dbo.RbacAuthorizationAuditLogs (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RbacAuthorizationAuditLogs PRIMARY KEY,
    OccurredAt DATETIMEOFFSET(7) NOT NULL,
    EventType INT NOT NULL,
    Actor NVARCHAR(128) NOT NULL,
    ActorUserId UNIQUEIDENTIFIER NULL,
    TargetType NVARCHAR(128) NOT NULL,
    TargetId UNIQUEIDENTIFIER NULL,
    OldValue NVARCHAR(2000) NULL,
    NewValue NVARCHAR(2000) NULL,
    IpAddress NVARCHAR(64) NULL,
    CorrelationId NVARCHAR(64) NULL
);
CREATE INDEX IX_RbacAudit_OccurredAt ON dbo.RbacAuthorizationAuditLogs (OccurredAt);
CREATE INDEX IX_RbacAudit_Target ON dbo.RbacAuthorizationAuditLogs (TargetType, TargetId);
