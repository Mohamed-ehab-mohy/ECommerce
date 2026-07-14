using System.Collections.Concurrent;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.Specifications;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly StoreDbContext _dbContext;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;
    private readonly ISpecificationEvaluator _evaluator;
    private readonly ConcurrentDictionary<Type, object> _repos = new();
    private IDbContextTransaction? _transaction;

    public UnitOfWork(StoreDbContext dbContext, SoftDeleteInterceptor softDeleteInterceptor, ISpecificationEvaluator evaluator)
    {
        _dbContext = dbContext;
        _softDeleteInterceptor = softDeleteInterceptor;
        _evaluator = evaluator;
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);

        if (_repos.TryGetValue(type, out var repo))
        {
            return (IRepository<T>)repo;
        }

        var newRepo = new Repository<T>(_dbContext, _evaluator);

        _repos.TryAdd(type, newRepo);

        return newRepo;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        _softDeleteInterceptor.ApplySoftDelete(_dbContext);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(ct);

        _transaction?.Dispose();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(ct);

        _transaction?.Dispose();
        _transaction = null;
    }
}
