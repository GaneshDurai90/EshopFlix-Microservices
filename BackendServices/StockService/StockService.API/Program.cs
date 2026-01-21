using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using StockService.API.Extensions;
using StockService.API.Http;
using StockService.API.Logging;
using StockService.API.Middleware;
using StockService.API.Observability;
using StockService.Infrastructure;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ============ Logging ============
SerilogExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();

// ============ OpenTelemetry ============
builder.Services.AddStockServiceOpenTelemetry(builder.Configuration);

// ============ Application Services ============
ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

// ============ Controllers & API Behavior ============
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockService API",
        Version = "v1",
        Description = "Inventory and Stock Management API for eShopFlix"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// ============ HTTP Infrastructure ============
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

// Configure outbound HTTP client with resilience policies
builder.Services.AddHttpClient("stock-dependency", client =>
{
    var catalogApiAddress = builder.Configuration["ApiAddress:CatalogApi"];
    if (!string.IsNullOrWhiteSpace(catalogApiAddress))
    {
        client.BaseAddress = new Uri(catalogApiAddress);
    }
    client.Timeout = Timeout.InfiniteTimeSpan; // Let Polly handle timeouts
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.CreatePolicy("catalog"));

// ============ Health Checks ============
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DbConnection")!,
        name: "sql-server",
        tags: ["db", "sql"]);

// ============ OpenAPI ============
builder.Services.AddOpenApi();

// ============ MassTransit (Azure Service Bus) ============
builder.Services.AddMassTransit(config =>
{
    // Consumers can be added here when needed
    // config.AddConsumer<StockValidateConsumer>();

    var connectionString = builder.Configuration["ServiceBus:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        config.UsingAzureServiceBus((ctx, cfg) =>
        {
            cfg.Host(connectionString);
            cfg.ConfigureEndpoints(ctx);
        });
    }
    else
    {
        // Use in-memory transport for development
        config.UsingInMemory((ctx, cfg) =>
        {
            cfg.ConfigureEndpoints(ctx);
        });
    }
});

var app = builder.Build();

// ============ Middleware Pipeline ============

// Correlation ID must be first
app.UseMiddleware<CorrelationIdMiddleware>();

// Serilog request logging with enrichment
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", Activity.Current?.TraceId.ToString());
        diag.Set("CorrelationId", http.Items[CorrelationIdMiddleware.HeaderName]?.ToString() ?? http.TraceIdentifier);
        diag.Set("RequestPath", http.Request.Path);
        diag.Set("RequestMethod", http.Request.Method);
        diag.Set("StatusCode", http.Response.StatusCode);
        diag.Set("Application", "StockService");
    };
});

// Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ============ Swagger ============
var enableSwagger = app.Configuration.GetValue<bool?>("Swagger:Enabled") ?? app.Environment.IsDevelopment();
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StockService API v1");
        c.RoutePrefix = "swagger";
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// ============ Health Check Endpoints ============
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponse
});

app.Run();

// Health check response writer
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            error = entry.Value.Exception?.Message,
            duration = entry.Value.Duration.TotalMilliseconds
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        at = DateTime.UtcNow
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));
}
