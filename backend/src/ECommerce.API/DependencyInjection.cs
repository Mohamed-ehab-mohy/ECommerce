using System.Threading.RateLimiting;
using ECommerce.API.Audit;
using ECommerce.API.Common;
using ECommerce.API.Grpc;
using ECommerce.API.Hubs;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Jobs;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Notifications;
using ECommerce.Infrastructure.Realtime;
using ECommerce.Infrastructure.Resilience;
using ECommerce.Infrastructure.Shipping;
using ECommerce.Infrastructure.Vault;
using ECommerce.UseCases;
using ECommerce.UseCases.Fulfillment.Shipping;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace ECommerce.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();

        var dataProvider = configuration.GetValue("DataProvider:Provider", "Postgres")!;
        var sqlServerCs = configuration.GetConnectionString("SqlServer");

        services.AddInfrastructure(
            configuration.GetConnectionString("Postgres")!,
            configuration.GetConnectionString("Redis")!,
            sqlServerCs,
            dataProvider);
        services.AddPaymentInfrastructure(configuration);
        services.AddResilience(configuration);
        services.AddMessageBus(configuration);
        services.AddJobs(configuration);
        services.AddSearchInfrastructure(configuration);

        services.Configure<VaultOptions>(configuration.GetSection(VaultOptions.SectionName));
        services.AddHttpClient("vault");
        services.AddSingleton<IVaultService, VaultSecretService>();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<CarrierOptions>(configuration.GetSection(CarrierOptions.SectionName));

        services.AddControllers();
        services.AddResponseCaching();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdProvider, HttpContextCorrelationIdProvider>();
        services.AddScoped<ITenantService, HttpContextTenantService>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ECommerce API",
                Version = "v1",
                Description = "Full-featured e-commerce platform API with 18+ modules"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile);
            }
        });

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

        var oauthOptions = configuration.GetSection(OAuthOptions.SectionName).Get<OAuthOptions>() ?? new OAuthOptions();
        services.AddSingleton(oauthOptions);
        services.AddSingleton(new OAuthClientStore(oauthOptions));

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

        services.AddGrpc(options =>
        {
            options.Interceptors.Add<JwtTokenForwardingInterceptor>();
        });

        services.AddGrpcHealthChecks();

        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));

        services.AddDataProtection().SetApplicationName("ECommerce");

        var authIpLimit = configuration.GetValue("RateLimiting:AuthIpPermitLimit", 10);
        var userLimit = configuration.GetValue("RateLimiting:UserPermitLimit", 120);
        var ipLimit = configuration.GetValue("RateLimiting:IpPermitLimit", 300);
        var tenantLimit = configuration.GetValue("RateLimiting:TenantPermitLimit", 600);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.OnRejected = (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? retry.TotalSeconds.ToString("F0")
                    : "60";

                context.HttpContext.Response.Headers.RetryAfter = retryAfter;

                var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 429,
                    Title = "Too Many Requests",
                    Detail = $"Rate limit exceeded. Retry after {retryAfter} seconds.",
                    Type = "https://tools.ietf.org/html/rfc6585"
                };

                context.HttpContext.Response.ContentType = "application/problem+json";
                return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken));
            };

            // Layered (multi-dimensional) rate limiting. Every request is evaluated
            // against ALL layers simultaneously via PartitionedRateLimiter.CreateChained;
            // exceeding any single layer rejects the request (429). This protects
            // against brute force (auth), per-user abuse, per-IP abuse, and the
            // multi-tenant "noisy neighbor" problem at the same time.
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained<HttpContext>(
            [
                // Layer 1: strict brute-force protection on credential endpoints.
                // Tighter per-IP quota applies only to /api/v1/auth/* paths.
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var path = context.Request.Path.Value ?? string.Empty;
                    if (!path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase))
                    {
                        return RateLimitPartition.GetNoLimiter("non-auth");
                    }

                    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        $"auth-ip:{clientIp}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = authIpLimit,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                }),

                // Layer 2: per-authenticated-user quota.
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var userId = context.User?.Identity?.IsAuthenticated == true
                        ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        : null;

                    return string.IsNullOrEmpty(userId)
                        ? RateLimitPartition.GetNoLimiter("anonymous-no-user")
                        : RateLimitPartition.GetSlidingWindowLimiter(
                            $"user:{userId}",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = userLimit,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                }),

                // Layer 3: per-IP quota applied to everyone (anonymous + authenticated).
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        $"ip:{clientIp}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = ipLimit,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                }),

                // Layer 4: per-tenant quota (noisy-neighbor protection). Only applies
                // when the request is resolved to a tenant.
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var tenantId = context.RequestServices.GetService<ECommerce.UseCases.Common.ITenantService>()?.GetCurrentTenantId();

                    return !tenantId.HasValue
                        ? RateLimitPartition.GetNoLimiter("anonymous-no-tenant")
                        : RateLimitPartition.GetSlidingWindowLimiter(
                            $"tenant:{tenantId.Value}",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = tenantLimit,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 6,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                })
            ]);
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowConfiguredOrigins", policy =>
            {
                policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders("X-Pagination");
            });
        });

        return services;
    }
}
