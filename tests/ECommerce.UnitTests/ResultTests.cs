using ECommerce.Domain.Errors;
using ECommerce.Domain.Primitives;

namespace ECommerce.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_Returns_SuccessResult_With_No_Error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_Returns_FailedResult_With_Error()
    {
        var error = new Error("Order.NotFound", "Order was not found", ErrorType.NotFound);
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Implicit_Conversion_From_Error_Creates_Failed_Result()
    {
        var error = new Error("Order.Conflict", "Order is already paid", ErrorType.Conflict);
        Result result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_Success_Returns_Value()
    {
        var result = Result<string>.Success("shipped");

        Assert.True(result.IsSuccess);
        Assert.Equal("shipped", result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Generic_Implicit_Conversion_From_Value_Creates_Success()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_Implicit_Conversion_From_Error_Creates_Failure()
    {
        var error = new Error("Stock.Insufficient", "Not enough stock", ErrorType.Conflict);
        Result<int> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_Failure_Returns_Error_And_Default_Value()
    {
        var error = new Error("Payment.Declined", "Card was declined", ErrorType.Validation);
        var result = Result<Guid>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Equal(Guid.Empty, result.Value);
    }
}
