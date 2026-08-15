using FluentValidation;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed class GetProductImportQueryValidator : AbstractValidator<GetProductImportQuery>
{
    public GetProductImportQueryValidator()
    {
        RuleFor(query => query.ImportId).NotEmpty();
    }
}
