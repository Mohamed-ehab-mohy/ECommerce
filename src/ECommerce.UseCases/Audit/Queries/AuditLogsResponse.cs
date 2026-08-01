namespace ECommerce.UseCases.Audit.Queries;

public sealed record AuditLogEntryResponse(
    long Id,
    Guid? ActorId,
    string Action,
    string EntityType,
    string? EntityId,
    string? Before,
    string? After,
    string? Ip,
    string? TraceId,
    string Hash,
    string? PreviousHash,
    DateTime OccurredAt);

public sealed record PagedAuditLogsResponse(
    IReadOnlyList<AuditLogEntryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
