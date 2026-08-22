using ECommerce.Domain.Audit;
using ECommerce.Domain.Reviews;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>Rejects a pending review with a reason (US-K-002).</summary>
public sealed class RejectReviewCommandHandler(
    IProductReviewRepository reviews,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter,
    TimeProvider timeProvider,
    IValidator<RejectReviewCommand> validator) : IRequestHandler<RejectReviewCommand, Result<ReviewModerationResponse>>
{
    public async Task<Result<ReviewModerationResponse>> Handle(
        RejectReviewCommand request,
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

        var result = review.Reject(request.ModeratorId, request.Reason, utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ReviewModerated,
            "ProductReview",
            review.Id.ToString(),
            Before: new { Status = ProductReviewStatus.Pending },
            After: new { review.Status, request.ModeratorId, review.RejectionReason }),
            cancellationToken);

        return new ReviewModerationResponse(
            review.Id,
            review.Status.ToString(),
            request.ModeratorId,
            review.ModeratedAt);
    }
}
