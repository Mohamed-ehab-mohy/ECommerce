using System.Collections.Concurrent;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Repositories;
using ECommerce.Infrastructure.Data.DbContexts;

namespace ECommerce.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly StoreDbContext _dbContext;
    private readonly ConcurrentDictionary<string, object> _repositories = new();

    public UnitOfWork(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var typeName = typeof(T).Name;

        return (IRepository<T>)_repositories.GetOrAdd(typeName, _ => new Repository<T>(_dbContext));
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }
}