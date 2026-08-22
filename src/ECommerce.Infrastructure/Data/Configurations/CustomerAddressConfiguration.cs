using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses");

        builder.HasKey(address => address.Id);
        builder.Property(address => address.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(address => address.CustomerId).IsRequired().HasColumnName("customer_id");
        builder.Property(address => address.Label).HasMaxLength(50).HasColumnName("label");
        builder.Property(address => address.Street).HasMaxLength(200).IsRequired().HasColumnName("street");
        builder.Property(address => address.City).HasMaxLength(100).IsRequired().HasColumnName("city");
        builder.Property(address => address.Region).HasMaxLength(100).HasColumnName("region");
        builder.Property(address => address.Country).HasMaxLength(2).IsRequired().HasColumnName("country");
        builder.Property(address => address.PostalCode).HasMaxLength(20).HasColumnName("postal_code");

        builder.HasIndex(address => address.CustomerId);

        builder.Property(address => address.CreatedAt).HasColumnName("created_at");
        builder.Property(address => address.UpdatedAt).HasColumnName("updated_at");
        builder.Property(address => address.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(address => address.DomainEvents);
    }
}
