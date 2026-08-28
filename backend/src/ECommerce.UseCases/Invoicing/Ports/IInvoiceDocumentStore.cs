namespace ECommerce.UseCases.Invoicing.Ports;

/// <summary>
/// Object-store persistence for generated finance documents. Keys are relative
/// document references (e.g. <c>invoices/I-20260814-000001.pdf</c>).
/// </summary>
public interface IInvoiceDocumentStore
{
    Task<string> PutAsync(string key, byte[] content, CancellationToken cancellationToken);

    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken);
}
