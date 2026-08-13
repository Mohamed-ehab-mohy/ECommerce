using ECommerce.API;
using ECommerce.API.Common;
using ECommerce.API.Jobs;
using ECommerce.Infrastructure.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

app.UseSerilogRequestLogging();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new OpsRoleDashboardAuthorizationFilter()],
    StatsPollingInterval = 5000
});

if (!builder.Environment.IsDevelopment())
{
    RecurringJob.AddOrUpdate<ExpiredCartPurgeJob>(
        "expired-cart-purge",
        job => job.ExecuteAsync(CancellationToken.None),
        ExpiredCartPurgeJob.Schedule);

    RecurringJob.AddOrUpdate<PromotionScheduleEnforcerJob>(
        "promotion-schedule-enforcer",
        job => job.ExecuteAsync(CancellationToken.None),
        PromotionScheduleEnforcerJob.Schedule);
}

app.MapControllers();

app.Run();

public partial class Program;

