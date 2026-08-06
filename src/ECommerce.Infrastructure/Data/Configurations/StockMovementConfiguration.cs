using ECommerce.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(movement => movement.StockItemId)
            .IsRequired()
            .HasColumnName("stock_item_id");
        builder.HasOne<StockItem>()
            .WithMany()
            .HasForeignKey(movement => movement.StockItemId)
            .HasConstraintName("fk_stock_movements_stock_items");
        builder.HasIndex(movement => movement.StockItemId)
            .HasDatabaseName("ix_stock_movements_stock_item_id");

        builder.Property(movement => movement.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(movement => movement.Quantity)
            .IsRequired()
            .HasColumnName("quantity");

        builder.Property(movement => movement.OnHandDelta)
            .IsRequired()
            .HasColumnName("on_hand_delta");

        builder.Property(movement => movement.AllocatedDelta)
            .IsRequired()
            .HasColumnName("allocated_delta");

        builder.Property(movement => movement.Reason)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("reason");

        builder.Property(movement => movement.Reference)
            .HasMaxLength(100)
            .HasColumnName("reference");

        builder.Property(movement => movement.Note)
            .HasMaxLength(500)
            .HasColumnName("note");

        builder.Property(movement => movement.CreatedAt).HasColumnName("created_at");
        builder.Property(movement => movement.UpdatedAt).HasColumnName("updated_at");
        builder.Property(movement => movement.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(movement => movement.DomainEvents);
    }
}
