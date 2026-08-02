using ECommerce.Domain.Identity;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Identity.Responses;

public static class CustomerLookupResponseFactory
{
    public static CustomerLookupResponse From(Customer customer, bool includePii)
    {
        return new CustomerLookupResponse(
            customer.Id,
            includePii ? customer.Email : PiiMasker.MaskEmail(customer.Email),
            customer.DisplayName,
            includePii ? customer.Phone : PiiMasker.MaskPhone(customer.Phone),
            customer.Locale,
            customer.Currency,
            customer.EmailVerified,
            customer.CreatedAt);
    }
}
