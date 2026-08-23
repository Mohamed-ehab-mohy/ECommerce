using ECommerce.Domain.Wallets;
using ECommerce.UseCases.Wallets.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data.Repositories;

public sealed class WalletRepository(ECommerceDbContext dbContext) : IWalletRepository
{
    public Task<Wallet?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Wallets
            .FirstOrDefaultAsync(w => w.CustomerId == customerId, cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await dbContext.Wallets.AddAsync(wallet, cancellationToken);
    }
}
