using FluentValidation;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
        RuleFor(x => x.Locale).MaximumLength(10).When(x => x.Locale is not null);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
    }
}
