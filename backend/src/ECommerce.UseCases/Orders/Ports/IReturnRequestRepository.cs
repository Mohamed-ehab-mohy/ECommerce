using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Orders.Ports;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReturnRequest>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReturnRequest>> ListPendingAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountPendingAsync(CancellationToken cancellationToken);
    void Add(ReturnRequest returnRequest);
}
