namespace ECommerce.UseCases.Catalog.Ports;

/// <summary>Enqueues an import batch for asynchronous processing.</summary>
public interface IProductImportJobScheduler
{
    void Enqueue(Guid importId);
}
