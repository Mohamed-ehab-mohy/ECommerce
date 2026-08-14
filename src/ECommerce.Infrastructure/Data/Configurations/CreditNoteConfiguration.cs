using ECommerce.Domain.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("credit_notes");

        builder.HasKey(creditNote => creditNote.Id);
        builder.Property(creditNote => creditNote.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(creditNote => creditNote.CreditNoteNumber)
            .HasMaxLength(CreditNoteNumber.MaxLength)
            .IsRequired()
            .HasColumnName("credit_note_number");

        builder.Property(creditNote => creditNote.InvoiceId).HasColumnName("invoice_id");
        builder.Property(creditNote => creditNote.RefundId).HasColumnName("refund_id");

        builder.Property(creditNote => creditNote.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(creditNote => creditNote.Reason)
            .HasMaxLength(500)
            .IsRequired()
            .HasColumnName("reason");

        builder.Property(creditNote => creditNote.IssuedAt).HasColumnName("issued_at");

        builder.Property(creditNote => creditNote.CreatedAt).HasColumnName("created_at");
        builder.Property(creditNote => creditNote.UpdatedAt).HasColumnName("updated_at");
        builder.Property(creditNote => creditNote.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(creditNote => creditNote.DomainEvents);

        builder.HasIndex(creditNote => creditNote.CreditNoteNumber)
            .IsUnique()
            .HasDatabaseName("ux_credit_notes_credit_note_number");

        builder.HasIndex(creditNote => creditNote.InvoiceId).HasDatabaseName("ix_credit_notes_invoice_id");
        builder.HasIndex(creditNote => creditNote.RefundId).HasDatabaseName("ux_credit_notes_refund_id");
    }
}
