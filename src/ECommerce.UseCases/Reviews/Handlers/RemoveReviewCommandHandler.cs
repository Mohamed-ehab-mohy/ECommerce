using ECommerce.Domain.Audit;
using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>Removes a published review for compliance/abuse; the rating re-aggregates (US-K-004).</summary>
public sealed class RemoveReviewCommandHandler(
    IProductReviewRepository reviews,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter,
    TimeProvider timeProvider,
    IValidator<RemoveReviewCommand> validator) : IRequestHandler<RemoveReviewCommand, Result<ReviewModerationResponse>>
{
    public async Task<Result<ReviewModerationResponse>> Handle(
        RemoveReviewCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ReviewModerationResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            return ProductReviewErrors.ReviewNotFound;
        }

        var result = review.Remove(request.ModeratorId, request.Reason, utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ReviewRemovedAction,
            "ProductReview",
            review.Id.ToString(),
            Before: new { Status = ProductReviewStatus.Published },
            After: new { review.Status, request.ModeratorId, review.RejectionReason }),
            cancellationToken);

        return new ReviewModerationResponse(
            review.Id,
            review.Status.ToString(),
            request.ModeratorId,
            review.ModeratedAt);
    }
}
