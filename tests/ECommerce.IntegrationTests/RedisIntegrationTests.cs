using StackExchange.Redis;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
public sealed class RedisIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public RedisIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Redis_Stores_And_Reads_Value()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var connection = await ConnectionMultiplexer.ConnectAsync(_fixture.RedisConnectionString);
        var database = connection.GetDatabase();
        await database.StringSetAsync("smoke", "ok");
        var value = await database.StringGetAsync("smoke");
        Assert.Equal("ok", value.ToString());
    }
}
