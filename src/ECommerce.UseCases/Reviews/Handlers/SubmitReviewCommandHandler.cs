using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>
/// Submits a review for a product the customer actually purchased (US-K-001). One review per
/// customer per product; reviews queue for moderation with a Verified Purchase flag.
/// </summary>
public sealed class SubmitReviewCommandHandler(
    IProductRepository products,
    IProductReviewRepository reviews,
    IVerifiedPurchaseChecker verifiedPurchaseChecker,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter,
    TimeProvider timeProvider,
    IValidator<SubmitReviewCommand> validator) : IRequestHandler<SubmitReviewCommand, Result<SubmitReviewResponse>>
{
    public async Task<Result<SubmitReviewResponse>> Handle(
        SubmitReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return AuthorizationErrors.NotAuthenticated;
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<SubmitReviewResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var product = await products.GetActiveByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductReviewErrors.ProductNotFound;
        }

        if (await reviews.ExistsAsync(request.ProductId, request.CustomerId, cancellationToken))
        {
            return ProductReviewErrors.AlreadyReviewed;
        }

        if (!await verifiedPurchaseChecker.HasPurchasedAsync(request.CustomerId, request.ProductId, cancellationToken))
        {
            return ProductReviewErrors.ProductNotPurchased;
        }

        var review = ProductReview.Create(
            request.ProductId,
            request.CustomerId,
            request.Rating,
            request.Comment,
            verifiedPurchase: true,
            utcNow);

        reviews.Add(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ReviewSubmitted,
            "ProductReview",
            review.Id.ToString(),
            After: new
            {
                review.ProductId,
                review.CustomerId,
                review.Rating,
                review.VerifiedPurchase
            }),
            cancellationToken);

        return new SubmitReviewResponse(
            review.Id,
            review.ProductId,
            review.Status.ToString(),
            review.VerifiedPurchase);
    }
}
