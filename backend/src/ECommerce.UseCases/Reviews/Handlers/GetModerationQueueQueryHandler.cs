using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Queries;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>Returns the moderation queue of pending reviews (US-K-002).</summary>
public sealed class GetModerationQueueQueryHandler(
    IProductReviewRepository reviews,
    IValidator<GetModerationQueueQuery> validator) : IRequestHandler<GetModerationQueueQuery, Result<ModerationQueueResponse>>
{
    public async Task<Result<ModerationQueueResponse>> Handle(
        GetModerationQueueQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ModerationQueueResponse>();
        }

        var pending = await reviews.ListPendingAsync(cancellationToken);

        var items = pending
            .OrderBy(review => review.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(review => new ModerationReviewResponse(
                review.Id,
                review.ProductId,
                review.Rating,
                review.Comment,
                review.VerifiedPurchase,
                review.CustomerId,
                review.CreatedAt))
            .ToList();

        return new ModerationQueueResponse(
            request.Page,
            request.PageSize,
            pending.Count,
            items);
    }
}
