using ECommerce.Domain.Catalog;
using Xunit;

namespace ECommerce.UnitTests.Tests.Contract;

public sealed class ProductVariantContractTests
{
    [Fact]
    public void ProductVariant_CreatesWithCorrectValues()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), "SKU-V1-RED", "Red Large", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, variant.Id);
        Assert.Equal("SKU-V1-RED", variant.Sku);
        Assert.Equal("Red Large", variant.Name);
    }

    [Fact]
    public void ProductVariant_HasEmptyAttributes()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), "SKU-V2", "Blue", DateTime.UtcNow);

        Assert.NotNull(variant.Attributes);
        Assert.Empty(variant.Attributes);
    }
}
