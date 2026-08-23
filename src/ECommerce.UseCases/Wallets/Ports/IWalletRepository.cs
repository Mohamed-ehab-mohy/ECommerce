using ECommerce.Domain.Wallets;

namespace ECommerce.UseCases.Wallets.Ports;

public interface IWalletRepository
{
    Task<Wallet?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByCustomerIdAsNoTrackingAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
}
