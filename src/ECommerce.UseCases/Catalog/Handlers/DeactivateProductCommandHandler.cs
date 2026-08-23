using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class DeactivateProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter) : IRequestHandler<DeactivateProductCommand, Result>
{
    public async Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.ProductNotFound);
        }

        var before = new { product.Status };

        product.Deactivate();

        var after = new { product.Status };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ProductDeactivated,
            "Product",
            product.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
