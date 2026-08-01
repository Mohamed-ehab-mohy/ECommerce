using ECommerce.Domain.Audit;
using ECommerce.Shared.Audit;

namespace ECommerce.UseCases.Audit.Ports;

public sealed record AuditContext(
    Guid? ActorId,
    AuditActorType? ActorType,
    string? Ip,
    string? UserAgent,
    string? TraceId);

public interface IAuditContextProvider
{
    AuditContext Get();
}
