using RabbitMQ.Client;

namespace ECommerce.IntegrationTests;

public sealed class RabbitMqIntegrationTests : IClassFixture<RabbitMqContainerFixture>
{
    private readonly RabbitMqContainerFixture _fixture;

    public RabbitMqIntegrationTests(RabbitMqContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task RabbitMq_Connects_And_Declares_Queue()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_fixture.ConnectionString)
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync("smoke", durable: false, exclusive: false, autoDelete: true);
        Assert.True(connection.IsOpen);
    }
}
