using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Reviews.Handlers;
using ECommerce.UseCases.Reviews.Queries;

namespace ECommerce.UnitTests;

public sealed class GetModerationQueueQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductReviewRepository _reviews = new();

    private GetModerationQueueQueryHandler CreateHandler() =>
        new(_reviews, new GetModerationQueueQueryValidator());

    [Fact]
    public async Task Handle_Returns_Pending_Reviews_Oldest_First()
    {
        var first = ProductReview.Create(Guid.NewGuid(), Guid.NewGuid(), 4, "One.", true, UtcNow);
        var second = ProductReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            "Two.",
            false,
            UtcNow.AddMinutes(5));
        second.Publish(Guid.NewGuid(), UtcNow.AddMinutes(10));
        _reviews.Add(first);
        _reviews.Add(second);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetModerationQueueQuery(1, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(first.Id, item.ReviewId);
        Assert.Equal(1, result.Value.Total);
        Assert.True(item.VerifiedPurchase);
    }

    [Fact]
    public async Task Handle_Empty_Queue_Returns_Empty_Page()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetModerationQueueQuery(1, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.Total);
    }
}
