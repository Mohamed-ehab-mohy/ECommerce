using ECommerce.Domain.Entities;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.Specifications;
using ECommerce.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly StoreDbContext _dbContext;
    private readonly DbSet<T> _dbSet;
    private readonly ISpecificationEvaluator _evaluator;

    public Repository(StoreDbContext dbContext, ISpecificationEvaluator evaluator)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<T>();
        _evaluator = evaluator;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await _evaluator.GetQuery(_dbSet.AsQueryable(), spec).ToListAsync(ct);
    }

    public async Task<T?> GetOneAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await _evaluator.GetQuery(_dbSet.AsQueryable(), spec).FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await _evaluator.GetQuery(_dbSet.AsQueryable(), spec).CountAsync(ct);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await _evaluator.GetQuery(_dbSet.AsQueryable(), spec).AnyAsync(ct);
    }

    public void Add(T entity)
    {
        _dbSet.Add(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}
