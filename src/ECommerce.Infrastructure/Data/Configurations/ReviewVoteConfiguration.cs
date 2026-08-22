using ECommerce.Domain.Reviews;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("review_votes");

        builder.HasKey(vote => vote.Id);
        builder.Property(vote => vote.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(vote => vote.ReviewId).HasColumnName("review_id");
        builder.Property(vote => vote.CustomerId).HasColumnName("customer_id");
        builder.Property(vote => vote.Value)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("value");

        builder.HasOne<ProductReview>()
            .WithMany()
            .HasForeignKey(vote => vote.ReviewId)
            .HasConstraintName("fk_review_votes_product_reviews")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(vote => vote.CreatedAt).HasColumnName("created_at");
        builder.Property(vote => vote.UpdatedAt).HasColumnName("updated_at");
        builder.Property(vote => vote.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(vote => vote.DomainEvents);

        builder.HasIndex(vote => new { vote.ReviewId, vote.CustomerId })
            .IsUnique()
            .HasDatabaseName("ix_review_votes_review_customer");
    }
}
