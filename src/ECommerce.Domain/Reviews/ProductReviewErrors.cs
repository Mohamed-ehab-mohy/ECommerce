
namespace ECommerce.Domain.Reviews;

public static class ProductReviewErrors
{
    public static readonly Error ProductNotFound = new(
        "Reviews.ProductNotFound",
        "The product was not found.",
        ErrorType.NotFound);

    public static readonly Error ReviewNotFound = new(
        "Reviews.ReviewNotFound",
        "The review was not found.",
        ErrorType.NotFound);

    public static readonly Error ProductNotPurchased = new(
        "Reviews.ProductNotPurchased",
        "Only verified purchases can be reviewed.",
        ErrorType.Forbidden);

    public static readonly Error AlreadyReviewed = new(
        "Reviews.AlreadyReviewed",
        "You have already reviewed this product.",
        ErrorType.Conflict);

    public static readonly Error InvalidState = new(
        "Reviews.InvalidState",
        "The review cannot transition to the requested state.",
        ErrorType.Conflict);

    public static readonly Error ReviewNotPublished = new(
        "Reviews.ReviewNotPublished",
        "Only published reviews can be voted on.",
        ErrorType.Conflict);
}
