using ECommerce.Domain.Reporting;
using ECommerce.UseCases.Reports.Ports;

namespace ECommerce.UseCases.Reports.Ports;

public interface IExportJobRepository
{
    Task<ExportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(ExportJob exportJob);
}

public interface IExportJobScheduler
{
    void Enqueue(Guid exportId);
}

/// <summary>Stores generated report files (T-DAT-017). Key is a safe relative path.</summary>
public interface IExportFileStore
{
    Task<string> PutAsync(string key, byte[] content, CancellationToken cancellationToken);

    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken);
}
