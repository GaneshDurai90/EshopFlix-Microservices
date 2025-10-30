using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CartService.Api.Observability
{
    public static class OpenTelemetryExtensions
    {
        public static IServiceCollection AddCartServiceOpenTelemetry(this IServiceCollection services, IConfiguration config)
        {
            var serviceName = "CartService";
            var serviceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(serviceName, serviceVersion: serviceVersion))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(o =>
                    {
                        o.SetDbStatementForText = true; // include SQL text (ensure no secrets in queries)
                        o.RecordException = true;
                    })
                    .AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = config["OpenTelemetry:AzureMonitor:ConnectionString"];
                    }))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAzureMonitorMetricExporter(o =>
                    {
                        o.ConnectionString = config["OpenTelemetry:AzureMonitor:ConnectionString"];
                    }));

            return services;
        }
    }
}
