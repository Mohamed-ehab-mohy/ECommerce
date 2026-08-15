using ECommerce.Domain.Catalog;
using ECommerce.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("product_reviews");

        builder.HasKey(review => review.Id);
        builder.Property(review => review.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(review => review.ProductId).HasColumnName("product_id");
        builder.Property(review => review.CustomerId).HasColumnName("customer_id");
        builder.Property(review => review.Rating).HasColumnName("rating");
        builder.Property(review => review.Comment)
            .HasMaxLength(2000)
            .IsRequired()
            .HasColumnName("comment");
        builder.Property(review => review.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("status");
        builder.Property(review => review.VerifiedPurchase).HasColumnName("verified_purchase");
        builder.Property(review => review.ModeratorId).HasColumnName("moderator_id");
        builder.Property(review => review.RejectionReason)
            .HasMaxLength(500)
            .HasColumnName("rejection_reason");
        builder.Property(review => review.ModeratedAt).HasColumnName("moderated_at");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(review => review.ProductId)
            .HasConstraintName("fk_product_reviews_products")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(review => review.CreatedAt).HasColumnName("created_at");
        builder.Property(review => review.UpdatedAt).HasColumnName("updated_at");
        builder.Property(review => review.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(review => review.DomainEvents);

        builder.HasIndex(review => new { review.ProductId, review.CustomerId })
            .IsUnique()
            .HasDatabaseName("ix_product_reviews_product_customer");
        builder.HasIndex(review => review.Status)
            .HasDatabaseName("ix_product_reviews_status");
    }
}
