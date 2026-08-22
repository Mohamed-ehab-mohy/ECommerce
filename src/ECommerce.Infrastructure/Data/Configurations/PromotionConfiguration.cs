using ECommerce.Domain.Pricing;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");

        builder.HasKey(promotion => promotion.Id);
        builder.Property(promotion => promotion.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(promotion => promotion.Name)
            .HasMaxLength(120)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(promotion => promotion.State)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired()
            .HasColumnName("state");

        builder.Property(promotion => promotion.StartsAt).HasColumnName("starts_at");
        builder.Property(promotion => promotion.EndsAt).HasColumnName("ends_at");

        builder.Property(promotion => promotion.Stacking)
            .HasColumnType("jsonb")
            .HasColumnName("stacking_matrix")
            .HasConversion(new JsonValueConverter<StackingMatrix>())
            .IsRequired();

        builder.Property(promotion => promotion.Conditions)
            .HasColumnType("jsonb")
            .HasColumnName("conditions")
            .HasConversion(new JsonValueConverter<IReadOnlyCollection<PromotionCondition>>())
            .IsRequired();

        builder.Property(promotion => promotion.Actions)
            .HasColumnType("jsonb")
            .HasColumnName("actions")
            .HasConversion(new JsonValueConverter<IReadOnlyCollection<DiscountRule>>())
            .IsRequired();

        builder.Property(promotion => promotion.EligibleCountries)
            .HasColumnType("jsonb")
            .HasColumnName("eligible_countries")
            .HasConversion(new JsonValueConverter<IReadOnlyCollection<string>>())
            .IsRequired();

        builder.Property(promotion => promotion.EligibleCurrencies)
            .HasColumnType("jsonb")
            .HasColumnName("eligible_currencies")
            .HasConversion(new JsonValueConverter<IReadOnlyCollection<string>>())
            .IsRequired();

        builder.Property(promotion => promotion.CreatedAt).HasColumnName("created_at");
        builder.Property(promotion => promotion.UpdatedAt).HasColumnName("updated_at");
        builder.Property(promotion => promotion.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(promotion => promotion.DomainEvents);

        builder.HasIndex(promotion => promotion.State).HasDatabaseName("ix_promotions_state");
        builder.HasIndex(promotion => new { promotion.State, promotion.StartsAt, promotion.EndsAt })
            .HasDatabaseName("ix_promotions_active")
            .HasFilter("\"state\" = 'Active'");
    }
}
