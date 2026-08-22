using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CategoryHierarchyConfiguration : IEntityTypeConfiguration<CategoryHierarchy>
{
    public void Configure(EntityTypeBuilder<CategoryHierarchy> builder)
    {
        builder.ToTable("category_hierarchy");

        builder.HasKey(hierarchy => new { hierarchy.AncestorId, hierarchy.DescendantId });

        builder.Property(hierarchy => hierarchy.AncestorId).IsRequired().HasColumnName("ancestor_id");
        builder.Property(hierarchy => hierarchy.DescendantId).IsRequired().HasColumnName("descendant_id");
        builder.Property(hierarchy => hierarchy.Depth).HasColumnName("depth");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(hierarchy => hierarchy.AncestorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(hierarchy => hierarchy.DescendantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(hierarchy => hierarchy.DescendantId)
            .HasDatabaseName("ix_category_hierarchy_descendant_id");
    }
}
