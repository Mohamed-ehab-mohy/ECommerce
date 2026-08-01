using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string postgresConnectionString)
    {
        services.AddDbContext<ECommerceDbContext>(options => options
            .UseNpgsql(postgresConnectionString)
            .AddInterceptors(new DomainEventsInterceptor()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailSender, LogEmailSender>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();

        services.AddHttpClient<IPasswordBreachChecker, HibpPasswordBreachChecker>(client =>
            client.Timeout = TimeSpan.FromSeconds(10));

        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }
}
