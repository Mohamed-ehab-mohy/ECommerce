using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Common;

public sealed class UnitOfWork(ECommerceDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        IDbContextTransaction? transaction = null;

        await strategy.ExecuteAsync(async () =>
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        });

        return new DbContextTransaction(dbContext, transaction!);
    }

    private sealed class DbContextTransaction(ECommerceDbContext dbContext, IDbContextTransaction transaction) : ITransaction
    {
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await transaction.CommitAsync(cancellationToken);
            });
        }

        public Task RollbackAsync(CancellationToken cancellationToken) =>
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
