namespace Rbac.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkDeleted(string? actor, DateTimeOffset? at = null)
    {
        IsDeleted = true;
        DeletedAt = at ?? DateTimeOffset.UtcNow;
        DeletedBy = actor;
        UpdatedAt = DeletedAt;
        UpdatedBy = actor;
    }

    public void Touch(string? actor, DateTimeOffset? at = null)
    {
        UpdatedAt = at ?? DateTimeOffset.UtcNow;
        UpdatedBy = actor;
    }
}
