using System.Text.Json;
using ECommerce.Shared.Audit;

namespace ECommerce.Domain.Audit;

public sealed class AuditEntry
{
    private AuditEntry()
    {
        Action = string.Empty;
        EntityType = string.Empty;
        Hash = string.Empty;
    }

    public long Id { get; private set; }

    public Guid? ActorId { get; private set; }

    public AuditActorType ActorType { get; private set; }

    public string Action { get; private set; }

    public string EntityType { get; private set; }

    public string? EntityId { get; private set; }

    public string? Before { get; private set; }

    public string? After { get; private set; }

    public string? Ip { get; private set; }

    public string? UserAgent { get; private set; }

    public string? TraceId { get; private set; }

    public string Hash { get; private set; }

    public string? PreviousHash { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static AuditEntry Create(
        Guid? actorId,
        AuditActorType actorType,
        string action,
        string entityType,
        string? entityId,
        string? before,
        string? after,
        string? ip,
        string? userAgent,
        string? traceId,
        string? previousHash,
        DateTime occurredAt)
    {
        occurredAt = new DateTime(
            occurredAt.Ticks - (occurredAt.Ticks % TimeSpan.TicksPerMillisecond),
            DateTimeKind.Utc);

        var entry = new AuditEntry
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before,
            After = after,
            Ip = ip,
            UserAgent = userAgent,
            TraceId = traceId,
            PreviousHash = previousHash,
            OccurredAt = occurredAt
        };

        entry.Hash = AuditChain.Compute(previousHash, entry.CanonicalPayload());
        return entry;
    }

    public string CanonicalPayload() => JsonSerializer.Serialize(new
    {
        Action,
        ActorId,
        ActorType,
        EntityType,
        EntityId,
        Before,
        After,
        OccurredAt
    });
}
