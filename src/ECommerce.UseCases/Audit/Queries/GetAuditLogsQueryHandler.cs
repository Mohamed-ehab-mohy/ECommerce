using ECommerce.Domain.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Audit.Queries;

public sealed class GetAuditLogsQueryHandler(
    IAuditEntryRepository entries,
    IValidator<GetAuditLogsQuery> validator) : IRequestHandler<GetAuditLogsQuery, Result<PagedAuditLogsResponse>>
{
    public async Task<Result<PagedAuditLogsResponse>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedAuditLogsResponse>();
        }

        var query = new AuditLogQuery(
            request.ActorId,
            request.Action,
            request.EntityType,
            EntityId: null,
            request.From,
            request.To,
            request.Page,
            request.PageSize);

        var items = await entries.QueryAsync(query, cancellationToken);
        var total = await entries.CountAsync(query, cancellationToken);

        return Result<PagedAuditLogsResponse>.Success(new PagedAuditLogsResponse(
            items.Select(ToResponse).ToList(),
            request.Page,
            request.PageSize,
            total));
    }

    private static AuditLogEntryResponse ToResponse(AuditEntry entry) => new(
        entry.Id,
        entry.ActorId,
        entry.Action,
        entry.EntityType,
        entry.EntityId,
        entry.Before,
        entry.After,
        entry.Ip,
        entry.TraceId,
        entry.Hash,
        entry.PreviousHash,
        entry.OccurredAt);
}
