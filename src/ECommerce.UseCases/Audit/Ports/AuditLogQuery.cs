namespace ECommerce.UseCases.Audit.Ports;

public sealed record AuditLogQuery(
    Guid? ActorId = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20);
