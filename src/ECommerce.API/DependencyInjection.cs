using ECommerce.API.Audit;
using ECommerce.API.Common;
using ECommerce.API.Hubs;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Jobs;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Notifications;
using ECommerce.Infrastructure.Realtime;
using ECommerce.UseCases;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace ECommerce.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(
            configuration.GetConnectionString("Postgres")!,
            configuration.GetConnectionString("Redis")!);
        services.AddPaymentInfrastructure(configuration);
        services.AddMessageBus(configuration);
        services.AddJobs(configuration);

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddControllers();
        services.AddProblemDetails();
        services.AddHttpContextAccessor();

        services.AddScoped<IAuditContextProvider, AuditContextProvider>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddSignalR()
            .AddStackExchangeRedis(configuration.GetConnectionString("Redis")!, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("signalr:");
            });

        services.AddScoped<IOrderRealtimeHubContext, OrderRealtimeHubContext>();
        services.AddScoped<IWarehouseRealtimeHubContext, WarehouseRealtimeHubContext>();
        services.AddScoped<IAdminRealtimeHubContext, AdminRealtimeHubContext>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AuditRead", policy => policy
                .RequireClaim("roles", IdentityRoles.Admin, IdentityRoles.SuperAdmin));
        });

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var authSettings = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
        var keyProvider = new JwtRsaKeyProvider(jwtOptions);

        services.AddSingleton(jwtOptions);
        services.AddSingleton(authSettings);
        services.AddSingleton(keyProvider);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new RsaSecurityKey(keyProvider.Key),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddDataProtection().SetApplicationName("ECommerce");

        return services;
    }
}
