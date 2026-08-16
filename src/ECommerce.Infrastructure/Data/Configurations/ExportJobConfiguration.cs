using ECommerce.Domain.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        builder.ToTable("export_jobs");

        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(job => job.ReportType)
            .HasMaxLength(16)
            .IsRequired()
            .HasColumnName("report_type");

        builder.Property(job => job.FiltersJson)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("filters_json");

        builder.Property(job => job.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(job => job.RowCount)
            .IsRequired()
            .HasColumnName("row_count");

        builder.Property(job => job.FileKey)
            .HasMaxLength(512)
            .HasColumnName("file_key");

        builder.Property(job => job.CreatedBy).HasColumnName("created_by");
        builder.Property(job => job.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(job => job.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(job => job.CreatedAt).HasColumnName("created_at");
        builder.Property(job => job.UpdatedAt).HasColumnName("updated_at");
        builder.Property(job => job.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(job => job.DomainEvents);

        builder.HasIndex(job => job.Status).HasDatabaseName("ix_export_jobs_status");
    }
}
