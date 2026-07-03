using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
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
            .Select(p => new GetAllProductsResponse(
                p.Id,
                p.Name,
                p.Description,
                p.PictureUrl,
                p.Price,
                p.ProductType.Name,
                p.ProductBrand.Name))
            .ToListAsync(ct);
    }
}
