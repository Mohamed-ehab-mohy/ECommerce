using ECommerce.Domain.Identity;
using ECommerce.Domain.Reviews;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Handlers;

namespace ECommerce.UnitTests;

public sealed class VoteReviewCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid ModeratorId = Guid.NewGuid();

    private readonly FakeProductReviewRepository _reviews = new();

    private readonly FakeReviewVoteRepository _votes = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private VoteReviewCommandHandler CreateHandler() =>
        new(
            _reviews,
            _votes,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new VoteReviewCommandValidator());

    private ProductReview PublishedReview(Guid customerId)
    {
        var review = ProductReview.Create(Guid.NewGuid(), customerId, 5, "Nice.", true, UtcNow);
        review.Publish(ModeratorId, UtcNow);
        _reviews.Add(review);
        return review;
    }

    [Fact]
    public async Task Handle_Records_Helpful_Vote()
    {
        var customer = Guid.NewGuid();
        var review = PublishedReview(Guid.NewGuid());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new VoteReviewCommand(review.Id, customer, helpful: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(1, result.Value.HelpfulVotes);
        var vote = Assert.Single(_votes.Votes);
        Assert.Equal(ReviewVoteValue.Helpful, vote.Value);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_One_Vote_Per_Customer_Per_Review()
    {
        var customer = Guid.NewGuid();
        var review = PublishedReview(Guid.NewGuid());
        var handler = CreateHandler();

        await handler.Handle(new VoteReviewCommand(review.Id, customer, helpful: true), CancellationToken.None);
        var result = await handler.Handle(new VoteReviewCommand(review.Id, customer, helpful: true), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Single(_votes.Votes);
        Assert.Equal(1, result.Value.HelpfulVotes);
    }

    [Fact]
    public async Task Handle_Changing_Vote_Updates_Value()
    {
        var customer = Guid.NewGuid();
        var review = PublishedReview(Guid.NewGuid());
        var handler = CreateHandler();

        await handler.Handle(new VoteReviewCommand(review.Id, customer, helpful: true), CancellationToken.None);
        var result = await handler.Handle(new VoteReviewCommand(review.Id, customer, helpful: false), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var vote = Assert.Single(_votes.Votes);
        Assert.Equal(ReviewVoteValue.NotHelpful, vote.Value);
        Assert.Equal(0, result.Value.HelpfulVotes);
    }

    [Fact]
    public async Task Handle_Pending_Review_Cannot_Be_Voted_On()
    {
        var review = ProductReview.Create(Guid.NewGuid(), Guid.NewGuid(), 4, "Nice.", true, UtcNow);
        _reviews.Add(review);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new VoteReviewCommand(review.Id, Guid.NewGuid(), helpful: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.ReviewNotPublished, result.Error);
        Assert.Empty(_votes.Votes);
    }

    [Fact]
    public async Task Handle_Unauthenticated_Caller_Is_Rejected()
    {
        var review = PublishedReview(Guid.NewGuid());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new VoteReviewCommand(review.Id, Guid.Empty, helpful: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.NotAuthenticated, result.Error);
    }

    [Fact]
    public void Moderation_Commands_Require_Reviews_Moderate_Permission()
    {
        Assert.Equal(Permissions.ReviewsModerate, new PublishReviewCommand(Guid.NewGuid(), null).Permission);
        Assert.Equal(Permissions.ReviewsModerate, new RejectReviewCommand(Guid.NewGuid(), null, "Spam.").Permission);
        Assert.Equal(Permissions.ReviewsModerate, new RemoveReviewCommand(Guid.NewGuid(), null, "Abuse.").Permission);
    }
}
