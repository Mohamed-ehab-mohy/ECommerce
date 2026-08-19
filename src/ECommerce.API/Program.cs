using ECommerce.API;
using ECommerce.API.Common;
using ECommerce.API.Grpc;
using ECommerce.API.Hubs;
using ECommerce.API.Jobs;
using ECommerce.Infrastructure.Jobs;
using Grpc.Core.Interceptors;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) => configuration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["SeqUrl"] ?? "http://localhost:5341"));

builder.Services.AddApi(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ecommerce-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OtlpTracesEndpoint"] ?? "http://localhost:5341");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OtlpMetricsEndpoint"] ?? "http://localhost:5341");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        })
        .AddPrometheusExporter());

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { },
    KnownProxies = { }
});

if (!builder.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();

app.UseSecurityHeaders();

app.UseMiddleware<ApiVersionMiddleware>();

app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.MapPrometheusScrapingEndpoint();

app.MapGet("/", () => "ECommerce API");

app.UseCors("AllowConfiguredOrigins");
app.UseRateLimiter();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

var hangfireDisabled = builder.Configuration.GetValue("Hangfire:Disabled", false);

if (!hangfireDisabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new OpsRoleDashboardAuthorizationFilter()],
        StatsPollingInterval = 5000
    });
}

if (!builder.Environment.IsDevelopment() && !hangfireDisabled)
{
    RecurringJob.AddOrUpdate<ExpiredCartPurgeJob>(
        "expired-cart-purge",
        job => job.ExecuteAsync(CancellationToken.None),
        ExpiredCartPurgeJob.Schedule);

    RecurringJob.AddOrUpdate<PromotionScheduleEnforcerJob>(
        "promotion-schedule-enforcer",
        job => job.ExecuteAsync(CancellationToken.None),
        PromotionScheduleEnforcerJob.Schedule);

    RecurringJob.AddOrUpdate<NightlyReconciliationJob>(
        "nightly-reconciliation",
        job => job.ExecuteAsync(CancellationToken.None),
        NightlyReconciliationJob.Schedule);

    RecurringJob.AddOrUpdate<LiveOpsMetricsJob>(
        "live-ops-metrics",
        job => job.ExecuteAsync(CancellationToken.None),
        LiveOpsMetricsJob.Schedule);

    RecurringJob.AddOrUpdate<StockReservationExpiryJob>(
        "stock-reservation-expiry",
        job => job.ExecuteAsync(CancellationToken.None),
        StockReservationExpiryJob.Schedule);

    RecurringJob.AddOrUpdate<PaymentTimeoutJob>(
        "payment-timeout",
        job => job.ExecuteAsync(CancellationToken.None),
        PaymentTimeoutJob.Schedule);
}

app.MapGrpcService<OrderStatusGrpcService>();
app.MapGrpcService<CatalogLookupGrpcService>();
app.MapGrpcHealthChecksService();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.MapHub<OrderHub>("/hubs/orders");
app.MapHub<WarehouseHub>("/hubs/warehouse");
app.MapHub<AdminHub>("/hubs/admin");

app.MapGet("/gateway/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/v1/rate-limit-status", () => Results.Ok(new
{
    fixedWindow = new { permitLimit = 100, windowSeconds = 10, description = "Global per-IP rate limit" },
    slidingWindowAuth = new { permitLimit = 10, windowSeconds = 60, segmentsPerWindow = 6, description = "Auth endpoints (/api/v1/auth/)" },
    rejectionStatusCode = 429,
    timestamp = DateTime.UtcNow
}));

app.MapReverseProxy();

app.Run();

public partial class Program;

