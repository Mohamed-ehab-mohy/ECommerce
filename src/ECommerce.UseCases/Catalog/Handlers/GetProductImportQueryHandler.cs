using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

/// <summary>Returns the status and per-row error report of an import (US-B-007).</summary>
public sealed class GetProductImportQueryHandler(
    IProductImportRepository imports,
    IValidator<GetProductImportQuery> validator) : IRequestHandler<GetProductImportQuery, Result<ProductImportStatusResponse>>
{
    public async Task<Result<ProductImportStatusResponse>> Handle(
        GetProductImportQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ProductImportStatusResponse>();
        }

        var import = await imports.GetByIdAsync(request.ImportId, cancellationToken);

        return import is null
            ? ProductImportErrors.ImportNotFound
            : new ProductImportStatusResponse(
            import.Id,
            import.Status.ToString(),
            import.TotalRows,
            import.SucceededRows,
            import.FailedRows,
            import.StartedAtUtc,
            import.FinishedAtUtc,
            import.Errors
                .OrderBy(error => error.RowNumber)
                .Select(error => new ProductImportErrorResponse(error.RowNumber, error.Sku, error.Message))
                .ToList());
    }
}
