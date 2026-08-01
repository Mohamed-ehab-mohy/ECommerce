using ECommerce.Domain.Common;

namespace ECommerce.Domain.Identity;

public sealed class CustomerAddress : BaseEntity<Guid>
{
    private CustomerAddress()
    {
        Label = null;
        Street = string.Empty;
        City = string.Empty;
        Region = null;
        Country = string.Empty;
        PostalCode = null;
    }

    public Guid CustomerId { get; private set; }

    public string? Label { get; private set; }

    public string Street { get; private set; }

    public string City { get; private set; }

    public string? Region { get; private set; }

    public string Country { get; private set; }

    public string? PostalCode { get; private set; }

    public static CustomerAddress Create(
        Guid customerId,
        string? label,
        string street,
        string city,
        string? region,
        string country,
        string? postalCode,
        DateTime utcNow)
    {
        return new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Label = string.IsNullOrWhiteSpace(label) ? null : label,
            Street = street,
            City = city,
            Region = string.IsNullOrWhiteSpace(region) ? null : region,
            Country = country,
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }
}
