using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Queries;

/// <summary>Returns the moderation queue of pending reviews.</summary>
public sealed class GetModerationQueueQuery(
    int page,
    int pageSize) : IRequest<Result<ModerationQueueResponse>>, IRequirePermission
{
    public int Page { get; } = page;

    public int PageSize { get; } = pageSize;

    public string Permission => Permissions.ReviewsModerate;
}
