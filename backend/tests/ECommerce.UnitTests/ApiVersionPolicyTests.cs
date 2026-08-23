using ECommerce.Shared.Api;

namespace ECommerce.UnitTests;

public sealed class ApiVersionPolicyTests
{
    [Theory]
    [InlineData("/api/v1/orders", "v1")]
    [InlineData("/api/v1/health/live", "v1")]
    [InlineData("/api/v2/orders", "v2")]
    [InlineData("/api/v1", "v1")]
    public void VersionSegment_Extracts_Version_From_Api_Path(string path, string expected)
    {
        Assert.Equal(expected, ApiVersionPolicy.VersionSegment(path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/hangfire")]
    [InlineData("/swagger")]
    public void VersionSegment_Returns_Null_For_Unversioned_Path(string path)
    {
        Assert.Null(ApiVersionPolicy.VersionSegment(path));
    }

    [Theory]
    [InlineData("v1", true)]
    [InlineData("V1", true)]
    [InlineData("v2", false)]
    [InlineData(null, false)]
    public void IsCurrentVersion_Matches_Only_V1(string? version, bool expected)
    {
        Assert.Equal(expected, ApiVersionPolicy.IsCurrentVersion(version));
    }

    [Theory]
    [InlineData("/health/live", true)]
    [InlineData("/health/ready", true)]
    [InlineData("/", true)]
    [InlineData("/api/v1/health/live", false)]
    [InlineData("/api/v1/orders", false)]
    [InlineData("/hangfire", false)]
    public void IsDeprecatedPath_Flags_Legacy_Routes(string path, bool expected)
    {
        Assert.Equal(expected, ApiVersionPolicy.IsDeprecatedPath(path));
    }
}
