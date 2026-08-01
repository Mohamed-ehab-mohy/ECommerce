using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Audit.Queries;

public sealed record GetAuditLogsQuery(
    Guid? ActorId = null,
    string? Action = null,
    string? EntityType = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedAuditLogsResponse>>;
