using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Ports;

public interface IAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerAddress?> GetByIdAndCustomerIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken);

    void Add(CustomerAddress address);

    void Remove(CustomerAddress address);
}
