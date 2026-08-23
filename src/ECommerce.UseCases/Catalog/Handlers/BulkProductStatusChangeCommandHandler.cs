using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

/// <summary>
/// Applies a bulk product status change and reports partial success per item (US-M-008, BR-2308,
/// FR-13-008).
/// </summary>
public sealed class BulkProductStatusChangeCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IValidator<BulkProductStatusChangeCommand> validator,
    IAuditLogWriter auditLogWriter,
    TimeProvider timeProvider) : IRequestHandler<BulkProductStatusChangeCommand, Result<BulkProductStatusChangeResponse>>
{
    public async Task<Result<BulkProductStatusChangeResponse>> Handle(
        BulkProductStatusChangeCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<BulkProductStatusChangeResponse>();
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var existing = await products.GetByIdsAsync(productIds, cancellationToken);
        var byId = existing.ToDictionary(product => product.Id);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var results = new List<BulkProductStatusItemResponse>(request.Items.Count);
        var succeeded = 0;
        var failed = 0;

        foreach (var item in request.Items)
        {
            if (!byId.TryGetValue(item.ProductId, out var product))
            {
                failed++;
                results.Add(new BulkProductStatusItemResponse(item.ProductId, null, false, $"Product '{item.ProductId}' was not found."));
                continue;
            }

            var result = Apply(product, item.Action, utcNow);
            if (result is not null)
            {
                failed++;
                results.Add(new BulkProductStatusItemResponse(product.Id, product.Sku, false, result));
                continue;
            }

            succeeded++;
            results.Add(new BulkProductStatusItemResponse(product.Id, product.Sku, true, null));
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.BulkProductStatusChange,
            "Product",
            $"bulk:{request.Items.Count}",
            After: new { Requested = request.Items.Count, Succeeded = succeeded, Failed = failed }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BulkProductStatusChangeResponse>.Success(
            new BulkProductStatusChangeResponse(request.Items.Count, succeeded, failed, results));
    }

    private static string? Apply(Product product, BulkProductStatusAction action, DateTime utcNow)
    {
        switch (action)
        {
            case BulkProductStatusAction.Activate when product.Status != ProductStatus.Active:
                product.Activate();
                return null;

            case BulkProductStatusAction.Deactivate when product.Status != ProductStatus.Inactive:
                product.Deactivate();
                return null;

            case BulkProductStatusAction.Activate:
            case BulkProductStatusAction.Deactivate:
                return $"Product '{product.Sku}' is already in the requested status.";

            default:
                return "Unknown action.";
        }
    }
}
