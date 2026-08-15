using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Identity;
using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Handlers;

namespace ECommerce.UnitTests;

public sealed class SubmitReviewCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly FakeProductRepository _products = new();

    private readonly FakeProductReviewRepository _reviews = new();

    private readonly FakeVerifiedPurchaseChecker _purchases = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeAuditLogWriter _audit = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private SubmitReviewCommandHandler CreateHandler() =>
        new(
            _products,
            _reviews,
            _purchases,
            _unitOfWork,
            _audit,
            new FixedTimeProvider(UtcNow),
            new SubmitReviewCommandValidator());

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

    private static SubmitReviewCommand Command(Guid? productId = null, int rating = 5) =>
        new(productId ?? Guid.NewGuid(), CustomerId, rating, "Great product.");

    [Fact]
    public async Task Handle_Verified_Purchase_Queues_For_Moderation()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        _purchases.Purchases.Add((CustomerId, product.Id));
        var handler = CreateHandler();

        var result = await handler.Handle(Command(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var review = Assert.Single(_reviews.Reviews);
        Assert.Equal(ProductReviewStatus.Pending, review.Status);
        Assert.True(review.VerifiedPurchase);
        Assert.Equal(result.Value.ReviewId, review.Id);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations, op => op.Action == AuditActions.ReviewSubmitted);
    }

    [Fact]
    public async Task Handle_Non_Purchaser_Is_Rejected()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(product.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.ProductNotPurchased, result.Error);
        Assert.Empty(_reviews.Reviews);
    }

    [Fact]
    public async Task Handle_Duplicate_Review_Is_Rejected()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        _purchases.Purchases.Add((CustomerId, product.Id));
        _reviews.Add(ProductReview.Create(product.Id, CustomerId, 5, "Already reviewed.", true, UtcNow));
        var handler = CreateHandler();

        var result = await handler.Handle(Command(product.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.AlreadyReviewed, result.Error);
        Assert.Single(_reviews.Reviews);
    }

    [Fact]
    public async Task Handle_Unknown_Product_Is_Rejected()
    {
        _purchases.Purchases.Add((CustomerId, Guid.NewGuid()));
        var handler = CreateHandler();

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Deactivated_Product_Is_Hidden()
    {
        var product = CreateActiveProduct();
        product.Deactivate();
        _products.Add(product);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(product.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductReviewErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Unauthenticated_Caller_Is_Rejected()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new SubmitReviewCommand(product.Id, Guid.Empty, 5, "Great product."),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.NotAuthenticated, result.Error);
    }

    [Fact]
    public async Task Handle_Invalid_Rating_Is_Rejected()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        _purchases.Purchases.Add((CustomerId, product.Id));
        var handler = CreateHandler();

        var result = await handler.Handle(Command(product.Id, rating: 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_reviews.Reviews);
    }
}
