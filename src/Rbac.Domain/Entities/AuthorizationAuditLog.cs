using Rbac.Domain.Common;
using Rbac.Domain.Enums;

namespace Rbac.Domain.Entities;

public class AuthorizationAuditLog : Entity
{
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public AuditEventType EventType { get; set; }
    public string Actor { get; set; } = "system";
    public Guid? ActorUserId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}
