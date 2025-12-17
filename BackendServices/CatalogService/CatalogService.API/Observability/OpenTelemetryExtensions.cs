using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CatalogService.Api.Observability
{
    public static class OpenTelemetryExtensions
    {
        public static IServiceCollection AddCatalogServiceOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceName = "CatalogService";
            var serviceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = true;
                    })
                    .AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = configuration["OpenTelemetry:AzureMonitor:ConnectionString"];
                    }))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = configuration["OpenTelemetry:AzureMonitor:ConnectionString"];
                    }));

            return services;
        }
    }
}
