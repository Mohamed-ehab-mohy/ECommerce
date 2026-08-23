using ECommerce.Domain.Invoicing;

namespace ECommerce.UseCases.Invoicing.Ports;

public interface IInvoiceNumberGenerator
{
    Task<InvoiceNumber> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken);
}
