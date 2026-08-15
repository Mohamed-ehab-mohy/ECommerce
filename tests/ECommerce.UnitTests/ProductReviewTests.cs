using ECommerce.Domain.Events;
using ECommerce.Domain.Reviews;

namespace ECommerce.UnitTests;

public sealed class ProductReviewTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid ProductId = Guid.NewGuid();

    private static readonly Guid CustomerId = Guid.NewGuid();

    private static readonly Guid ModeratorId = Guid.NewGuid();

    private static ProductReview CreateReview() =>
        ProductReview.Create(ProductId, CustomerId, 4, "Great product.", verifiedPurchase: true, UtcNow);

    [Fact]
    public void Create_Queues_For_Moderation_With_Verified_Purchase_Flag()
    {
        var review = CreateReview();

        Assert.Equal(ProductId, review.ProductId);
        Assert.Equal(CustomerId, review.CustomerId);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Great product.", review.Comment);
        Assert.True(review.VerifiedPurchase);
        Assert.Equal(ProductReviewStatus.Pending, review.Status);
        Assert.Single(review.DomainEvents.OfType<ReviewSubmitted>());
    }

    [Fact]
    public void Publish_Approved_Review_Is_Published()
    {
        var review = CreateReview();

        var result = review.Publish(ModeratorId, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductReviewStatus.Published, review.Status);
        Assert.Equal(ModeratorId, review.ModeratorId);
        Assert.Equal(UtcNow, review.ModeratedAt);
        Assert.Single(review.DomainEvents.OfType<ReviewPublished>());
    }

    [Fact]
    public void Publish_Only_Works_For_Pending_Reviews()
    {
        var review = CreateReview();
        review.Publish(ModeratorId, UtcNow);

        var result = review.Publish(ModeratorId, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.InvalidState, result.Error);
    }

    [Fact]
    public void Reject_Records_Reason()
    {
        var review = CreateReview();

        var result = review.Reject(ModeratorId, "Inappropriate language.", UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductReviewStatus.Rejected, review.Status);
        Assert.Equal("Inappropriate language.", review.RejectionReason);
        Assert.Single(review.DomainEvents.OfType<ReviewRejected>());
    }

    [Fact]
    public void Reject_Requires_A_Reason()
    {
        var review = CreateReview();

        var result = review.Reject(ModeratorId, "  ", UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewStatus.Pending, review.Status);
    }

    [Fact]
    public void Remove_Only_Works_For_Published_Reviews()
    {
        var pending = CreateReview();
        Assert.True(pending.Remove(ModeratorId, "Compliance removal.", UtcNow).IsFailure);
        Assert.Equal(ProductReviewStatus.Pending, pending.Status);

        var rejected = CreateReview();
        rejected.Reject(ModeratorId, "Spam.", UtcNow);
        Assert.True(rejected.Remove(ModeratorId, "Compliance removal.", UtcNow).IsFailure);

        var published = CreateReview();
        published.Publish(ModeratorId, UtcNow);

        var result = published.Remove(ModeratorId, "Compliance removal.", UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductReviewStatus.Removed, published.Status);
        Assert.Single(published.DomainEvents.OfType<ReviewRemoved>());
    }
}
