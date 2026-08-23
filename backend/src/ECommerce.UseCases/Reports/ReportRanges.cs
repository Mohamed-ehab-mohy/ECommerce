namespace ECommerce.UseCases.Reports;

public static class ReportRanges
{
    public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, DateTime utcNow)
    {
        var toUtc = to is { Kind: DateTimeKind.Local } local ? local.ToUniversalTime() : (to ?? utcNow);
        var fromUtc = from is { Kind: DateTimeKind.Local } localFrom ? localFrom.ToUniversalTime() : (from ?? utcNow.AddDays(-30));

        return (fromUtc, toUtc);
    }
}
