using ECommerce.Domain.Audit;
using ECommerce.Shared.Audit;

namespace ECommerce.UseCases.Audit.Ports;

public sealed record AuditOperation(
    string Action,
    string EntityType,
    string? EntityId = null,
    object? Before = null,
    object? After = null,
    Guid? ActorId = null,
    AuditActorType? ActorType = null);

public interface IAuditLogWriter
{
    Task WriteAsync(AuditOperation operation, CancellationToken cancellationToken);
}
