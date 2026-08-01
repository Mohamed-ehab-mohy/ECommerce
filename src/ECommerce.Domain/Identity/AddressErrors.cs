using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Identity;

public static class AddressErrors
{
    public static readonly Error AddressNotFound = new(
        "Address.AddressNotFound",
        "The address was not found.",
        ErrorType.NotFound);
}
