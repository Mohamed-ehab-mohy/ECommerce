# 22 — Analytics & Reporting

## 1. Overview

The reporting module provides real-time analytical queries (sales, inventory, finance, promotions, fulfillment) and async CSV export. Reports are computed from the transactional database via covering indexes; exports are offloaded to Hangfire jobs.

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `ExportJob` | `Reporting` | Tracks async export lifecycle: `Queued -> Running -> Completed/Failed`. Stores `FileKey` for retrieval. |
| `ExportJobStatus` (enum) | `Reporting` | `Queued`, `Running`, `Completed`, `Failed`. |
| `ExportReportTypes` (static) | `Reporting` | Constants: `sales`, `inventory`, `finance`, `promotions`, `fulfillment`. |

### Key Ports

| Port | Purpose |
|------|---------|
| `IReportingQueryService` | Read-model queries: `GetSalesSeriesAsync`, `GetInventoryAsync`, `GetFinanceAsync`, `GetPromotionsAsync`, `GetFulfillmentAsync`. |
| `IExportJobScheduler` | Enqueues `GenerateExportJob` into Hangfire. |
| `IExportJobRepository` | CRUD for `ExportJob` aggregate. |
| `IExportFileStore` | Local filesystem storage for generated CSV files. |

## 3. Key Operations

| Operation | Flow | Key Files |
|-----------|------|-----------|
| **Sales report** | Query -> `ReportingQueryService.GetSalesSeriesAsync()` buckets orders by day/week/month | `ReportingQueryService.cs` |
| **Inventory report** | Query -> `GetInventoryAsync()` stock levels across warehouses with low-stock flag | `ReportingQueryService.cs` |
| **Finance report** | Query -> `GetFinanceAsync()` collected/refunded/outstanding by currency from `PaymentLedgerEntry` | `ReportingQueryService.cs` |
| **Promotions report** | Query -> `GetPromotionsAsync()` per-promotion orders applied, discount total, coupon redemptions | `ReportingQueryService.cs` |
| **Fulfillment report** | Query -> `GetFulfillmentAsync()` tasks shipped, avg hours, on-time rate, per-warehouse breakdown | `ReportingQueryService.cs` |
| **Start export** | `POST /exports` -> `CreateExportCommand` -> `ExportJob.Create()` -> `IExportJobScheduler.Enqueue()` | `ExportsController.cs`, `HangfireExportJobScheduler.cs` |
| **Execute export** | Hangfire -> `GenerateExportJob.ExecuteAsync()` -> query + render CSV -> `IExportFileStore.PutAsync()` | `GenerateExportJob.cs`, `CsvReportRenderer.cs` |
| **Download export** | `GET /exports/{id}/download` -> `IExportFileStore.GetAsync()` -> stream CSV | `ExportsController.cs`, `LocalExportFileStore.cs` |

### Export Flow

1. Client POSTs to `/api/v1/exports` with report type + filters.
2. `ExportJob` is created with `Queued` status and serialized `FiltersJson`.
3. `HangfireExportJobScheduler` enqueues `GenerateExportJob`.
4. `GenerateExportJob` marks job `Running`, runs the appropriate `IReportingQueryService` method, renders via `CsvReportRenderer`, stores file in `LocalExportFileStore`.
5. On completion, client polls `GET /api/v1/exports/{id}` for status; downloads via `/download` endpoint.

### Report Type Constants

Defined in `ExportReportTypes`: `sales`, `inventory`, `finance`, `promotions`, `fulfillment`. The `IsSupported()` method validates incoming report type strings.

## 4. API Endpoints

| Method | Route | Controller | Description |
|--------|-------|------------|-------------|
| `GET` | `/api/v1/reports/sales` | `ReportsController` | Sales time-series (from/to/granularity/currency) |
| `GET` | `/api/v1/reports/inventory` | `ReportsController` | Inventory position report |
| `GET` | `/api/v1/reports/finance` | `ReportsController` | Finance report (from/to date range) |
| `GET` | `/api/v1/reports/promotions` | `ReportsController` | Promotion performance report (from/to) |
| `GET` | `/api/v1/reports/fulfillment` | `ReportsController` | Fulfillment SLA report (from/to) |
| `POST` | `/api/v1/exports` | `ExportsController` | Start async CSV export |
| `GET` | `/api/v1/exports/{exportId}` | `ExportsController` | Get export status |
| `GET` | `/api/v1/exports/{exportId}/download` | `ExportsController` | Download completed CSV |

## 5. Integration Points

- **Database queries**: `ReportingQueryService` reads directly from `Order`, `Payment`, `PaymentLedgerEntry`, `StockItem`, `Warehouse`, `Promotion`, `Coupon`, `FulfillmentTask` tables.
- **Hangfire**: `GenerateExportJob` (`[AutomaticRetry(Attempts = 2)]`) runs async. Scheduled via `HangfireExportJobScheduler`.
- **CSV rendering**: `CsvReportRenderer` renders RFC-4180-style CSV for each report type.
- **File storage**: `LocalExportFileStore` writes to `Storage:BasePath/exports/` on the local filesystem.
- **Finance reports**: Read `PaymentLedgerEntry` for captured/refunded amounts, `Payment` for authorized outstanding.

## 6. File References

| File | Path |
|------|------|
| `ExportJob.cs` | `src/ECommerce.Domain/Reporting/ExportJob.cs` |
| `ExportReportTypes.cs` | `src/ECommerce.Domain/Reporting/ExportReportTypes.cs` |
| `ExportErrors.cs` | `src/ECommerce.Domain/Reporting/ExportErrors.cs` |
| `ReportsController.cs` | `src/ECommerce.API/Controllers/ReportsController.cs` |
| `ExportsController.cs` | `src/ECommerce.API/Controllers/ExportsController.cs` |
| `ReportingQueryService.cs` | `src/ECommerce.Infrastructure/Reports/ReportingQueryService.cs` |
| `CsvReportRenderer.cs` | `src/ECommerce.Infrastructure/Reports/CsvReportRenderer.cs` |
| `ExportJobRepository.cs` | `src/ECommerce.Infrastructure/Reports/ExportJobRepository.cs` |
| `HangfireExportJobScheduler.cs` | `src/ECommerce.Infrastructure/Reports/HangfireExportJobScheduler.cs` |
| `LocalExportFileStore.cs` | `src/ECommerce.Infrastructure/Reports/LocalExportFileStore.cs` |
| `GenerateExportJob.cs` | `src/ECommerce.Infrastructure/Jobs/GenerateExportJob.cs` |
