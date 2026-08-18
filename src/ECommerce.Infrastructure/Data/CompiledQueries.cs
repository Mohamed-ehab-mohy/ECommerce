using ECommerce.Domain.Catalog;
using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

internal static class CompiledQueries
{
    // Orders
    internal static readonly Func<ECommerceDbContext, Guid, Task<Order?>> GetOrderById =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, Guid id) =>
            ctx.Orders
                .Include(o => o.Items)
                .Include(o => o.BackorderItems)
                .FirstOrDefault(o => o.Id == id));

    internal static readonly Func<ECommerceDbContext, string, Task<Order?>> GetOrderByNumber =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, string number) =>
            ctx.Orders
                .Include(o => o.Items)
                .Include(o => o.BackorderItems)
                .FirstOrDefault(o => o.OrderNumber == number));

    internal static readonly Func<ECommerceDbContext, string, Task<Order?>> GetOrderByNumberWithDetails =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, string number) =>
            ctx.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusLogs)
                .Include(o => o.BackorderItems)
                .AsSplitQuery()
                .FirstOrDefault(o => o.OrderNumber == number));

    // Products
    internal static readonly Func<ECommerceDbContext, Guid, Task<Product?>> GetProductById =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, Guid id) =>
            ctx.Products
                .Include(p => p.Translations)
                .Include(p => p.Prices)
                .FirstOrDefault(p => p.Id == id));

    internal static readonly Func<ECommerceDbContext, Guid, Task<Product?>> GetActiveProductById =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, Guid id) =>
            ctx.Products
                .Include(p => p.Translations)
                .Include(p => p.Prices)
                .FirstOrDefault(p => p.Id == id && p.Status == ProductStatus.Active && !p.IsDeleted));

    // Cart
    internal static readonly Func<ECommerceDbContext, Guid, Task<Domain.Cart.Cart?>> GetCartById =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, Guid id) =>
            ctx.Carts
                .Include(c => c.Items)
                .FirstOrDefault(c => c.Id == id));

    internal static readonly Func<ECommerceDbContext, string, Task<Domain.Cart.Cart?>> GetCartByOwnerKey =
        EF.CompileAsyncQuery((ECommerceDbContext ctx, string ownerKey) =>
            ctx.Carts
                .Include(c => c.Items)
                .FirstOrDefault(c => c.OwnerKey == ownerKey));
}
