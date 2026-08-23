using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class CreateProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateProductCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        var sku = request.Sku.Trim().ToUpperInvariant();
        var slug = request.Slug.Trim().ToLowerInvariant();
        var currency = request.Currency.Trim().ToUpperInvariant();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (await products.SkuExistsAsync(sku, cancellationToken))
        {
            return Result<Guid>.Failure(ProductErrors.SkuAlreadyExists);
        }

        if (await products.SlugExistsAsync(slug, cancellationToken))
        {
            return Result<Guid>.Failure(ProductErrors.SlugAlreadyExists);
        }

        var product = Product.Create(
            sku,
            slug,
            request.Locale.Trim().ToLowerInvariant(),
            request.Name.Trim(),
            request.Description?.Trim(),
            currency,
            request.ListAmount,
            request.OfferAmount,
            request.CategoryId,
            request.BrandId,
            request.IsFeatured,
            ParseStatus(request.Status),
            utcNow);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ProductCreated,
            "Product",
            product.Id.ToString(),
            After: new
            {
                product.Sku,
                product.Slug,
                product.Status,
                product.IsFeatured,
                product.CategoryId,
                product.BrandId
            }), cancellationToken);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }

    private static ProductStatus ParseStatus(string? status) =>
        Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : ProductStatus.Draft;
}
