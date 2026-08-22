using ECommerce.Domain.Invoicing;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Invoicing.Ports;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class CreditNoteNumberGenerator(ECommerceDbContext dbContext) : ICreditNoteNumberGenerator
{
    public async Task<CreditNoteNumber> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var sequence = await dbContext.Database
            .SqlQuery<long>($"SELECT nextval('credit_note_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return CreditNoteNumber.Create(utcNow, sequence);
    }
}
