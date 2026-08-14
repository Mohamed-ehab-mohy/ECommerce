using ECommerce.UseCases.Invoicing.Services;
using Hangfire;

namespace ECommerce.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 5)]
public sealed class GenerateInvoicePdfJob(InvoicePdfGenerationService pdfGenerationService)
{
    public async Task ExecuteAsync(Guid invoiceId)
    {
        await pdfGenerationService.GenerateAsync(invoiceId, CancellationToken.None);
    }
}
