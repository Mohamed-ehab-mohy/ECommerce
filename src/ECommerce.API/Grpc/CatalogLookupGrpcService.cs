using ECommerce.UseCases.Grpc.Ports;
using Grpc.Core;

namespace ECommerce.API.Grpc;

public sealed class CatalogLookupGrpcService(
    IGrpcQueryService queryService) : Protos.CatalogLookupService.CatalogLookupServiceBase
{
    public override async Task<Protos.ProductSummaryResponse> GetBySku(
        Protos.SkuRequest request,
        ServerCallContext context)
    {
        var dto = await queryService.GetProductBySkuAsync(request.Sku, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Product with SKU '{request.Sku}' not found."));

        return new Protos.ProductSummaryResponse
        {
            Id = dto.Id.ToString(),
            Sku = dto.Sku,
            Name = dto.Name,
            Slug = dto.Slug,
            ListPrice = (double)dto.ListPrice,
            IsActive = dto.IsActive
        };
    }
}
