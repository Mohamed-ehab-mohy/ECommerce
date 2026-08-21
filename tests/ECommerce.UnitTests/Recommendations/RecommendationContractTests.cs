using ECommerce.UseCases.Recommendations;

namespace ECommerce.UnitTests.Tests.Recommendations;

public sealed class RecommendationContractTests
{
    [Fact]
    public void ProductRecommendation_HasRequiredFields()
    {
        var recommendation = new ProductRecommendation(
            Guid.NewGuid(),
            "SKU-001",
            "Test Product",
            29.99m,
            0.85m,
            "collaborative-filtering");

        Assert.NotEqual(Guid.Empty, recommendation.ProductId);
        Assert.Equal("SKU-001", recommendation.Sku);
        Assert.Equal("Test Product", recommendation.Name);
        Assert.Equal(29.99m, recommendation.Price);
        Assert.Equal(0.85m, recommendation.Score);
        Assert.Equal("collaborative-filtering", recommendation.Reason);
    }

    [Fact]
    public void ProductRecommendation_Score_IsBetweenZeroAndOne()
    {
        var recommendation = new ProductRecommendation(
            Guid.NewGuid(),
            "SKU-001",
            "Product",
            10m,
            1.0m,
            "trending");

        Assert.True(recommendation.Score is >= 0 and <= 1);
    }

    [Fact]
    public void ProductRecommendation_Reason_IsKnownType()
    {
        var reasons = new[] { "collaborative-filtering", "frequently-bought-together", "trending" };

        foreach (var reason in reasons)
        {
            var recommendation = new ProductRecommendation(
                Guid.NewGuid(),
                "SKU",
                "Product",
                10m,
                0.5m,
                reason);

            Assert.Contains(recommendation.Reason, reasons);
        }
    }

    [Fact]
    public void ProductRecommendation_RecordEquality_Works()
    {
        var id = Guid.NewGuid();
        var r1 = new ProductRecommendation(id, "SKU", "Product", 10m, 0.5m, "trending");
        var r2 = new ProductRecommendation(id, "SKU", "Product", 10m, 0.5m, "trending");

        Assert.Equal(r1, r2);
    }

    [Fact]
    public void ProductRecommendation_RecordInequality_Works()
    {
        var r1 = new ProductRecommendation(Guid.NewGuid(), "SKU-A", "Product A", 10m, 0.5m, "trending");
        var r2 = new ProductRecommendation(Guid.NewGuid(), "SKU-B", "Product B", 20m, 0.8m, "collaborative-filtering");

        Assert.NotEqual(r1, r2);
    }

    [Fact]
    public void ProductRecommendation_ZeroScore_IsValid()
    {
        var recommendation = new ProductRecommendation(
            Guid.NewGuid(),
            "SKU",
            "Product",
            0m,
            0.0m,
            "trending");

        Assert.Equal(0.0m, recommendation.Score);
    }
}
