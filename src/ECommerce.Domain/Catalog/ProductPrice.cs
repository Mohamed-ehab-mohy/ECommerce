namespace ECommerce.Domain.Catalog;

public sealed class ProductPrice
{
    private ProductPrice()
    {
        Currency = string.Empty;
    }

    public Guid ProductId { get; private set; }

    public string Currency { get; private set; }

    public decimal ListAmount { get; private set; }

    public decimal? OfferAmount { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static ProductPrice Create(
        Guid productId,
        string currency,
        decimal listAmount,
        decimal? offerAmount,
        DateTime utcNow)
    {
        return new ProductPrice
        {
            ProductId = productId,
            Currency = currency,
            ListAmount = listAmount,
            OfferAmount = offerAmount,
            UpdatedAt = utcNow
        };
    }

    public void Update(decimal listAmount, decimal? offerAmount, DateTime utcNow)
    {
        ListAmount = listAmount;
        OfferAmount = offerAmount;
        UpdatedAt = utcNow;
    }
}
