using ECommerce.Domain.Catalog;
using HotChocolate.Types;

namespace ECommerce.Infrastructure.GraphQL;

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Ignore(p => p.DomainEvents);
    }
}
