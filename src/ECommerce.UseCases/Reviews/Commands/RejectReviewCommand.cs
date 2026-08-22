using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Commands;

/// <summary>Rejects a pending review with a reason (US-K-002).</summary>
public sealed class RejectReviewCommand(
    Guid reviewId,
    Guid? moderatorId,
    string reason) : IRequest<Result<ReviewModerationResponse>>, IRequirePermission
{
    public Guid ReviewId { get; } = reviewId;

    public Guid? ModeratorId { get; } = moderatorId;

    public string Reason { get; } = reason;

    public string Permission => Permissions.ReviewsModerate;
}
