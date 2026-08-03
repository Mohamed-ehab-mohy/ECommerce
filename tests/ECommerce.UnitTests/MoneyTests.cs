using ECommerce.Domain.Pricing;

namespace ECommerce.UnitTests;

public sealed class MoneyTests
{
    public static TheoryData<decimal, decimal> FromRoundingCases => new()
    {
        { 1.23456m, 1.2346m },
        { 1.23454m, 1.2345m },
        { -1.23455m, -1.2346m },
        { 1.23445m, 1.2345m }
    };

    public static TheoryData<decimal, decimal> DisplayRoundingCases => new()
    {
        { 1.2345m, 1.23m },
        { 1.235m, 1.24m },
        { -1.235m, -1.24m },
        { 2.005m, 2.01m }
    };

    [Theory]
    [MemberData(nameof(FromRoundingCases))]
    public void From_Rounds_To_Four_Decimal_Places_Half_Away_From_Zero(decimal input, decimal expected)
    {
        var money = Money.From(input, "USD");

        Assert.Equal(expected, money.Amount);
        Assert.Equal(4, ScaleOf(money.Amount));
    }

    [Theory]
    [MemberData(nameof(DisplayRoundingCases))]
    public void DisplayAmount_Rounds_To_Two_Decimal_Places_Half_Away_From_Zero(decimal input, decimal expected)
    {
        var money = Money.From(input, "USD");

        Assert.Equal(expected, money.DisplayAmount);
        Assert.Equal(2, ScaleOf(money.DisplayAmount));
    }

    [Fact]
    public void From_UpperCases_And_Trims_Currency()
    {
        var money = Money.From(10m, "  aed ");

        Assert.Equal("AED", money.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void From_Throws_When_Currency_Is_Missing(string? currency)
    {
        Assert.Throws<ArgumentException>(() => Money.From(10m, currency!));
    }

    [Fact]
    public void ConvertTo_Multiplies_By_Rate_And_Rounds_To_Four_Decimal_Places()
    {
        var money = Money.From(99.99m, "USD");

        var converted = money.ConvertTo("AED", 3.6725m);

        Assert.Equal("AED", converted.Currency);
        Assert.Equal(367.2133m, converted.Amount);
        Assert.Equal(367.21m, converted.DisplayAmount);
    }

    [Fact]
    public void ConvertTo_Same_Currency_With_Identity_Rate_Keeps_Amount()
    {
        var money = Money.From(10.5m, "USD");

        var converted = money.ConvertTo("USD", 1m);

        Assert.Equal("USD", converted.Currency);
        Assert.Equal(10.5m, converted.Amount);
    }

    [Fact]
    public void Money_Equality_Is_By_Value()
    {
        Assert.Equal(Money.From(10m, "USD"), Money.From(10m, "USD"));
        Assert.NotEqual(Money.From(10m, "USD"), Money.From(10m, "AED"));
        Assert.NotEqual(Money.From(10m, "USD"), Money.From(10.5m, "USD"));
    }

    private static int ScaleOf(decimal value) => BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
}
