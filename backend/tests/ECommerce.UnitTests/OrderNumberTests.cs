using ECommerce.Domain.Orders;

namespace ECommerce.UnitTests;

public sealed class OrderNumberTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1, "E-20260807-000001")]
    [InlineData(42, "E-20260807-000042")]
    [InlineData(999999, "E-20260807-999999")]
    [InlineData(1000000, "E-20260807-000000")]
    public void Create_Formats_Sequence_Zero_Padded(long sequence, string expected)
    {
        var orderNumber = OrderNumber.Create(Now, sequence);

        Assert.Equal(expected, orderNumber.Value);
    }

    [Fact]
    public void TryParse_Accepts_Valid_Order_Number()
    {
        Assert.True(OrderNumber.TryParse("E-20260807-000123", out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal("E-20260807-000123", parsed.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ORDER-123")]
    public void TryParse_Rejects_Invalid_Order_Number(string? value)
    {
        Assert.False(OrderNumber.TryParse(value, out var parsed));
        Assert.Null(parsed);
    }
}
