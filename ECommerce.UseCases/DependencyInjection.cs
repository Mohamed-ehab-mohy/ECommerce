using System.Reflection;
using ECommerce.UseCases.Common.Messaging;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<ISender, Sender>();

        services.Scan(assembly);

        return services;
    }

    private static void Scan(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces(),
                (type, iface) => new { Type = type, Interface = iface })
            .Where(x => x.Interface.IsGenericType &&
                         x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        foreach (var handler in handlerTypes)
        {
            var serviceType = handler.Interface;
            var implementationType = handler.Type;
            services.AddScoped(serviceType, implementationType);
        }
    }
}
