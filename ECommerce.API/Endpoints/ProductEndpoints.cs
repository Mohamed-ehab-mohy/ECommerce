namespace ECommerce.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/products")
            .WithTags("Products");
    }
}
