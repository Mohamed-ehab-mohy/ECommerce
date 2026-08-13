using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class FulfillmentTaskConfiguration : IEntityTypeConfiguration<FulfillmentTask>
{
    public void Configure(EntityTypeBuilder<FulfillmentTask> builder)
    {
        builder.ToTable("fulfillment_tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(task => task.OrderId)
            .IsRequired()
            .HasColumnName("order_id");
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(task => task.OrderId)
            .HasConstraintName("fk_fulfillment_tasks_orders");
        builder.HasIndex(task => task.OrderId)
            .HasDatabaseName("ix_fulfillment_tasks_order_id");

        builder.Property(task => task.WarehouseId)
            .IsRequired()
            .HasColumnName("warehouse_id");

        builder.Property(task => task.ParentTaskId)
            .HasColumnName("parent_task_id");
        builder.HasOne<FulfillmentTask>()
            .WithMany()
            .HasForeignKey(task => task.ParentTaskId)
            .HasConstraintName("fk_fulfillment_tasks_parent");
        builder.HasIndex(task => task.ParentTaskId)
            .HasDatabaseName("ix_fulfillment_tasks_parent_task_id");

        builder.Property(task => task.Zone)
            .HasMaxLength(64)
            .HasColumnName("zone");

        builder.Property(task => task.Priority)
            .IsRequired()
            .HasColumnName("priority");

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(task => task.AssignedTo)
            .HasColumnName("assigned_to");

        builder.Property(task => task.AssignedAt).HasColumnName("assigned_at");
        builder.Property(task => task.StartedAt).HasColumnName("started_at");
        builder.Property(task => task.PackedAt).HasColumnName("packed_at");
        builder.Property(task => task.ShippedAt).HasColumnName("shipped_at");
        builder.Property(task => task.CancelledAt).HasColumnName("cancelled_at");

        builder.Property(task => task.CancellationReason)
            .HasMaxLength(256)
            .HasColumnName("cancellation_reason");

        builder.Property(task => task.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .HasColumnName("version");

        builder.Property(task => task.CreatedAt).HasColumnName("created_at");
        builder.Property(task => task.UpdatedAt).HasColumnName("updated_at");
        builder.Property(task => task.IsDeleted).HasColumnName("is_deleted");

        builder.HasMany(task => task.Items)
            .WithOne()
            .HasForeignKey(item => item.TaskId)
            .HasConstraintName("fk_fulfillment_task_items_tasks");

        builder.HasIndex(task => new { task.WarehouseId, task.Status })
            .HasDatabaseName("ix_fulfillment_tasks_warehouse_status");

        builder.Ignore(task => task.DomainEvents);
    }
}
