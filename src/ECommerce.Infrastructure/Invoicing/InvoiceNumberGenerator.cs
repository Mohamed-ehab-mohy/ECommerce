using ECommerce.Domain.Invoicing;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Invoicing.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class InvoiceNumberGenerator(ECommerceDbContext dbContext) : IInvoiceNumberGenerator
{
    public async Task<InvoiceNumber> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var sequence = await dbContext.Database
            .SqlQuery<long>($"SELECT nextval('invoice_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return InvoiceNumber.Create(utcNow, sequence);
    }
}
