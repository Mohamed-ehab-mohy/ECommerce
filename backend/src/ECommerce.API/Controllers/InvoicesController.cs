using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/invoices")]
public sealed class InvoicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListInvoicesQuery(status, page, pageSize), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> Get(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceQuery(invoiceId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{invoiceId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DownloadInvoicePdfQuery(invoiceId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : File(result.Value.Content, "application/pdf", result.Value.FileName);
    }

    [HttpGet("{invoiceId:guid}/credit-notes")]
    public async Task<IActionResult> ListCreditNotes(
        Guid invoiceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListCreditNotesQuery(invoiceId, page, pageSize), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}
