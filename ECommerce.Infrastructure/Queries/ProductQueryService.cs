using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Queries;

public sealed class ProductQueryService : IProductQueryService
{
    private readonly StoreDbContext _dbContext;

    public ProductQueryService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GetAllProductsResponse>> GetAllProductsAsync(CancellationToken ct = default)
    {
        return await _dbContext.Products
            .Where(p => !p.IsDeleted)
            .ProjectToType<GetAllProductsResponse>()
            .ToListAsync(ct);
    }

    public async Task<GetByIdProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Products
            .Where(p => !p.IsDeleted && p.Id == id)
            .ProjectToType<GetByIdProductResponse>()
            .FirstOrDefaultAsync(ct);
    }
}
