using ECommerce.Domain.Events;
using ECommerce.UseCases.Messaging.Consumers;
using ECommerce.UseCases.Messaging.Ports;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RabbitMq");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<OrderPlacedConsumer>();
            bus.AddConsumer<OrderCancelledConsumer>();
            bus.AddConsumer<OrderShippedConsumer>();

            bus.AddSagaStateMachine<CheckoutSagaStateMachine, Domain.Checkout.CheckoutSagaState>()
                .InMemoryRepository();

            bus.UsingRabbitMq((context, cfg) =>
            {
                var uri = new Uri(connectionString);
                cfg.Host(uri.Host, (ushort)(uri.Port > 0 ? uri.Port : 5672), "/", h =>
                {
                    var userInfo = uri.UserInfo.Split(':');
                    if (userInfo.Length == 2)
                    {
                        h.Username(userInfo[0]);
                        h.Password(userInfo[1]);
                    }
                });

                cfg.ReceiveEndpoint(OrderPlacedConsumer.QueueName, endpoint =>
                {
                    endpoint.SetQuorumQueue();
                    endpoint.ConfigureConsumer<OrderPlacedConsumer>(context);
                    endpoint.ConfigureConsumer<OrderCancelledConsumer>(context);
                    endpoint.ConfigureConsumer<OrderShippedConsumer>(context);
                });
            });
        });

        return services;
    }
}
