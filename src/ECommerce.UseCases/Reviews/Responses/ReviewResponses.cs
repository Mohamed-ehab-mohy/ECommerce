namespace ECommerce.UseCases.Reviews.Responses;

public sealed record SubmitReviewResponse(
    Guid ReviewId,
    Guid ProductId,
    string Status,
    bool VerifiedPurchase);

public sealed record ProductReviewResponse(
    Guid ReviewId,
    Guid ProductId,
    int Rating,
    string Comment,
    bool VerifiedPurchase,
    DateTime? PublishedAtUtc,
    int HelpfulVotes);

public sealed record ProductReviewsResponse(
    Guid ProductId,
    decimal RatingAverage,
    int RatingCount,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<ProductReviewResponse> Items);

public sealed record ModerationReviewResponse(
    Guid ReviewId,
    Guid ProductId,
    int Rating,
    string Comment,
    bool VerifiedPurchase,
    Guid CustomerId,
    DateTime SubmittedAtUtc);

public sealed record ModerationQueueResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<ModerationReviewResponse> Items);

public sealed record ReviewModerationResponse(
    Guid ReviewId,
    string Status,
    Guid? ModeratorId,
    DateTime? ModeratedAtUtc);

public sealed record VoteReviewResponse(Guid ReviewId, int HelpfulVotes);
