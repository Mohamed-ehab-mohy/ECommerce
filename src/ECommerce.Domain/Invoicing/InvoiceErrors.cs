using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Invoicing;

public static class InvoiceErrors
{
    public static readonly Error InvoiceNotFound = new(
        "ERR_INV_001",
        "The invoice was not found.",
        ErrorType.NotFound);

    public static readonly Error InvoiceAlreadyExists = new(
        "ERR_INV_002",
        "An invoice already exists for this order.",
        ErrorType.Conflict);

    public static readonly Error InvalidCreditAmount = new(
        "ERR_INV_003",
        "The credit amount must be greater than zero.",
        ErrorType.Validation);

    public static readonly Error CreditExceedsTotal = new(
        "ERR_INV_004",
        "The credit amount exceeds the remaining invoice total.",
        ErrorType.Conflict);

    public static readonly Error InvoiceNotCreditable = new(
        "ERR_INV_005",
        "The invoice cannot be credited in its current state.",
        ErrorType.Conflict);
}
