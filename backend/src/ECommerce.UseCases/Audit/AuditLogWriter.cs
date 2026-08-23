using System.Text.Json;
using ECommerce.Domain.Audit;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;

namespace ECommerce.UseCases.Audit;

public sealed class AuditLogWriter(
    IAuditEntryRepository entries,
    IAuditContextProvider contextProvider) : IAuditLogWriter
{
    public async Task WriteAsync(AuditOperation operation, CancellationToken cancellationToken)
    {
        var context = contextProvider.Get();
        var previousHash = await entries.GetLatestHashAsync(cancellationToken);

        var entry = AuditEntry.Create(
            operation.ActorId ?? context.ActorId,
            operation.ActorType ?? context.ActorType ?? AuditActorType.User,
            operation.Action,
            operation.EntityType,
            operation.EntityId,
            operation.Before is null ? null : JsonSerializer.Serialize(operation.Before),
            operation.After is null ? null : JsonSerializer.Serialize(operation.After),
            context.Ip,
            context.UserAgent,
            context.TraceId,
            previousHash,
            DateTime.UtcNow);

        await entries.AppendAsync(entry, cancellationToken);
    }
}
