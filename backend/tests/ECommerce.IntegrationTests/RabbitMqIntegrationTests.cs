using RabbitMQ.Client;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
public sealed class RabbitMqIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public RabbitMqIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task RabbitMq_Connects_And_Declares_Queue()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_fixture.RabbitMqConnectionString),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync("smoke", durable: false, exclusive: false, autoDelete: true);
        Assert.True(connection.IsOpen);
    }
}
