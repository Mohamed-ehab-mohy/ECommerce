using ECommerce.Infrastructure.Jobs;
using ECommerce.UseCases.Invoicing.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class HangfireInvoicePdfJobScheduler(
    IBackgroundJobClient? backgroundJobClient,
    ILogger<HangfireInvoicePdfJobScheduler> logger) : IInvoicePdfJobScheduler
{
    public void Enqueue(Guid invoiceId)
    {
        if (backgroundJobClient is not null)
        {
            backgroundJobClient.Enqueue<GenerateInvoicePdfJob>(job => job.ExecuteAsync(invoiceId));
            return;
        }

        logger.LogInformation(
            "Hangfire not configured; invoice PDF generation for {InvoiceId} will not be scheduled.",
            invoiceId);
    }
}
