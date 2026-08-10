namespace ECommerce.Shared.Api;

public static class ApiVersionPolicy
{
    public const string CurrentVersion = "1.0";

    public const string CurrentRouteVersion = "v1";

    public const string DeprecationSunset = "2027-08-31";

    public static string? VersionSegment(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
        {
            return null;
        }

        if (!string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var version = segments[1];
        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : null;
    }

    public static bool IsCurrentVersion(string? versionSegment) =>
        string.Equals(versionSegment, CurrentRouteVersion, StringComparison.OrdinalIgnoreCase);

    public static bool IsDeprecatedPath(string path) =>
        path.Equals("/", StringComparison.Ordinal)
        || (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith($"/api/{CurrentRouteVersion}/health", StringComparison.OrdinalIgnoreCase));
}
