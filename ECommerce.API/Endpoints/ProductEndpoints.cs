using ECommerce.UseCases.Products;

namespace ECommerce.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/products")
            .WithTags("Products");

        group.MapGet("/", async (IProductQueryService queryService) =>
        {
            var products = await queryService.GetAllProductsAsync();
            return Results.Ok(products);
        });
    }
}
