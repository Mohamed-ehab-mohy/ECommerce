using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Reviews;

/// <summary>
/// Customer review of a product (FRS-K-001). Reviews are submitted for moderation, published by
/// moderators, or rejected with a reason; published reviews can be removed for compliance/abuse
/// (FRS-K-004). The rating aggregate is recomputed on publish and remove (FRS-K-003).
/// </summary>
public sealed class ProductReview : BaseEntity<Guid>
{
    private ProductReview()
    {
        Comment = string.Empty;
    }

    public Guid ProductId { get; private set; }

    public Guid CustomerId { get; private set; }

    public int Rating { get; private set; }

    public string Comment { get; private set; }

    public ProductReviewStatus Status { get; private set; }

    public bool VerifiedPurchase { get; private set; }

    public Guid? ModeratorId { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime? ModeratedAt { get; private set; }

    public static ProductReview Create(
        Guid productId,
        Guid customerId,
        int rating,
        string comment,
        bool verifiedPurchase,
        DateTime utcNow)
    {
        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            Rating = rating,
            Comment = comment,
            VerifiedPurchase = verifiedPurchase,
            Status = ProductReviewStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        review.AddDomainEvent(new ReviewSubmitted(
            review.Id,
            productId,
            customerId,
            rating,
            comment,
            verifiedPurchase));

        return review;
    }

    public Result Publish(Guid? moderatorId, DateTime utcNow)
    {
        if (Status != ProductReviewStatus.Pending)
        {
            return ProductReviewErrors.InvalidState;
        }

        Status = ProductReviewStatus.Published;
        ModeratorId = moderatorId;
        ModeratedAt = utcNow;
        UpdatedAt = utcNow;

        AddDomainEvent(new ReviewPublished(Id, ProductId, CustomerId, Rating, moderatorId));

        return Result.Success();
    }

    public Result Reject(Guid? moderatorId, string reason, DateTime utcNow)
    {
        if (Status != ProductReviewStatus.Pending)
        {
            return ProductReviewErrors.InvalidState;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return ProductReviewErrors.InvalidState;
        }

        Status = ProductReviewStatus.Rejected;
        ModeratorId = moderatorId;
        RejectionReason = reason;
        ModeratedAt = utcNow;
        UpdatedAt = utcNow;

        AddDomainEvent(new ReviewRejected(Id, ProductId, CustomerId, reason, moderatorId));

        return Result.Success();
    }

    public Result Remove(Guid? moderatorId, string reason, DateTime utcNow)
    {
        if (Status != ProductReviewStatus.Published)
        {
            return ProductReviewErrors.InvalidState;
        }

        Status = ProductReviewStatus.Removed;
        ModeratorId = moderatorId;
        RejectionReason = reason;
        ModeratedAt = utcNow;
        UpdatedAt = utcNow;

        AddDomainEvent(new ReviewRemoved(Id, ProductId, CustomerId, reason, moderatorId));

        return Result.Success();
    }
}
