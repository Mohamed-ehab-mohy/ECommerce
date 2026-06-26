using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // هنا هنضيف الـ Application Services والـ MediatR مستقبلاً

        return services;
    }
}