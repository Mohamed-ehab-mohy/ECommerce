using ECommerce.Domain.Fulfillment;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class FulfillmentTaskItemConfiguration : IEntityTypeConfiguration<FulfillmentTaskItem>
{
    public void Configure(EntityTypeBuilder<FulfillmentTaskItem> builder)
    {
        builder.ToTable("fulfillment_task_items");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(item => item.TaskId)
            .IsRequired()
            .HasColumnName("task_id");
        builder.HasOne<FulfillmentTask>()
            .WithMany(task => task.Items)
            .HasForeignKey(item => item.TaskId)
            .HasConstraintName("fk_fulfillment_task_items_tasks");

        builder.Property(item => item.ProductId)
            .IsRequired()
            .HasColumnName("product_id");

        builder.Property(item => item.Sku)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnName("sku");

        builder.Property(item => item.Quantity)
            .IsRequired()
            .HasColumnName("quantity");

        builder.Property(item => item.BinLocation)
            .HasMaxLength(32)
            .HasColumnName("bin_location");

        builder.HasIndex(item => item.TaskId).HasDatabaseName("ix_fulfillment_task_items_task_id");
    }
}
