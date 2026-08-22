
namespace ECommerce.UseCases.Orders.Queries;

public sealed class SupportOrderLookupQueryValidator : AbstractValidator<SupportOrderLookupQuery>
{
    public SupportOrderLookupQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => !string.IsNullOrWhiteSpace(query.OrderNumber)
                || !string.IsNullOrWhiteSpace(query.Email)
                || query.CustomerId is not null)
            .WithMessage("Provide at least one of order number, email or customer id.")
            .WithErrorCode("SupportLookup.EmptyFilters");

        RuleFor(query => query.OrderNumber).MaximumLength(24);
        RuleFor(query => query.Email).EmailAddress().When(query => !string.IsNullOrWhiteSpace(query.Email));
    }
}
