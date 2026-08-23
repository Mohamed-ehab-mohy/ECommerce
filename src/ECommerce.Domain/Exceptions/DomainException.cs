
namespace ECommerce.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(Error error)
        : base(error.Description)
    {
        Error = error;
    }

    public Error Error { get; }
}
