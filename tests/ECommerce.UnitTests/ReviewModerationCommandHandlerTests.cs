using ECommerce.Domain.Audit;
using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Handlers;

namespace ECommerce.UnitTests;

public sealed class ReviewModerationCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid ModeratorId = Guid.NewGuid();

    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly FakeProductReviewRepository _reviews = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeAuditLogWriter _audit = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static ProductReview PendingReview() =>
        ProductReview.Create(ProductId, Guid.NewGuid(), 4, "Nice.", true, UtcNow);

    [Fact]
    public async Task Publish_Approves_And_Audits()
    {
        _reviews.Add(PendingReview());
        var handler = new PublishReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new PublishReviewCommandValidator());
        var review = _reviews.Reviews[0];

        var result = await handler.Handle(new PublishReviewCommand(review.Id, ModeratorId), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ProductReviewStatus.Published, _reviews.Reviews[0].Status);
        Assert.Equal("Published", result.Value.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations, op => op.Action == AuditActions.ReviewModerated);
    }

    [Fact]
    public async Task Publish_Non_Pending_Review_Is_Rejected()
    {
        var review = PendingReview();
        review.Publish(ModeratorId, UtcNow);
        _reviews.Add(review);
        var handler = new PublishReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new PublishReviewCommandValidator());

        var result = await handler.Handle(new PublishReviewCommand(review.Id, ModeratorId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.InvalidState, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Publish_Unknown_Review_Is_Rejected()
    {
        var handler = new PublishReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new PublishReviewCommandValidator());

        var result = await handler.Handle(new PublishReviewCommand(Guid.NewGuid(), ModeratorId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.ReviewNotFound, result.Error);
    }

    [Fact]
    public async Task Reject_Records_Reason_And_Audits()
    {
        _reviews.Add(PendingReview());
        var handler = new RejectReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new RejectReviewCommandValidator());
        var review = _reviews.Reviews[0];

        var result = await handler.Handle(
            new RejectReviewCommand(review.Id, ModeratorId, "Inappropriate language."),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ProductReviewStatus.Rejected, _reviews.Reviews[0].Status);
        Assert.Equal("Inappropriate language.", _reviews.Reviews[0].RejectionReason);
        Assert.Single(_audit.Operations, op => op.Action == AuditActions.ReviewModerated);
    }

    [Fact]
    public async Task Reject_Without_Reason_Is_Rejected_By_Validation()
    {
        _reviews.Add(PendingReview());
        var handler = new RejectReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new RejectReviewCommandValidator());

        var result = await handler.Handle(
            new RejectReviewCommand(_reviews.Reviews[0].Id, ModeratorId, string.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewStatus.Pending, _reviews.Reviews[0].Status);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Remove_Published_Review_Reaggregates()
    {
        var review = PendingReview();
        review.Publish(ModeratorId, UtcNow);
        _reviews.Add(review);
        var handler = new RemoveReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new RemoveReviewCommandValidator());

        var result = await handler.Handle(
            new RemoveReviewCommand(review.Id, ModeratorId, "Compliance removal."),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ProductReviewStatus.Removed, _reviews.Reviews[0].Status);
        Assert.Single(_audit.Operations, op => op.Action == AuditActions.ReviewRemovedAction);
    }

    [Fact]
    public async Task Remove_Non_Published_Review_Is_Rejected()
    {
        _reviews.Add(PendingReview());
        var handler = new RemoveReviewCommandHandler(
            _reviews,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new RemoveReviewCommandValidator());

        var result = await handler.Handle(
            new RemoveReviewCommand(_reviews.Reviews[0].Id, ModeratorId, "Compliance removal."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.InvalidState, result.Error);
    }
}
