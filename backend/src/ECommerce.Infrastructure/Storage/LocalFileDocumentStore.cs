using ECommerce.UseCases.Invoicing.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Storage;

/// <summary>
/// Local filesystem object store for generated documents. Configure the base
/// directory with <c>Storage:BasePath</c> (default: <c>./storage</c>).
/// </summary>
public sealed class LocalFileDocumentStore(
    ILogger<LocalFileDocumentStore> logger,
    string basePath) : IInvoiceDocumentStore
{
    private readonly string _basePath = Path.GetFullPath(basePath);

    public async Task<string> PutAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        var relative = key.TrimStart('/', '\\');
        if (!IsSafeRelative(relative))
        {
            throw new InvalidOperationException($"Unsafe document key '{key}'.");
        }

        var path = Path.Combine(_basePath, relative);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(path, content, cancellationToken);

        logger.LogDebug("Stored document {Key} ({Length} bytes).", key, content.Length);

        return relative;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var relative = key.TrimStart('/', '\\');
        var path = Path.Combine(_basePath, relative);

        return !File.Exists(path)
            ? null
            : await File.ReadAllBytesAsync(path, cancellationToken);
    }

    private static bool IsSafeRelative(string relative) =>
        !relative.Contains("..", StringComparison.Ordinal);
}
