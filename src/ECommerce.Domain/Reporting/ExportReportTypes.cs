namespace ECommerce.Domain.Reporting;

/// <summary>Report types that can be queried or exported (docs/08 §6.9, T-DAT-017).</summary>
public static class ExportReportTypes
{
    public const string Sales = "sales";
    public const string Inventory = "inventory";
    public const string Finance = "finance";

    public static IReadOnlyList<string> All { get; } = [Sales, Inventory, Finance];

    public static bool IsSupported(string reportType) =>
        All.Contains(reportType, StringComparer.OrdinalIgnoreCase);
}
