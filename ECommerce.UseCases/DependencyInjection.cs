using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        // Future: Add MediatR handlers and application services
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}