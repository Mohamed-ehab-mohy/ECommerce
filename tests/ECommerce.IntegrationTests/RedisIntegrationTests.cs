using StackExchange.Redis;

namespace ECommerce.IntegrationTests;

public sealed class RedisIntegrationTests : IClassFixture<RedisContainerFixture>
{
    private readonly RedisContainerFixture _fixture;

    public RedisIntegrationTests(RedisContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Redis_Stores_And_Reads_Value()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var connection = await ConnectionMultiplexer.ConnectAsync(_fixture.ConnectionString);
        var database = connection.GetDatabase();
        await database.StringSetAsync("smoke", "ok");
        var value = await database.StringGetAsync("smoke");
        Assert.Equal("ok", value.ToString());
    }
}
