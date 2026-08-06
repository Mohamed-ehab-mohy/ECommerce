using ECommerce.Domain.Inventory;
using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Exceptions;

public sealed class StockBalanceException : DomainException
{
    public StockBalanceException(Error error)
        : base(error)
    {
    }
}
