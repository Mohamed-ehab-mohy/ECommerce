using System.Text.Json;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

/// <summary>
/// Starts an async bulk product import: persists the batch, enqueues the processing job, and
/// returns the import id.
/// </summary>
public sealed class StartProductImportCommandHandler(
    IProductImportRepository imports,
    IProductImportJobScheduler scheduler,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<StartProductImportCommand> validator) : IRequestHandler<StartProductImportCommand, Result<ProductImportStartedResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<ProductImportStartedResponse>> Handle(
        StartProductImportCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ProductImportStartedResponse>();
        }

        var rowsJson = JsonSerializer.Serialize(request.Rows, JsonOptions);
        var import = ProductImport.Create(rowsJson, request.Rows.Count, timeProvider.GetUtcNow().UtcDateTime);

        imports.Add(import);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        scheduler.Enqueue(import.Id);

        return Result<ProductImportStartedResponse>.Success(
            new ProductImportStartedResponse(import.Id, import.Status.ToString()));
    }
}
