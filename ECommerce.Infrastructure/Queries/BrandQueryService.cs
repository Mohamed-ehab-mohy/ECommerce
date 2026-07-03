using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Queries;

public sealed class BrandQueryService : IBrandQueryService
{
    private readonly StoreDbContext _dbContext;

    public BrandQueryService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GetAllBrandsResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.ProductBrands
            .Where(b => !b.IsDeleted)
            .ProjectToType<GetAllBrandsResponse>()
            .ToListAsync(ct);
    }
}
