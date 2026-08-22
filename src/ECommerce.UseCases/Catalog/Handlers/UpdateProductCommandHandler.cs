using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class UpdateProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateProductCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.ProductNotFound);
        }

        var slug = request.Slug?.Trim().ToLowerInvariant();
        if (slug is not null && slug != product.Slug)
        {
            if (await products.SlugExistsAsync(slug, product.Id, cancellationToken))
            {
                return Result.Failure(ProductErrors.SlugAlreadyExists);
            }
        }

        var before = new
        {
            product.Slug,
            product.Status,
            product.IsFeatured,
            product.CategoryId,
            product.BrandId
        };

        product.UpdateDetails(
            slug,
            request.CategoryId,
            request.BrandId,
            request.IsFeatured,
            ParseStatus(request.Status),
            request.Locale?.Trim().ToLowerInvariant(),
            request.Name?.Trim(),
            request.Description?.Trim(),
            request.Currency?.Trim().ToUpperInvariant(),
            request.ListAmount,
            request.OfferAmount,
            timeProvider.GetUtcNow().UtcDateTime);

        var after = new
        {
            product.Slug,
            product.Status,
            product.IsFeatured,
            product.CategoryId,
            product.BrandId
        };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ProductUpdated,
            "Product",
            product.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static ProductStatus? ParseStatus(string? status) =>
        status is null
            ? null
            : Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var parsed)
                ? parsed
                : null;
}
