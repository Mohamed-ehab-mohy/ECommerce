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

    public Repository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<T>();
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
        return await ApplySpecification(spec).AsNoTracking().ToListAsync(ct);
    }

    public async Task<T?> GetOneAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).CountAsync(ct);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).AnyAsync(ct);
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

    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        return SpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), spec);
    }
}
