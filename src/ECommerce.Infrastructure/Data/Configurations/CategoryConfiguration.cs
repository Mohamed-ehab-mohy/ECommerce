using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(category => category.Name)
            .HasMaxLength(120)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(category => category.Slug)
            .HasMaxLength(160)
            .IsRequired()
            .HasColumnName("slug");
        builder.HasIndex(category => category.Slug)
            .IsUnique()
            .HasDatabaseName("ux_categories_slug");

        builder.Property(category => category.ParentId).HasColumnName("parent_id");
        builder.Property(category => category.SortOrder).HasColumnName("sort_order");
        builder.Property(category => category.Level).HasColumnName("level");

        builder.HasOne<Category>(category => category.Parent)
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => category.ParentId).HasDatabaseName("ix_categories_parent_id");

        builder.Property(category => category.CreatedAt).HasColumnName("created_at");
        builder.Property(category => category.UpdatedAt).HasColumnName("updated_at");
        builder.Property(category => category.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(category => category.DomainEvents);
    }
}
