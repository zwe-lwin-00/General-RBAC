using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rbac.Application;
using Rbac.Application.Abstractions;
using Rbac.Domain.Entities;
using Rbac.Domain.Enums;
using Rbac.Infrastructure.Persistence;

namespace Rbac.Infrastructure.Caching;

public sealed class MemoryPermissionCache : IPermissionCache
{
    private readonly IMemoryCache _cache;
    private readonly RbacOptions _options;
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    public MemoryPermissionCache(IMemoryCache cache, IOptions<RbacOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public Task<IReadOnlySet<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(Key(userId), out IReadOnlySet<string>? value);
        return Task.FromResult(value);
    }

    public Task SetAsync(Guid userId, IReadOnlySet<string> permissions, CancellationToken cancellationToken = default)
    {
        var duration = _options.PermissionCacheDuration <= TimeSpan.Zero ? DefaultDuration : _options.PermissionCacheDuration;
        _cache.Set(Key(userId), permissions, duration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(Key(userId));
        return Task.CompletedTask;
    }

    public Task RemoveUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            _cache.Remove(Key(userId));
        }

        return Task.CompletedTask;
    }

    private static string Key(Guid userId) => $"rbac:permissions:user:{userId:D}";
}

public sealed class EfAuditWriter : IAuditWriter
{
    private readonly RbacDbContext _db;
    private readonly IRbacActor _actor;

    public EfAuditWriter(RbacDbContext db, IRbacActor actor)
    {
        _db = db;
        _actor = actor;
    }

    public async Task WriteAsync(
        AuditEventType eventType,
        string targetType,
        Guid? targetId,
        string? oldValue,
        string? newValue,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuthorizationAuditLog
        {
            EventType = eventType,
            Actor = string.IsNullOrWhiteSpace(_actor.Name) ? "system" : _actor.Name,
            ActorUserId = _actor.UserId,
            TargetType = targetType,
            TargetId = targetId,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = _actor.IpAddress,
            CorrelationId = _actor.CorrelationId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class SystemRbacActor : IRbacActor
{
    public string? Name => "system";
    public Guid? UserId => null;
    public string? IpAddress => null;
    public string? CorrelationId => null;
}
