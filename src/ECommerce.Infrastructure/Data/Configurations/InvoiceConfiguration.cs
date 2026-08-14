using ECommerce.Domain.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasMaxLength(InvoiceNumber.MaxLength)
            .IsRequired()
            .HasColumnName("invoice_number");

        builder.Property(invoice => invoice.OrderId).HasColumnName("order_id");
        builder.Property(invoice => invoice.CustomerId).HasColumnName("customer_id");

        builder.Property(invoice => invoice.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(invoice => invoice.TaxAmount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("tax_amount");

        builder.Property(invoice => invoice.TaxRate)
            .HasColumnType("decimal(18,6)")
            .IsRequired()
            .HasColumnName("tax_rate");

        builder.Property(invoice => invoice.Total)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("total");

        builder.Property(invoice => invoice.CreditedTotal)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("credited_total");

        builder.Property(invoice => invoice.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(invoice => invoice.PdfUrl)
            .HasMaxLength(500)
            .HasColumnName("pdf_url");

        builder.Property(invoice => invoice.IssuedAt).HasColumnName("issued_at");

        builder.Property(invoice => invoice.CreatedAt).HasColumnName("created_at");
        builder.Property(invoice => invoice.UpdatedAt).HasColumnName("updated_at");
        builder.Property(invoice => invoice.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(invoice => invoice.DomainEvents);

        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey(line => line.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_invoice_lines_invoices");

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("ux_invoices_invoice_number");

        builder.HasIndex(invoice => invoice.OrderId).HasDatabaseName("ux_invoices_order_id");
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines");

        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(line => line.InvoiceId).HasColumnName("invoice_id");

        builder.Property(line => line.Sku)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("sku");

        builder.Property(line => line.Description)
            .HasMaxLength(300)
            .IsRequired()
            .HasColumnName("description");

        builder.Property(line => line.Quantity).IsRequired().HasColumnName("quantity");

        builder.Property(line => line.UnitAmount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("unit_amount");

        builder.Property(line => line.TaxRate)
            .HasColumnType("decimal(18,6)")
            .IsRequired()
            .HasColumnName("tax_rate");

        builder.Property(line => line.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.HasIndex(line => line.InvoiceId).HasDatabaseName("ix_invoice_lines_invoice_id");
    }
}
