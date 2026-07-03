using ECommerce.Domain.Entities;
using ECommerce.UseCases.Products.DTOs;
using Mapster;

namespace ECommerce.UseCases.Products;

public sealed class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, GetAllProductsResponse>()
            .Map(dest => dest.ProductType, src => src.ProductType.Name)
            .Map(dest => dest.ProductBrand, src => src.ProductBrand.Name);

        config.NewConfig<Product, GetByIdProductResponse>()
            .Map(dest => dest.ProductType, src => src.ProductType.Name)
            .Map(dest => dest.ProductBrand, src => src.ProductBrand.Name);

        config.NewConfig<ProductBrand, GetAllBrandsResponse>();
        config.NewConfig<ProductType, GetAllTypesResponse>();
    }
}
