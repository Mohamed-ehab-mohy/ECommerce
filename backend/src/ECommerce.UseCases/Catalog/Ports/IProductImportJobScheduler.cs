namespace ECommerce.UseCases.Catalog.Ports;

/// <summary>Enqueues an import batch for asynchronous processing (FR-02-007).</summary>
public interface IProductImportJobScheduler
{
    void Enqueue(Guid importId);
}
