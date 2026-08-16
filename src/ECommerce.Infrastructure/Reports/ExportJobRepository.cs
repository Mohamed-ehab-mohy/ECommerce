using ECommerce.Domain.Reporting;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Reports.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Reports;

public sealed class ExportJobRepository(ECommerceDbContext dbContext) : IExportJobRepository
{
    public Task<ExportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<ExportJob>()
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

    public void Add(ExportJob exportJob) => dbContext.Set<ExportJob>().Add(exportJob);
}
