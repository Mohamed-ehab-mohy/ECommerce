
namespace ECommerce.Domain.Invoicing;

public static class CreditNoteErrors
{
    public static readonly Error CreditNoteNotFound = new(
        "ERR_CN_001",
        "The credit note was not found.",
        ErrorType.NotFound);

    public static readonly Error InvalidAmount = new(
        "ERR_CN_002",
        "The credit note amount must be greater than zero.",
        ErrorType.Validation);
}
