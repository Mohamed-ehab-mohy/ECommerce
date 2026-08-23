using ECommerce.Domain.Audit;

namespace ECommerce.UseCases.Audit.Ports;

public interface IAuditEntryRepository
{
    Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken);

    Task<string?> GetLatestHashAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken);

    Task<int> CountAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
