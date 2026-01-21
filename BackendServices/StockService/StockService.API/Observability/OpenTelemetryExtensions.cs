using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace StockService.API.Observability;

/// <summary>
/// OpenTelemetry configuration extensions for distributed tracing and metrics.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddStockServiceOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = "StockService";
        var serviceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        var azureConnectionString = configuration["OpenTelemetry:AzureMonitor:ConnectionString"];

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", 
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown")
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext => 
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/swagger");
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = true;
                    })
                    .AddSource("StockService.Commands")
                    .AddSource("StockService.Queries");

                // Add Azure Monitor exporter if configured
                if (!string.IsNullOrWhiteSpace(azureConnectionString))
                {
                    tracing.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = azureConnectionString;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("StockService.Reservations")
                    .AddMeter("StockService.Adjustments")
                    .AddMeter("StockService.Alerts");

                // Add Azure Monitor exporter if configured
                if (!string.IsNullOrWhiteSpace(azureConnectionString))
                {
                    metrics.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = azureConnectionString;
                    });
                }
            });

        return services;
    }
}
