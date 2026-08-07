using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Orders;

public static class CheckoutErrors
{
    public static readonly Error CheckoutNotFound = new(
        "ERR_RES_001",
        "The checkout was not found.",
        ErrorType.NotFound);

    public static readonly Error CheckoutExpired = new(
        "ERR_CHK_001",
        "The checkout has expired. Please start a new checkout.",
        ErrorType.Conflict);

    public static readonly Error InvalidState = new(
        "ERR_CHK_002",
        "The checkout state does not allow this operation.",
        ErrorType.Conflict);

    public static readonly Error CartEmpty = new(
        "ERR_CHK_003",
        "The cart is empty. Add items before checking out.",
        ErrorType.Conflict);

    public static readonly Error ShippingMethodUnsupported = new(
        "ERR_CHK_004",
        "The shipping method is not available for this destination.",
        ErrorType.BadRequest);

    public static Error InsufficientStock(IReadOnlyList<StockShortageLine> lines) =>
        new Error(
            "ERR_STK_001",
            "Insufficient stock to complete the order.",
            ErrorType.Conflict).With(new Dictionary<string, object?>
            {
                ["lines"] = lines.Select(line => new Dictionary<string, object?>
                {
                    ["sku"] = line.Sku,
                    ["requested"] = line.Requested,
                    ["available"] = line.Available
                }).ToList()
            });
}

public sealed record StockShortageLine(string Sku, int Requested, int Available);
