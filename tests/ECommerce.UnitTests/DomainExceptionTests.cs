using ECommerce.Domain.Errors;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests;

public sealed class DomainExceptionTests
{
    private sealed class TestException : DomainException
    {
        public TestException(Error error)
            : base(error)
        {
        }
    }

    [Fact]
    public void Exception_Carries_Error_And_Description()
    {
        var error = new Error("Stock.Insufficient", "Not enough stock", ErrorType.Conflict);
        var exception = new TestException(error);

        Assert.Equal(error, exception.Error);
        Assert.Equal(error.Description, exception.Message);
    }
}
