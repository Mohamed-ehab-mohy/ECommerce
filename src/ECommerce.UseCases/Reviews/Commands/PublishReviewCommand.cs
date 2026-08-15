using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Responses;
using MediatR;

namespace ECommerce.UseCases.Reviews.Commands;

/// <summary>Approves a pending review for publication (US-K-002).</summary>
public sealed class PublishReviewCommand(
    Guid reviewId,
    Guid? moderatorId) : IRequest<Result<ReviewModerationResponse>>, IRequirePermission
{
    public Guid ReviewId { get; } = reviewId;

    public Guid? ModeratorId { get; } = moderatorId;

    public string Permission => Permissions.ReviewsModerate;
}
