using ECommerce.Domain.Audit;
using ECommerce.Domain.Reviews;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>Approves a pending review; the rating aggregate recomputes on publication (US-K-002/003).</summary>
public sealed class PublishReviewCommandHandler(
    IProductReviewRepository reviews,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter,
    TimeProvider timeProvider,
    IValidator<PublishReviewCommand> validator) : IRequestHandler<PublishReviewCommand, Result<ReviewModerationResponse>>
{
    public async Task<Result<ReviewModerationResponse>> Handle(
        PublishReviewCommand request,
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

        var result = review.Publish(request.ModeratorId, utcNow);
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
            After: new { review.Status, request.ModeratorId }),
            cancellationToken);

        return new ReviewModerationResponse(
            review.Id,
            review.Status.ToString(),
            request.ModeratorId,
            review.ModeratedAt);
    }
}
