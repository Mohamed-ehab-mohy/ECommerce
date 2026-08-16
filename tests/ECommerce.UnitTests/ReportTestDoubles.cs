using ECommerce.Domain.Reporting;
using ECommerce.UseCases.Reports.Ports;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UnitTests;

internal sealed class FakeExportJobRepository : IExportJobRepository
{
    public List<ExportJob> Jobs { get; } = [];

    public Task<ExportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Jobs.FirstOrDefault(job => job.Id == id));

    public void Add(ExportJob job) => Jobs.Add(job);
}

internal sealed class FakeExportJobScheduler : IExportJobScheduler
{
    public List<Guid> Enqueued { get; } = [];

    public void Enqueue(Guid exportId) => Enqueued.Add(exportId);
}

internal sealed class FakeExportFileStore : IExportFileStore
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public Task<string> PutAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        Files[key] = content;
        return Task.FromResult(key);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Files.TryGetValue(key, out var content) ? content : null);
}

internal sealed class FakeReportingQueryService : IReportingQueryService
{
    public IReadOnlyList<SalesPoint> Sales { get; set; } = [];

    public InventoryReportData Inventory { get; set; } = new([], 0);

    public IReadOnlyList<FinanceLine> Finance { get; set; } = [];

    public Task<IReadOnlyList<SalesPoint>> GetSalesSeriesAsync(
        SalesReportFilter filter,
        CancellationToken cancellationToken) =>
        Task.FromResult(Sales);

    public Task<InventoryReportData> GetInventoryAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Inventory);

    public Task<IReadOnlyList<FinanceLine>> GetFinanceAsync(
        FinanceReportFilter filter,
        CancellationToken cancellationToken) =>
        Task.FromResult(Finance);
}
