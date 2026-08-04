using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Pricing;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UseCases.Cart.Responses;

public sealed record CartItemResponse(
    Guid ProductId,
    string Sku,
    string Name,
    string? ImageUrl,
    decimal ListPrice,
    decimal UnitPrice,
    int Quantity,
    decimal LineSubtotal,
    decimal LineDiscount);

public sealed record CartTotalsResponse(
    decimal Subtotal,
    decimal ItemDiscount,
    decimal Shipping,
    decimal Tax,
    decimal Total);

public sealed record CartResponse(
    Guid Id,
    string Currency,
    long Version,
    DateTime ExpiresAt,
    DateTime UpdatedAt,
    IReadOnlyList<CartItemResponse> Items,
    CartTotalsResponse Totals);

public static class CartTotalsCalculator
{
    private static readonly decimal FlatShippingUsd = 9.90m;

    private static readonly decimal TaxRate = 0.05m;

    public static CartTotalsResponse Compute(CartAggregate cart, ICurrencyCatalog currencies)
    {
        var subtotal = 0m;
        var itemDiscount = 0m;

        foreach (var item in cart.Items)
        {
            subtotal += Money.From(item.ListPrice, cart.Currency).Amount * item.Quantity;
            itemDiscount += (Money.From(item.ListPrice, cart.Currency).Amount - Money.From(item.UnitPrice, cart.Currency).Amount) * item.Quantity;
        }

        var shipping = FlatShippingUsd == 0m || cart.Currency == "USD"
            ? Money.From(FlatShippingUsd, cart.Currency).Amount
            : Money.From(FlatShippingUsd, "USD").ConvertTo(cart.Currency, currencies.GetRate("USD", cart.Currency)).Amount;

        var tax = Money.From((subtotal - itemDiscount) * TaxRate, cart.Currency).Amount;
        var total = subtotal - itemDiscount + shipping + tax;

        return new CartTotalsResponse(
            Money.From(subtotal, cart.Currency).DisplayAmount,
            Money.From(itemDiscount, cart.Currency).DisplayAmount,
            Money.From(shipping, cart.Currency).DisplayAmount,
            Money.From(tax, cart.Currency).DisplayAmount,
            Money.From(total, cart.Currency).DisplayAmount);
    }
}

public static class CartResponseFactory
{
    public static CartResponse From(CartAggregate cart, ICurrencyCatalog currencies)
    {
        var items = cart.Items
            .Select(item => new CartItemResponse(
                item.ProductId,
                item.Sku,
                item.Name,
                item.ImageUrl,
                item.ListPrice,
                item.UnitPrice,
                item.Quantity,
                Money.From(item.ListPrice * item.Quantity, cart.Currency).DisplayAmount,
                Money.From((item.ListPrice - item.UnitPrice) * item.Quantity, cart.Currency).DisplayAmount))
            .ToList();

        return new CartResponse(
            cart.Id,
            cart.Currency,
            cart.Version,
            cart.ExpiresAt,
            cart.UpdatedAt,
            items,
            CartTotalsCalculator.Compute(cart, currencies));
    }
}

