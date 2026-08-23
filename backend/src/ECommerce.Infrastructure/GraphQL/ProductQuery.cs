using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.GraphQL;

public class ProductQuery
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> GetProducts([Service] ECommerceDbContext dbContext)
    {
        return dbContext.Products.AsNoTracking();
    }
}
