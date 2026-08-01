using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Events;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IEventHandler<CustomerRegistered>, CustomerRegisteredEmailHandler>();

        return services;
    }
}
