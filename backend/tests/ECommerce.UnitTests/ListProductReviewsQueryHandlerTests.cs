using ECommerce.Domain.Catalog;
using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Reviews.Handlers;
using ECommerce.UseCases.Reviews.Queries;

namespace ECommerce.UnitTests;

public sealed class ListProductReviewsQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid ModeratorId = Guid.NewGuid();

    private readonly FakeProductRepository _products = new();

    private readonly FakeProductReviewRepository _reviews = new();

    private readonly FakeReviewVoteRepository _votes = new();

    private ListProductReviewsQueryHandler CreateHandler() =>
        new(_products, _reviews, _votes, new ListProductReviewsQueryValidator());

    private static Product CreateActiveProduct() =>
        Product.Create(
            "SKU-1",
            "widget",
            "en",
            "Widget",
            null,
            "USD",
            15.00m,
            null,
            null,
            null,
            isFeatured: false,
            ProductStatus.Active,
            UtcNow);

    private void AddPublishedReview(Guid productId, int rating, Guid customerId)
    {
        var review = ProductReview.Create(productId, customerId, rating, $"Comment {rating}.", true, UtcNow);
        review.Publish(ModeratorId, UtcNow);
        _reviews.Add(review);
    }

    [Fact]
    public async Task Handle_Returns_Published_Reviews_And_Aggregated_Rating()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        AddPublishedReview(product.Id, 4, Guid.NewGuid());
        AddPublishedReview(product.Id, 5, Guid.NewGuid());
        _reviews.Add(ProductReview.Create(product.Id, Guid.NewGuid(), 1, "Pending.", true, UtcNow));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ListProductReviewsQuery(product.Id, 1, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.RatingCount);
        Assert.Equal(4.50m, result.Value.RatingAverage);
        Assert.All(result.Value.Items, item => Assert.True(item.VerifiedPurchase));
    }

    [Fact]
    public async Task Handle_No_Reviews_Returns_Zero_Rating()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ListProductReviewsQuery(product.Id, 1, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.RatingCount);
        Assert.Equal(0m, result.Value.RatingAverage);
    }

    [Fact]
    public async Task Handle_Unknown_Or_Deactivated_Product_Is_Rejected()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ListProductReviewsQuery(Guid.NewGuid(), 1, 20),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Pages_Published_Reviews()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        for (var i = 0; i < 3; i++)
        {
            AddPublishedReview(product.Id, 5, Guid.NewGuid());
        }

        var handler = CreateHandler();

        var firstPage = await handler.Handle(
            new ListProductReviewsQuery(product.Id, 1, 2),
            CancellationToken.None);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(2, firstPage.Value.Items.Count);
        Assert.Equal(3, firstPage.Value.Total);

        var secondPage = await handler.Handle(
            new ListProductReviewsQuery(product.Id, 2, 2),
            CancellationToken.None);

        Assert.True(secondPage.IsSuccess);
        Assert.Single(secondPage.Value.Items);
    }
}
