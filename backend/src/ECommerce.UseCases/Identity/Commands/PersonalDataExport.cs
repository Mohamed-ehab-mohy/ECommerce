namespace ECommerce.UseCases.Identity.Commands;

public sealed record PersonalDataExport(
    string Email,
    string Locale,
    DateTime RegisteredAt,
    IReadOnlyList<OrderExportData> Orders,
    IReadOnlyList<AddressExportData> Addresses,
    IReadOnlyList<string> Roles);

public sealed record OrderExportData(Guid OrderId, string OrderNumber, DateTime CreatedAt, string Status, decimal TotalAmount, string Currency);

public sealed record AddressExportData(string Label, string Street, string City, string State, string ZipCode, string Country);
