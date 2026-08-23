namespace ECommerce.UseCases.Invoicing.Ports;

public interface IInvoicePdfJobScheduler
{
    void Enqueue(Guid invoiceId);
}
