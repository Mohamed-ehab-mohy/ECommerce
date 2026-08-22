
namespace ECommerce.UseCases.Wishlist.Queries;

public sealed class GetWishlistQueryValidator : AbstractValidator<GetWishlistQuery>
{
    public GetWishlistQueryValidator()
    {
        RuleFor(query => query.OwnerKey).NotEmpty().MaximumLength(64);
    }
}
