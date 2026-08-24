namespace Rbac.Application;

public sealed class RbacOptions
{
    /// <summary>Claim type that holds the identity-provider subject mapped to <c>RbacUser.ExternalId</c>.</summary>
    public string ExternalIdClaimType { get; set; } = "sub";

    /// <summary>Fallback claim types used when the primary claim is missing.</summary>
    public string[] AdditionalExternalIdClaimTypes { get; set; } =
    [
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
    ];

    public TimeSpan PermissionCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    public bool EnableAdminApi { get; set; } = true;

    public string AdminApiPrefix { get; set; } = "/api/rbac";

    /// <summary>When true, unmapped authenticated principals are rejected by permission checks.</summary>
    public bool RequireMappedUser { get; set; } = true;
}
