using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Pricing;
using FluentValidation;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(ICurrencyCatalog currencies, ILocaleCatalog locales)
    {
        RuleFor(x => x.ProductId).NotEmpty();

        When(x => x.Slug is not null, () =>
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(160)
                .Matches("^[a-z0-9-]+$");
        });

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(255);
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(5000);
        });

        When(x => x.Currency is not null, () =>
        {
            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .Must(currency => currencies.IsSupported(currency))
                .WithMessage("'{PropertyValue}' is not a supported currency.");
        });

        When(x => x.ListAmount is not null, () =>
        {
            RuleFor(x => x.ListAmount)
                .GreaterThan(0)
                .LessThanOrEqualTo(999_999_999.99m);
        });

        When(x => x.OfferAmount is not null && x.ListAmount is not null, () =>
        {
            RuleFor(x => x.OfferAmount)
                .LessThanOrEqualTo(x => x.ListAmount!.Value);
        });

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status)
                .IsEnumName(typeof(ProductStatus), caseSensitive: false);
        });

        When(x => x.Locale is not null, () =>
        {
            RuleFor(x => x.Locale)
                .NotEmpty()
                .MaximumLength(10)
                .Must(locale => locales.IsSupported(locale))
                .WithMessage("'{PropertyValue}' is not a supported locale.");
        });

        RuleFor(x => x)
            .Must(x => x.Slug is not null ||
                       x.Name is not null ||
                       x.Description is not null ||
                       x.Currency is not null ||
                       x.ListAmount is not null ||
                       x.OfferAmount is not null ||
                       x.CategoryId is not null ||
                       x.BrandId is not null ||
                       x.IsFeatured is not null ||
                       x.Status is not null ||
                       x.Locale is not null)
            .WithMessage("At least one product field must be provided.");
    }
}
