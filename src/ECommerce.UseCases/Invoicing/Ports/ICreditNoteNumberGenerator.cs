using ECommerce.Domain.Invoicing;

namespace ECommerce.UseCases.Invoicing.Ports;

public interface ICreditNoteNumberGenerator
{
    Task<CreditNoteNumber> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken);
}
