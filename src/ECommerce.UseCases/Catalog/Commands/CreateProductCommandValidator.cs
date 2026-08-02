using ECommerce.Domain.Catalog;
using FluentValidation;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(160)
            .Matches("^[a-z0-9-]+$");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.ListAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(999_999_999.99m);

        RuleFor(x => x.OfferAmount)
            .LessThanOrEqualTo(x => x.ListAmount)
            .When(x => x.OfferAmount is not null);

        RuleFor(x => x.Status)
            .IsEnumName(typeof(ProductStatus), caseSensitive: false)
            .When(x => x.Status is not null);

        RuleFor(x => x.Locale)
            .NotEmpty()
            .MaximumLength(10);
    }
}
