using ECommerce.Domain.Inventory;

namespace ECommerce.Domain.Exceptions;

public sealed class StockBalanceException : DomainException
{
    public StockBalanceException(Error error)
        : base(error)
    {
    }
}
