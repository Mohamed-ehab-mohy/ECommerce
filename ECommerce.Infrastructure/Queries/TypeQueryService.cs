using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Queries;

public sealed class TypeQueryService : ITypeQueryService
{
    private readonly StoreDbContext _dbContext;

    public TypeQueryService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GetAllTypesResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.ProductTypes
            .Where(t => !t.IsDeleted)
            .ProjectToType<GetAllTypesResponse>()
            .ToListAsync(ct);
    }
}
