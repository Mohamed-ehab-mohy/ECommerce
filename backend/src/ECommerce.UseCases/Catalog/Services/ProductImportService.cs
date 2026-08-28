using System.Text.Json;
using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Catalog.Services;

/// <summary>
/// Processes a bulk product import batch: validates every row, creates the valid products, and
/// records per-row errors so partial success is reported.
/// </summary>
public sealed class ProductImportService(
    IProductImportRepository imports,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ICurrencyCatalog currencies,
    ILocaleCatalog locales,
    IAuditLogWriter auditLogWriter,
    ILogger<ProductImportService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(Guid importId, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var import = await imports.GetByIdAsync(importId, cancellationToken);
        if (import is null)
        {
            logger.LogWarning("Product import {ImportId} not found; skipping.", importId);
            return;
        }

        // Idempotent: an already-processed import is not re-run.
        if (import.Status is ProductImportStatus.Completed or ProductImportStatus.Failed)
        {
            logger.LogInformation("Product import {ImportId} already processed; skipping.", importId);
            return;
        }

        import.MarkProcessing(utcNow);

        List<ProductImportRow> rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<ProductImportRow>>(import.RowsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            import.Fail(utcNow);
            await auditLogWriter.WriteAsync(new AuditOperation(
                AuditActions.ProductImportRun,
                "ProductImport",
                import.Id.ToString(),
                After: new { import.TotalRows, import.SucceededRows, import.FailedRows }),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingBySku = await products.GetBySkusAsync(
            rows.Where(row => row.Sku is not null).Select(row => row.Sku.Trim().ToUpperInvariant()).ToArray(),
            cancellationToken);
        var existingSkuSet = existingBySku.Select(product => product.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var rowNumber = 0; rowNumber < rows.Count; rowNumber++)
        {
            var row = rows[rowNumber];
            var error = await ValidateRowAsync(row, seenSkus, seenSlugs, existingSkuSet, cancellationToken);
            if (error is not null)
            {
                import.AddError(rowNumber + 1, row.Sku, error, utcNow);
                continue;
            }

            var sku = row.Sku.Trim().ToUpperInvariant();
            var slug = CreateSlug(row.Slug, row.Name, seenSlugs);
            seenSkus.Add(sku);
            seenSlugs.Add(slug);

            products.Add(Product.Create(
                sku,
                slug,
                row.Locale.Trim().ToLowerInvariant(),
                row.Name.Trim(),
                row.Description?.Trim(),
                row.Currency.Trim().ToUpperInvariant(),
                row.ListAmount,
                row.OfferAmount,
                row.CategoryId,
                row.BrandId,
                row.IsFeatured,
                ParseStatus(row.Status),
                utcNow));

            import.AddSucceeded();
        }

        import.Complete(utcNow);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ProductImportRun,
            "ProductImport",
            import.Id.ToString(),
            After: new { import.TotalRows, import.SucceededRows, import.FailedRows }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Product import {ImportId} completed: {Succeeded} succeeded, {Failed} failed of {Total}.",
            import.Id,
            import.SucceededRows,
            import.FailedRows,
            import.TotalRows);
    }

    private async Task<string?> ValidateRowAsync(
        ProductImportRow row,
        ISet<string> seenSkus,
        ISet<string> seenSlugs,
        ISet<string> existingSkuSet,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(row.Sku))
        {
            return "SKU is required.";
        }

        if (row.Sku.Trim().Length is < 3 or > 50)
        {
            return "SKU must be between 3 and 50 characters.";
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(row.Sku.Trim(), "^[a-zA-Z0-9_-]+$"))
        {
            return "SKU may only contain letters, digits, underscores and dashes.";
        }

        var sku = row.Sku.Trim().ToUpperInvariant();
        if (seenSkus.Contains(sku))
        {
            return $"Duplicate SKU '{row.Sku}' in batch.";
        }

        if (existingSkuSet.Contains(sku))
        {
            return $"A product with SKU '{row.Sku}' already exists.";
        }

        if (string.IsNullOrWhiteSpace(row.Name) || row.Name.Trim().Length > 255)
        {
            return "Name is required and must be at most 255 characters.";
        }

        if (string.IsNullOrWhiteSpace(row.Currency) || !currencies.IsSupported(row.Currency.Trim()))
        {
            return $"Currency '{row.Currency}' is not supported.";
        }

        if (row.ListAmount is <= 0 or > 999_999_999.99m)
        {
            return "List amount must be greater than 0 and at most 999999999.99.";
        }

        if (row.OfferAmount is not null && row.OfferAmount > row.ListAmount)
        {
            return "Offer amount cannot exceed the list amount.";
        }

        if (string.IsNullOrWhiteSpace(row.Locale) || !locales.IsSupported(row.Locale.Trim()))
        {
            return $"Locale '{row.Locale}' is not supported.";
        }

        if (row.Status is not null && !Enum.TryParse<ProductStatus>(row.Status, ignoreCase: true, out _))
        {
            return $"Status '{row.Status}' is not a valid product status.";
        }

        var slug = CreateSlug(row.Slug, row.Name, seenSlugs);
        return seenSlugs.Contains(slug)
            ? $"Slug '{slug}' is used by another row in the batch."
            : await products.SlugExistsAsync(slug, cancellationToken)
                ? $"Slug '{slug}' already exists."
                : null;
    }

    private static string CreateSlug(string? slug, string name, ISet<string> seenSlugs)
    {
        var candidate = string.IsNullOrWhiteSpace(slug)
            ? Slugify(name)
            : slug.Trim().ToLowerInvariant();

        var unique = candidate;
        var suffix = 2;
        while (seenSlugs.Contains(unique))
        {
            unique = $"{candidate}-{suffix++}";
        }

        return unique;
    }

    private static string Slugify(string name)
    {
        var slug = System.Text.RegularExpressions.Regex.Replace(
            name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 160 ? slug[..160].Trim('-') : slug;
    }

    private static ProductStatus ParseStatus(string? status) =>
        Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : ProductStatus.Draft;
}
